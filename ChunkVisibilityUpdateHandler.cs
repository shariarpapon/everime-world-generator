using System;
using System.Collections.Generic;
using UnityEngine;

namespace Everime.WorldManagement
{
    /// <summary>
    /// This class handles chunk updates.
    /// </summary>
    public class ChunkVisibilityUpdateHandler : MonoBehaviour
    {
        [SerializeField]
        private Transform viewer;
        [SerializeField]
        private ComputeShader visibilityComputer;
        [SerializeField]
        private ChunkUpdateMethod updateMethod;
        [SerializeField]
        private bool updateVisibility = true;
      
        [SerializeField, Tooltip("This will auto round to the nearest multiple of 16 that is greater than or equal to 16 due to the thread compatibility")]
        private int visibleChunksPerAxis;
        [SerializeField]
        private float maxViewDistance = 100;

        private World world;
        private Dictionary<Vector2, Chunk> chunks;
        private ChunkUpdateData[] dataBuffer;
        private int maxVisibleChunks;
        private int chunksVisibleRadially;

        private const int BUFFER_BYTESIZE = sizeof(float) * 2 + sizeof(int);
        private const int CHUNK_UPDATE_BUFFER_THREAD_COUNT = 16;
        private int chunkUpdateThreadGroups;

        public void Init(World world) 
        {
            this.world = world;
            this.chunks = world.chunks;

            ValidateChunksPerAxisWithThreadCount();

            chunksVisibleRadially = visibleChunksPerAxis / 2;
            maxVisibleChunks = visibleChunksPerAxis * visibleChunksPerAxis;
            chunkUpdateThreadGroups = visibleChunksPerAxis / CHUNK_UPDATE_BUFFER_THREAD_COUNT;

            visibilityComputer.SetFloat("maxViewDistance", maxViewDistance);
            visibilityComputer.SetFloat("chunkSize", world.Settings.chunkSize);
            visibilityComputer.SetFloat("chunkExtent", world.Settings.chunkSize / 2.0f);
            visibilityComputer.SetInt("chunksVisibleRadially", chunksVisibleRadially);
            visibilityComputer.SetInt("visibleChunksPerAxis", visibleChunksPerAxis);
            dataBuffer = new ChunkUpdateData[maxVisibleChunks];
        }

        #region Update Methods
        private void Update()
        {
            if (updateMethod == ChunkUpdateMethod.Update)
                UpdateChunkVisibility();
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
            if (world == null || !updateVisibility) return;

            ComputeBuffer computeDataBuffer = new ComputeBuffer(maxVisibleChunks, BUFFER_BYTESIZE);
            computeDataBuffer.SetData(dataBuffer);

            //Check the previously visible chunks with new viewer position
            int checkPreviouslyActiveKernel = visibilityComputer.FindKernel("CheckPreviouslyActiveChunks");
            visibilityComputer.SetBuffer(checkPreviouslyActiveKernel, "dataBuffer", computeDataBuffer);
            visibilityComputer.SetVector("viewerPosition", viewer.position);
            visibilityComputer.Dispatch(checkPreviouslyActiveKernel, chunkUpdateThreadGroups, chunkUpdateThreadGroups, 1);
            computeDataBuffer.GetData(dataBuffer);
            CheckVisibility();

            //Check visible chunks with updated viewer position
            int surroundingCheckKernel = visibilityComputer.FindKernel("CheckViewerSurrounding");
            Vector2 viewerChunkCoords = world.GlobalToRelativeChunkPosition(viewer.position);
            visibilityComputer.SetInts("viewerChunkCoords", (int)viewerChunkCoords.x, (int)viewerChunkCoords.y);
            visibilityComputer.SetBuffer(surroundingCheckKernel, "dataBuffer", computeDataBuffer);
            visibilityComputer.Dispatch(surroundingCheckKernel, chunkUpdateThreadGroups, chunkUpdateThreadGroups, 1);
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
