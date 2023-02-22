using System.Collections.Generic;
using UnityEngine;

namespace XEntity.WorldGeneration
{
    /// <summary>
    /// This scriptable object contains the settings for world generation.
    /// </summary>
    [CreateAssetMenu(fileName = "New World Settings", menuName = "World Generation/World Settings" )]
    public class WorldSettings : ScriptableObject
    {
 
        public GlobalWorldSettings globalSettings;
        public HeightSettings heightSettings;
        public NoiseSettings terrainNoiseSettings;
        public SpawnSetttings spawnSettings;
         
        /// <summary>
        /// Randomizes the global seed.
        /// </summary>
        public void RandomizeSeed() 
        {
            globalSettings.seed = System.IO.Path.GetRandomFileName();
        }
    }

    [System.Serializable]
    public enum HeightCalculationMethod
    {
        Default = 0,
        Squared,
        Cubed,
        CurveOnly,
        SquaredCurve,
        CubedCurve,
    }

    [System.Serializable]
    public struct GlobalWorldSettings 
    {
        public bool useRandomSeed;

        [SerializeField]
        internal string seed;
        public int WorldSeed { get { return seed.GetHashCode(); } }

        [Tooltip("The number of chunks per axis (X, Z) of the world")]
        public int worldSizeInChunks;
        public int chunkSize;
        public int worldUnitSize { get { return worldSizeInChunks * chunkSize; } }
        public Material chunkMaterial;
        [Tooltip("Color gradient for the mesh based on the normalized height of the vertices.")]
        public Gradient vertexGradient;
        public bool generateCollider;
    }

    [System.Serializable]
    public class HeightSettings 
    {
        public AnimationCurve heightCurve;
        public float heightMultiplier = 10;
        public HeightCalculationMethod heightCalculationMethod;
        public bool uniformRadialFalloff;
    }

    [System.Serializable]
    public class SpawnSetttings 
    {
        [Header("This is kind of primitive. Sorry!\n"), Range(0, 1)]
        public float globalSpawnChance = 1;
        public SpawnData[] objectsToSpawn;
    }

    [System.Serializable]
    public class SpawnData
    {
        public GameObject prefab;
        public float minSpawnHeight = 5;
        public float maxSpawnHeight = 10;
        [Range(0, 1)]
        public float spawnChance = 1;
        public bool randomizeYRotation = true;
    }
}
