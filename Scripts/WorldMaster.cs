using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace XEntity.WorldGeneration
{
    /// <summary>
    /// This class is used for creating a world either on edit-mode or play-mode.
    /// </summary>
    [RequireComponent(typeof(ChunkVisibilityUpdateHandler)), ExecuteInEditMode]
    public class WorldMaster : MonoBehaviour
    {
        public WorldSettings worldSettings;
        [SerializeField]
        private bool generateOnStart = false;
        private ChunkVisibilityUpdateHandler chunkVisibilityUpdater;

        [HideInInspector]
        public World world;

        //world generation data
        public System.Random prng;
        private float minHeight;
        private float maxHeight;
        private Vector2 worldOffset;
        private Transform worldHolder;

        //chunk generation thread
        public bool IsGeneratingWorld { get; private set; }
        private int threadReleasedChunkCount = 0;
        private int maxChunkCount;
        private Queue<Chunk> threadReleasedChunks;
        private Queue<SpawnObject> threadReleasedSpawnObjects;

        private void Start()
        {
            //Generate only if the object is in play mode.
            if (generateOnStart && Application.IsPlaying(this)) GenerateWorld();
        }

        private void Update()
        {
            UpdateThreadReleasedData();

#if UNITY_EDITOR
            if (chunkVisibilityUpdater && !Application.isPlaying)
                chunkVisibilityUpdater.UpdateChunkVisibility();
#endif
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

            IsGeneratingWorld = true;
            Debug.Log("<color=#7FB5E3>Generating world...</color>");

            ClearExistingWorld();

            chunkVisibilityUpdater = GetComponent<ChunkVisibilityUpdateHandler>();

            GlobalWorldSettings globalSettings = worldSettings.globalSettings;

            if (worldSettings.terrainNoiseSettings.scale <= 0) worldSettings.terrainNoiseSettings.scale = 0.0001f;
            if (globalSettings.useRandomSeed) worldSettings.RandomizeSeed();

            if (globalSettings.chunkSize <= 0)
                globalSettings.chunkSize = 1;

            worldHolder = new GameObject($"World_{globalSettings.WorldSeed}").transform;
            worldHolder.position = Vector3.zero;

            world = worldHolder.gameObject.AddComponent<World>();
            world.Init(worldSettings);
            chunkVisibilityUpdater.Init(world);

            minHeight = float.MaxValue;
            maxHeight = float.MinValue;

            prng = new System.Random(globalSettings.WorldSeed);
            int offsetRange = 9_999_999;
            worldOffset = new Vector2((int)(prng.NextDouble() * 2 * offsetRange - offsetRange), (int)(prng.NextDouble() * 2 * offsetRange - offsetRange));

            threadReleasedChunks = new Queue<Chunk>();
            threadReleasedSpawnObjects = new Queue<SpawnObject>();
            maxChunkCount = globalSettings.worldSizeInChunks * globalSettings.worldSizeInChunks;
            threadReleasedChunkCount = 0;

            ThreadStart threadStart = new ThreadStart(
                delegate { GenerateChunks(); });

            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        //Generates chunks of the world
        private void GenerateChunks()
        {
            GlobalWorldSettings globalSettings = worldSettings.globalSettings;
            HeightSettings heightSettings = worldSettings.heightSettings;

            for (int x = 0; x < globalSettings.worldSizeInChunks; x++)
                for (int y = 0; y < globalSettings.worldSizeInChunks; y++)
                {
                    Vector2 relativePos = new Vector2(x, y);
                    Vector3 globalPos = world.RelativeToGlobalChunkPosition(relativePos);
                    Vector2 offset = worldOffset + new Vector2(globalPos.x, globalPos.z);
                    float[,] heightMap = MapGenerator.GenerateHeightMap(globalSettings.chunkSize + 1, offset, worldSettings.terrainNoiseSettings, ref minHeight, ref maxHeight);
                    ChunkData chunkData = new ChunkData(heightMap, globalSettings.chunkMaterial, relativePos, globalPos);
                    world.chunks.Add(new Vector2(x, y), new Chunk(chunkData, false));
                }

            //post full generation calculations
            for (int x = 0; x < globalSettings.worldSizeInChunks; x++)
                for (int y = 0; y < globalSettings.worldSizeInChunks; y++)
                {
                    Chunk chunk = world.chunks[new Vector2(x, y)];
                    chunk.chunkData.heightMap = EvaluatePostFullGenerationHeightValues(chunk.chunkData.heightMap, chunk.chunkData.globalPosition);
                    chunk.GenerateTerrainMeshData(heightSettings.heightMultiplier, heightSettings.heightCurve, globalSettings.vertexGradient, heightSettings.heightCalculationMethod);

                    lock (threadReleasedChunks)
                    {
                        threadReleasedChunks.Enqueue(chunk);
                    }
                    GenerateObjectSpawnData(chunk);
                }
        }

        //Evaluates post full world generation height values.
        //This second pass over the height values is needed for data calculation that not only depend on the individual chunk data but also require the globally generated data.
        private float[,] EvaluatePostFullGenerationHeightValues(float[,] heightMap, Vector3 positionOffset)
        {
            int size = heightMap.GetLength(0);
            float[,] evaluatedHeightMap = new float[size, size];

            float mapExtent = (size - 1) / 2f;
            float maxWorldRadius = worldSettings.globalSettings.worldUnitSize / 2f;
            Vector2 worldCenter = new Vector2(maxWorldRadius, maxWorldRadius);

            HeightSettings heightSettings = worldSettings.heightSettings;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    float heightValue = Mathf.InverseLerp(minHeight, maxHeight, heightMap[x, y]);

                    if (heightSettings.uniformRadialFalloff)
                    {
                        float worldX = (x - mapExtent) + positionOffset.x;
                        float worldZ = (y - mapExtent) + positionOffset.z;
                        Vector2 vertexPosition2D = new Vector2(worldX, worldZ);
                        float radialFalloff = 1 - maxWorldRadius / Vector2.Distance(vertexPosition2D, worldCenter);
                        heightValue = heightValue - radialFalloff;
                    }

                    evaluatedHeightMap[x, y] = Mathf.Clamp01(heightValue);
                }

            return evaluatedHeightMap;
        }

        //Generates object spawn data in chunks based on world settings
        private void GenerateObjectSpawnData(Chunk chunk)
        {
            SpawnSetttings spawnSettings = worldSettings.spawnSettings;

            if (spawnSettings.objectsToSpawn.Length <= 0) return;

            int size = worldSettings.globalSettings.chunkSize + 1;
            int vertexIndex = 0;

            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    Vector3 vertexPosition = chunk.meshData.vertices[vertexIndex];
                    float randomizer = (float)prng.NextDouble();

                    if (randomizer <= spawnSettings.globalSpawnChance)
                    {
                        SpawnData spawnData = spawnSettings.objectsToSpawn[prng.Next(0, spawnSettings.objectsToSpawn.Length)];
                        randomizer = (float)prng.NextDouble();
                        bool isHeightCompatible = vertexPosition.y >= spawnData.minSpawnHeight && vertexPosition.y <= spawnData.maxSpawnHeight;

                        if (randomizer <= spawnData.spawnChance && isHeightCompatible)
                        {
                            Quaternion spawnRotation = spawnData.randomizeYRotation ? Quaternion.Euler(0, (float)(prng.NextDouble() * 360), 0) : spawnData.prefab.transform.rotation;
                            Vector3 spawnPosition = vertexPosition + chunk.chunkData.globalPosition;
                            SpawnObject spawnObject = new SpawnObject(chunk, spawnData.prefab, spawnPosition, spawnRotation);
                            threadReleasedSpawnObjects.Enqueue(spawnObject);
                        }
                    }
                    vertexIndex++;
                }
        }

        /// <summary>
        /// Finalizes and updates any chunk that is released by the world generation thread.
        /// </summary>
        public void UpdateThreadReleasedData()
        {
            if (!IsGeneratingWorld) return;

            if (threadReleasedChunks != null)
            {
                while (threadReleasedChunks.Count > 0)
                {
                    Chunk chunk = threadReleasedChunks.Dequeue();
                    if (chunk == null) 
                    {
                        return;
                    }
                    chunk.InstantiateChunk(worldSettings.globalSettings.generateCollider);
                    chunk.chunkGameObject.transform.SetParent(worldHolder);
                    threadReleasedChunkCount++;
                }
            }

            if (threadReleasedSpawnObjects != null)
            {
                while (threadReleasedSpawnObjects.Count > 0)
                {
                    SpawnObject s = threadReleasedSpawnObjects.Dequeue();

                    if (s.prefab == null) 
                    {
                        Debug.LogError("Spawn object prefab is null.");
                    }

                    Instantiate(s.prefab, s.position, s.rotation, s.chunk.CreateChunkGameObject().transform);
                }
            }

            if (threadReleasedChunkCount >= maxChunkCount)
            {
                IsGeneratingWorld = false;
                Debug.Log("<color=#5AD37F>World generation complete!</color>");
            }
        }

        /// <summary>
        /// Clears any existing world on this world master
        /// </summary>
        public void ClearExistingWorld()
        {
            if (worldHolder == null) return;
            DestroyImmediate(worldHolder.gameObject);

            world = null;
        }
        #endregion

        /// <summary>
        /// Activate or deactivate all the chunks of the world.
        /// </summary>
        public void SetChunkVisibility(bool setVisible) 
        {
            if (world == null || IsGeneratingWorld) return;

            for (int x = 0; x < worldSettings.globalSettings.worldSizeInChunks; x++)
                for (int y = 0; y < worldSettings.globalSettings.worldSizeInChunks; y++) 
                {
                    world.chunks[new Vector2(x, y)].SetVisible(setVisible);
                }
        }

        //Holds data for spawning object in a chunk
        private struct SpawnObject 
        {
            public Chunk chunk;
            public GameObject prefab;
            public Vector3 position;
            public Quaternion rotation;

            public SpawnObject(Chunk chunk, GameObject prefab, Vector3 position, Quaternion rotation) 
            {
                this.prefab = prefab;
                this.chunk = chunk;
                this.position = position;
                this.rotation = rotation;
            }
        }

    }

}
