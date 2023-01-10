using System.Collections.Generic;
using UnityEngine;

namespace Everime.WorldManagement
{
    /// <summary>
    /// The world class generates the world map and holds all data of the particular world.
    /// </summary>
    public class World
    {
        public WorldSettings Settings { get; private set; }
        public readonly Dictionary<Vector2, Chunk> chunks;

        public World(WorldSettings settings) 
        {
            chunks = new Dictionary<Vector2, Chunk>();
            Settings = settings;
        }

        /// <summary>
        /// Returns the relative position of the chunk that the given world position is occupying.
        /// </summary>
        public Vector2 GlobalToRelativeChunkPosition(Vector3 worldPos)
        {
            Vector2 chunkCoord = new Vector2(Mathf.RoundToInt((worldPos.x - Settings.worldOffset.x) / Settings.chunkSize), Mathf.RoundToInt((worldPos.z - Settings.worldOffset.z) / Settings.chunkSize));
            chunkCoord.x = Mathf.Clamp(chunkCoord.x, 0, Settings.worldSizeInChunks - 1);
            chunkCoord.y = Mathf.Clamp(chunkCoord.y, 0, Settings.worldSizeInChunks - 1);
            return chunkCoord;
        }

        /// <summary>
        /// Returns the global position of the chunk that the given relative position is occupying. 
        /// </summary>
        public Vector3 RelativeToGlobalChunkPosition(Vector2 relativePos) 
        {
            return new Vector3(relativePos.x, 0, relativePos.y) * Settings.chunkSize + Settings.worldOffset;
        }
    }

}
