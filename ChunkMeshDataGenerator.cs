using UnityEngine;

namespace Everime.WorldManagement
{
    /// <summary>
    /// This class contains methods for chunk mesh data generation.
    /// </summary>
    internal static class ChunkMeshDataGenerator
    {
        internal static MeshData GenerateMeshData(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, Gradient vertexGradient,  HeightCalculationMethod method)
        {
            int meshSize = heightMap.GetLength(0);

            MeshData meshData = new MeshData(meshSize);

            float meshExtent = (meshSize - 1) / 2f;
            int vertexIndex = 0;

            for (int x = 0; x < meshSize; x++)
                for (int y = 0; y < meshSize; y++)
                {
                    float vertexHeight = CalculateVertexHeight(heightMap[x, y], heightCurve, method);

                    meshData.AddVertex(vertexIndex, new Vector3(x - meshExtent, vertexHeight * heightMultiplier, y - meshExtent));
                    meshData.AddUV(vertexIndex, new Vector2(x / (float)meshSize, y / (float)meshSize));
                    meshData.AddColor(vertexIndex, vertexGradient.Evaluate(vertexHeight));

                    if (x < meshSize - 1 && y < meshSize - 1)
                    {
                        meshData.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + meshSize);
                        meshData.AddTriangle(vertexIndex + 1, vertexIndex + meshSize + 1, vertexIndex + meshSize);
                    }

                    vertexIndex++;
                }

            return meshData;
        }

        #region Utils
        private static float CalculateVertexHeight(float heightMapValue, AnimationCurve heightCurve, HeightCalculationMethod method) 
        {
            switch (method)
            {
                case HeightCalculationMethod.Square:
                    return heightMapValue * heightMapValue;
                case HeightCalculationMethod.Cube:
                    return heightMapValue * heightMapValue * heightMapValue;
                case HeightCalculationMethod.Curve:
                    return heightCurve.Evaluate(heightMapValue);
                case HeightCalculationMethod.SquareCurve:
                    return heightMapValue * heightMapValue * heightCurve.Evaluate(heightMapValue);
                case HeightCalculationMethod.CubeCurve:
                    return heightMapValue * heightMapValue * heightMapValue * heightCurve.Evaluate(heightMapValue);
                default:
                    return heightMapValue;
            }
        }

        private static Vector3[] CalculateNormals(Mesh mesh) 
        {
            Vector3[] normals = new Vector3[mesh.vertices.Length];
            int triCount = mesh.triangles.Length / 3;
            for (int i = 0; i < triCount; i++) 
            {
                int triangleIndex = i * 3;
                int indexA = mesh.triangles[triangleIndex];
                int indexB= mesh.triangles[triangleIndex + 1];
                int indexC = mesh.triangles[triangleIndex + 2];

                Vector3 triangleNormal = SurfaceNormalFromIndices(mesh.vertices, indexA, indexB, indexC);
                normals[indexA] = triangleNormal;
                normals[indexB] = triangleNormal;
                normals[indexC] = triangleNormal;
            }

            for (int i = 0; i < normals.Length; i++) 
            {
                normals[i].Normalize();
            }

            return normals;
        }

        private static Vector3 SurfaceNormalFromIndices(Vector3[] vertices, int a, int b, int c) 
        {
            Vector3 pointA = vertices[a];
            Vector3 pointB = vertices[b];
            Vector3 pointC = vertices[c];

            Vector3 sideAB = pointB - pointA;
            Vector3 sideAC = pointC - pointB;
            return Vector3.Cross(sideAB, sideAC).normalized;
        }
        #endregion
    }

    public class MeshData 
    {
        public int[] triangles;
        public Vector3[] vertices;
        public Vector2[] uv;
        public Color[] colors;

        private int triangleIndex = 0;

        public MeshData(int vertsPerAxis) 
        {
            triangles = new int[(vertsPerAxis - 1) * (vertsPerAxis - 1) * 6];
            vertices = new Vector3[vertsPerAxis * vertsPerAxis];
            uv = new Vector2[vertsPerAxis * vertsPerAxis];
            colors = new Color[vertsPerAxis * vertsPerAxis];
        }

        public void AddTriangle(int a, int b, int c) 
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }

        public void AddVertex(int index, Vector3 position) 
        {
            vertices[index] = position;
        }

        public void AddUV(int index, Vector2 vertexUV) 
        {
            uv[index] = vertexUV;
        }

        public void AddColor(int index, Color color) 
        {
            colors[index] = color;
        }

        public Mesh CreateMeshFromData() 
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}