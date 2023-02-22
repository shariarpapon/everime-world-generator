using UnityEngine;

namespace Everime.WorldGeneration
{
    public class ChunkData
    {
        public float[,] heightMap;
        public readonly Vector2 relativePosition;
        public readonly Vector3 globalPosition;
        public readonly Material chunkMaterial;

        public ChunkData(float[,] heightMap, Material chunkMaterial, Vector2 relativePosition, Vector3 globalPosition)
        {
            this.heightMap = heightMap;
            this.chunkMaterial = chunkMaterial;
            this.relativePosition = relativePosition;
            this.globalPosition = globalPosition;
        }
    }
}