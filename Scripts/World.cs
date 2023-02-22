using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace XEntity.WorldGeneration
{
    /// <summary>
    /// The world class generates the world map and holds all data of the particular world.
    /// </summary>
    public class World : MonoBehaviour
    {
        public Dictionary<Vector2, Chunk> chunks;
        [HideInInspector]
        public WorldSettings worldSettings;

        /// <summary>
        /// Initializes all variables.
        /// </summary>
        public void Init(WorldSettings settings) 
        {
            chunks = new Dictionary<Vector2, Chunk>();
            worldSettings = settings;
        }

        /// <summary>
        /// Returns the relative position of the chunk that the given world position is occupying.
        /// </summary>
        public Vector2 GlobalToRelativeChunkPosition(Vector3 worldPos)
        {
            Vector2 chunkCoord = new Vector2(Mathf.RoundToInt(worldPos.x / worldSettings.globalSettings.chunkSize), Mathf.RoundToInt(worldPos.z / worldSettings.globalSettings.chunkSize));
            chunkCoord.x = Mathf.Clamp(chunkCoord.x, 0, worldSettings.globalSettings.worldSizeInChunks - 1);
            chunkCoord.y = Mathf.Clamp(chunkCoord.y, 0, worldSettings.globalSettings.worldSizeInChunks - 1);
            return chunkCoord;
        }

        /// <summary>
        /// Returns the global position of the chunk that the given relative position is occupying. 
        /// </summary>
        public Vector3 RelativeToGlobalChunkPosition(Vector2 relativePos) 
        {
            return new Vector3(relativePos.x, 0, relativePos.y) * worldSettings.globalSettings.chunkSize;
        }
    }

}
