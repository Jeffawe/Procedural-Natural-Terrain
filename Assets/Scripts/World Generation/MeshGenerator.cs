using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;


        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPos = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);
                meshData.AddVertex(vertexPos, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
                vertexIndex++;
            }
        }

        meshData.BakeNormals();
        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices;

    int[] triangles;
    Vector2[] uvs;

    Vector3[] borderVertices;
    Vector3[] bakedNormals;
    int[] borderTriangles;

    int triangleIndex;
    int borderTriangleIndex;

    public MeshData(int verticesPerLine)
    {
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertex(Vector3 vertexPos, Vector2 verticesUV, int vertexIndex)
    {
        if(vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPos;

        }
        else
        {
            vertices[vertexIndex] = vertexPos;
            uvs[vertexIndex] = verticesUV;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if(a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] normals = new Vector3[vertices.Length];

        int trianglesCount = triangles.Length / 3;
        for (int i = 0; i < trianglesCount; i++)
        {
            int normalTriIndex = i * 3;
            int vertexIndexA = triangles[normalTriIndex];
            int vertexIndexB = triangles[normalTriIndex + 1];
            int vertexIndexC = triangles[normalTriIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromVertex(vertexIndexA, vertexIndexB, vertexIndexC);
            normals[vertexIndexA] += triangleNormal;
            normals[vertexIndexB] += triangleNormal;
            normals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleIndex = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleIndex; i++)
        {
            int normalTriIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriIndex];
            int vertexIndexB = borderTriangles[normalTriIndex + 1];
            int vertexIndexC = borderTriangles[normalTriIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromVertex(vertexIndexA, vertexIndexB, vertexIndexC);
            if(vertexIndexA >= 0)
            {
                normals[vertexIndexA] += triangleNormal;
            }

            if(vertexIndexB >= 0)
            {
                normals[vertexIndexB] += triangleNormal;
            }

            if (vertexIndexC >= 0)
            {
                normals[vertexIndexC] += triangleNormal;
            } 
        }

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i].Normalize();
        }

        return normals;
    }

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    Vector3 SurfaceNormalFromVertex(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.normals =  bakedNormals;

        return mesh;
    }
}