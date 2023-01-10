using System.Collections.Generic;
using UnityEngine;

namespace Everime.WorldManagement
{        
    /// <summary>
    /// This scriptable object contains the settings for world generation.
    /// </summary>
    [CreateAssetMenu(fileName = "New World Settings", menuName = "World Generation/World Settings" )]
    public class WorldSettings : ScriptableObject
    {
        [Header("Global Settings")]
        public bool useRandomSeed;

        [SerializeField]
        private string seed;
        public int Seed { get { return seed.GetHashCode(); } }
        [Tooltip("The number of chunks per axis (X, Z) of the world")]
        public int worldSizeInChunks;
        public int worldUnitSize { get { return worldSizeInChunks * chunkSize; } }

        public AnimationCurve heightCurve;
        public float heightMultiplier;
        public Vector3 worldOffset;
        public HeightCalculationMethod heightCalculationMethod;
        public Gradient vertexGradient;
        public bool useFalloff;
        
        [Header("Chunk Settings")]
        public int chunkSize;
        public Material chunkMaterial;
        public NoiseSettings heightNoiseSettings;

        [Header("Spawn Settings"), Range(0, 1)]
        public float spawnChance;
        public List<GameObject> worldObjects;

        /// <summary>
        /// Returns a seed that is generated from appending the passed in seed with the gloabl seed.
        /// </summary>
        public int AppendedSeed(string appendSeed) 
        {
            return (seed + appendSeed).GetHashCode();
        }

        /// <summary>
        /// Randomizes the global seed.
        /// </summary>
        public void RandomizeSeed() 
        {
            seed = System.IO.Path.GetRandomFileName();
        }
    }

    [System.Serializable]
    public enum HeightCalculationMethod
    {
        Default = 0,
        Square,
        Cube,
        Curve,
        SquareCurve,
        CubeCurve,
    }
}
