using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Everime.WorldManagement
{
    /// <summary>
    /// This class is used for creating a world and updating its chunks at runtime.
    /// </summary>
    [RequireComponent(typeof(ChunkVisibilityUpdateHandler)), ExecuteAlways]
    public class WorldMaster : MonoBehaviour
    {
        public WorldSettings worldSettings; 
        [SerializeField]
        private bool generateOnStart = false;
        [SerializeField]
        private bool worldVisibleByDefault = false;

        private ChunkVisibilityUpdateHandler chunkVisibilityUpdater;
        private World world;

        //world generation
        [HideInInspector]
        public float minHeight;
        [HideInInspector]
        public float maxHeight;
        [HideInInspector]
        public int appendedSeed;
        public System.Random prng;
        private Vector2 worldOffset;
        private Transform worldHolder;

        //chunk generation thread
        public bool IsGeneratingWorld { get; private set; }
        private Queue<Chunk> chunkQueue;
        private Queue<SpawnObject> spawnObjectQueue;

        private void Start()
        {
            if (generateOnStart) GenerateWorld();
        }

        private void Update()
        {
#if UNITY_EDITOR 
            if (world != null)
            {
                chunkVisibilityUpdater?.UpdateChunkVisibility();
            }
#endif
            UpdateThreadReleasedChunks();
        }
       
        #region World Generation

        /// <summary>
        /// First clears any existing world, then generates a new world.
        /// </summary>
        public void GenerateWorld()
        {
            if (worldSettings == null)
            {
                Debug.LogError("World creation failed! Make sure to assign the worldSettings");
                return;
            }

            ClearExistingWorld();

            chunkVisibilityUpdater = GetComponent<ChunkVisibilityUpdateHandler>();

            if (worldSettings.heightNoiseSettings.scale <= 0) worldSettings.heightNoiseSettings.scale = 0.0001f;
            if (worldSettings.useRandomSeed) worldSettings.RandomizeSeed();

            if (worldSettings.chunkSize <= 0)
                worldSettings.chunkSize = 1;

            world = new World(worldSettings);
            chunkVisibilityUpdater.Init(world);

            minHeight = float.MaxValue;
            maxHeight = float.MinValue;
            appendedSeed = worldSettings.AppendedSeed("overworld");

            prng = new System.Random(appendedSeed);
            int noiseRange = 9_999_999;
            worldOffset = new Vector2((int)(prng.NextDouble() * 2 * noiseRange - noiseRange), (int)(prng.NextDouble() * 2 * noiseRange - noiseRange));

            worldHolder = new GameObject($"World_{appendedSeed}").transform;
            worldHolder.position = Vector3.zero;

            chunkQueue = new Queue<Chunk>();
            spawnObjectQueue = new Queue<SpawnObject>();

            ThreadStart threadStart = new ThreadStart(
                delegate { GenerateChunks(); });

            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        private void GenerateChunks()
        {
            for (int x = 0; x < worldSettings.worldSizeInChunks; x++)
                for (int y = 0; y < worldSettings.worldSizeInChunks; y++)
                {
                    Vector2 relativePos = new Vector2(x, y);
                    Vector3 globalPos = world.RelativeToGlobalChunkPosition(relativePos);
                    Vector2 offset = worldOffset + new Vector2(globalPos.x, globalPos.z);
                    float[,] heightMap = MapGenerator.GenerateHeightMap(worldSettings.chunkSize + 1, offset, worldSettings.heightNoiseSettings, ref minHeight, ref maxHeight);
                    ChunkData chunkData = new ChunkData(heightMap, worldSettings.chunkMaterial, relativePos, globalPos);
                    world.chunks.Add(new Vector2(x, y), new Chunk(chunkData, worldVisibleByDefault));
                }

            //post full generation calculations ----> 
            for (int x = 0; x < worldSettings.worldSizeInChunks; x++)
                for (int y = 0; y < worldSettings.worldSizeInChunks; y++)
                {
                    Chunk chunk = world.chunks[new Vector2(x, y)];

                    if (worldSettings.useFalloff)
                        chunk.chunkData.heightMap = EvaluatePostFullGenerationHeightValues(chunk.chunkData.heightMap, chunk.chunkData.globalPosition);
                    else 
                        chunk.chunkData.heightMap = NormalizeHeightMap(chunk.chunkData.heightMap);

                    chunk.GenerateTerrainMeshData(worldSettings.heightMultiplier, worldSettings.heightCurve, worldSettings.vertexGradient, worldSettings.heightCalculationMethod);
                    lock (chunkQueue)
                    {
                        chunkQueue.Enqueue(chunk);
                    }
                    SpawnObjectsInChunk(chunk);
                }
        }

        private float[,] EvaluatePostFullGenerationHeightValues(float[,] heightMap, Vector3 worldMapPosition)
        {
            int size = heightMap.GetLength(0);
            float[,] newHeightMap = new float[size, size];

            float mapExtent = (size - 1) / 2f;
            float maxDistToCenter = worldSettings.worldUnitSize / 2f;
            Vector2 center = new Vector2(maxDistToCenter, maxDistToCenter);

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float heightValue = Mathf.InverseLerp(minHeight, maxHeight, heightMap[x, y]);

                    float worldX = (x - mapExtent) + worldMapPosition.x;
                    float worldZ = (y - mapExtent) + worldMapPosition.z;

                    Vector2 vertexPosition = new Vector2(worldX, worldZ);
                    float falloff = 1 - maxDistToCenter / Vector2.Distance(vertexPosition, center);

                    newHeightMap[x, y] = Mathf.Clamp01(heightValue - (falloff));
                }
            return newHeightMap;
        }

        private float[,] NormalizeHeightMap(float[,] heightMap)
        {
            int size = heightMap.GetLength(0);
            float[,] normalizedMap = new float[size, size];

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float heightValue = Mathf.InverseLerp(minHeight, maxHeight, heightMap[x, y]);
                    normalizedMap[x, y] = heightValue;
                }
            return normalizedMap;
        }

        private void SpawnObjectsInChunk(Chunk chunk)
        {
            int size = worldSettings.chunkSize + 1;
            float ext = (size - 1) / 2f;
            int vertexIndex = 0;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    Vector3 vertexPosition = chunk.meshData.vertices[vertexIndex];
                    bool canSpawn = vertexPosition.y >= worldSettings.heightMultiplier;
                    if (canSpawn)
                    {
                        float rand = (float)prng.NextDouble();
                        if (rand <= worldSettings.spawnChance)
                        {
                            GameObject g = worldSettings.worldObjects[prng.Next(0, worldSettings.worldObjects.Count)];
                            Quaternion rot = Quaternion.Euler(0, (float)(prng.NextDouble() * 360), 0);
                            Vector3 pos = vertexPosition + chunk.chunkData.globalPosition;
                            SpawnObject s = new SpawnObject(g, chunk, pos, rot);
                            spawnObjectQueue.Enqueue(s);
                        }
                    }
                    vertexIndex++;
                }
        }

        /// <summary>
        /// Finalizes and updates any chunk that is released by the world generation thread.
        /// </summary>
        public void UpdateThreadReleasedChunks()
        {
            if (chunkQueue == null) return;

            while (chunkQueue.Count > 0)
            {
                Chunk chunk = chunkQueue.Dequeue();
                chunk.InstantiateChunk();
                chunk.chunkGameObject.transform.SetParent(worldHolder);
            }

            if (spawnObjectQueue == null) return;

            while (spawnObjectQueue.Count > 0) 
            {
                SpawnObject s = spawnObjectQueue.Dequeue();
                Instantiate(s.prefab, s.position, s.rotation, s.chunk.CreateChunkGameObject().transform);
            }
        }

        public void ClearExistingWorld()
        {
            if (worldHolder == null) return;
            DestroyImmediate(worldHolder.gameObject);
            world = null;
        }
        #endregion

        private struct SpawnObject 
        {
            public GameObject prefab;
            public Chunk chunk;
            public Vector3 position;
            public Quaternion rotation;

            public SpawnObject(GameObject obj, Chunk chunk, Vector3 pos, Quaternion rot) 
            {
                this.prefab = obj;
                this.chunk = chunk;
                this.position = pos;
                this.rotation = rot;
            }
        }

    }

}
