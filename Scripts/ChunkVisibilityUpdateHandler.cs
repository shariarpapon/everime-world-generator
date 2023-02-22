using System;
using System.Collections.Generic;
using UnityEngine;

namespace Everime.WorldGeneration
{
    /// <summary>
    /// This class handles chunk visibility updates.
    /// </summary>
    public class ChunkVisibilityUpdateHandler : MonoBehaviour
    {
        public bool isEnabled = true;

        [SerializeField]
        private Transform viewer;
        [SerializeField]
        private ComputeShader visibilityComputer;
        [SerializeField]
        private ChunkUpdateMethod updateMethod;
      
        [SerializeField, Tooltip("This will auto round to the nearest multiple of 16 that is greater than or equal to 16 due to the thread compatibility")]
        private int visibleChunksPerAxis;
        private float MaxViewDistance { get { return (visibleChunksPerAxis / 2) * world.worldSettings.globalSettings.chunkSize - world.worldSettings.globalSettings.chunkSize; } }

        private World world;
        private Dictionary<Vector2, Chunk> chunks;
        private int maxVisibleChunks;
        private int chunksVisibleRadially;

        // Do NOT modify unless you know your way around comptue shaders
        private const int COMPUTE_BUFFER_BYTESIZE = sizeof(float) * 2 + sizeof(int);
        private const int CHUNK_UPDATE_BUFFER_THREAD_COUNT = 16;
        private int chunkUpdateThreadGroups;
        private int checkSurroundingChunksKernel;
        private int checkPreviouslyActiveChunksKernel;
        private ChunkUpdateData[] dataBuffer;

        public void Init(World world) 
        {
            this.world = world;
            this.chunks = world.chunks;

            ValidateChunksPerAxisWithThreadCount();

            chunksVisibleRadially = visibleChunksPerAxis / 2;
            maxVisibleChunks = visibleChunksPerAxis * visibleChunksPerAxis;
            chunkUpdateThreadGroups = visibleChunksPerAxis / CHUNK_UPDATE_BUFFER_THREAD_COUNT;

            checkSurroundingChunksKernel = visibilityComputer.FindKernel("CheckSurroundingChunks");
            checkPreviouslyActiveChunksKernel = visibilityComputer.FindKernel("CheckPreviouslyActiveChunks");

            visibilityComputer.SetFloat("maxViewDistance", MaxViewDistance);
            visibilityComputer.SetFloat("chunkSize", world.worldSettings.globalSettings.chunkSize);
            visibilityComputer.SetFloat("chunkExtent", world.worldSettings.globalSettings.chunkSize / 2.0f);
            visibilityComputer.SetInt("chunksVisibleRadially", chunksVisibleRadially);
            visibilityComputer.SetInt("visibleChunksPerAxis", visibleChunksPerAxis);
            dataBuffer = new ChunkUpdateData[maxVisibleChunks];
        }

        #region Update Methods
        private void Update()
        {
            if (updateMethod == ChunkUpdateMethod.Update)
                UpdateChunkVisibility();

            //Updates chunk visibility here regardless of the chosen update method when in editor.
#if UNITY_EDITOR
            UpdateChunkVisibility();
#endif
        }

        private void FixedUpdate()
        {
            if (updateMethod == ChunkUpdateMethod.FixedUpdate)
                UpdateChunkVisibility();
        }

        private void LateUpdate()
        {
            if (updateMethod == ChunkUpdateMethod.LateUpdate)
                UpdateChunkVisibility();
        }
        #endregion

        /// <summary>
        /// Updates the visibility of the chunks based on its distance from the viewer.
        /// </summary>
        public void UpdateChunkVisibility()
        {
            if (world == null || chunks == null || !isEnabled) return;

            ComputeBuffer computeDataBuffer = new ComputeBuffer(maxVisibleChunks, COMPUTE_BUFFER_BYTESIZE);
            computeDataBuffer.SetData(dataBuffer);

            //Check the previously active chunks with new viewer position
            visibilityComputer.SetBuffer(checkPreviouslyActiveChunksKernel, "dataBuffer", computeDataBuffer);
            visibilityComputer.SetVector("viewerPosition", viewer.position);
            visibilityComputer.Dispatch(checkPreviouslyActiveChunksKernel, chunkUpdateThreadGroups, chunkUpdateThreadGroups, 1);
            computeDataBuffer.GetData(dataBuffer);
            CheckVisibility();

            //Check visible chunks with updated viewer position
            Vector2 viewerChunkCoords = world.GlobalToRelativeChunkPosition(viewer.position);
            visibilityComputer.SetInts("viewerChunkCoords", (int)viewerChunkCoords.x, (int)viewerChunkCoords.y);
            visibilityComputer.SetBuffer(checkSurroundingChunksKernel, "dataBuffer", computeDataBuffer);
            visibilityComputer.Dispatch(checkSurroundingChunksKernel, chunkUpdateThreadGroups, chunkUpdateThreadGroups, 1);
            computeDataBuffer.GetData(dataBuffer);
            computeDataBuffer.Dispose();
            CheckVisibility();
        }

        private void CheckVisibility()
        {
            foreach (ChunkUpdateData data in dataBuffer)
            {
                if (chunks.ContainsKey(data.coord))
                {
                    if (data.setActive == 1) world.chunks[data.coord].SetVisible(true);
                    else world.chunks[data.coord].SetVisible(false);
                }
            }
        }

        private void OnValidate()
        {
            ValidateChunksPerAxisWithThreadCount();
        }

        //NOTE: The number of visible chunks per axis must be greater than and a multiple of CHUNK_UPDATE_BUFFER_THREAD_COUNT.
        private void ValidateChunksPerAxisWithThreadCount() 
        {
            visibleChunksPerAxis -= visibleChunksPerAxis % CHUNK_UPDATE_BUFFER_THREAD_COUNT;
            if (visibleChunksPerAxis < CHUNK_UPDATE_BUFFER_THREAD_COUNT) visibleChunksPerAxis = CHUNK_UPDATE_BUFFER_THREAD_COUNT;
        }

        [Serializable]
        private enum ChunkUpdateMethod
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        [Serializable]
        private struct ChunkUpdateData 
        {
            public Vector2 coord;
            public int setActive;
        }

    }
}
