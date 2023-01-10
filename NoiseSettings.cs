using UnityEngine;

namespace Everime.WorldManagement
{
    /// <summary>
    /// This class contains settings for nosie map generation.
    /// </summary>
    [System.Serializable]
    public class NoiseSettings
    {
        public float scale = 40;
        public float amplitude = 1;
        public float frequency = 1;
        public int octaves = 1;
        [Range(1, 15)]
        public float lacunarity = 1;
        [Range(0, 1)]
        public float persistence = 1;
    }
}
