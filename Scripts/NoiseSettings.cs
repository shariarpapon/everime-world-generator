using UnityEngine;

namespace XEntity.WorldGeneration
{
    /// <summary>
    /// This class contains settings for nosie map generation.
    /// </summary>
    [System.Serializable]
    public class NoiseSettings
    {
        public float scale = 200;
        public int octaves = 4;
        [Range(1, 15)]
        public float lacunarity = 2;
        [Range(0, 1)]
        public float persistence = 0.3f;
    }
}
