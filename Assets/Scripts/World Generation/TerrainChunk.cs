using System;
using UnityEngine;

public class TerrainChunk
{
    public Vector2 coordinate;
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
    public const float colliderGenerationDistanceThreshold = 5f;

    GameObject meshObject;
    Vector2 sampleCenter;
    Bounds bounds;

    LODInfo[] detailLevels;
    LODMesh[] lodMeshes;

    int colliderLODIndex;

    MeshRenderer renderer;
    MeshFilter filter;
    MeshCollider collider;

    HeightMap heightMapData;
    bool heightMapRecieved;
    int previousLODIndex = -1;
    float maxViewDistance;

    bool hasSetCollider;
    HeightMapSettings heightMapSettings;
    MeshSettings meshSettings;
    Transform viewerPos;

    public TerrainChunk(Vector2 coord, HeightMapSettings settings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderODIndex, Transform parent, Transform viewerPos, Material mat)
    {
        this.coordinate = coord;
        this.detailLevels = detailLevels;
        this.colliderLODIndex = colliderODIndex;
        this.meshSettings = meshSettings;
        this.viewerPos = viewerPos;
        heightMapSettings = settings;

        sampleCenter = coord * meshSettings.meshWorldSize / meshSettings.meshScale;
        Vector2 pos = coordinate * meshSettings.meshWorldSize;
        bounds = new Bounds(pos, Vector2.one * meshSettings.meshWorldSize);


        meshObject = new GameObject("Terrain Object");
        renderer = meshObject.AddComponent<MeshRenderer>();
        filter = meshObject.AddComponent<MeshFilter>();
        collider = meshObject.AddComponent<MeshCollider>();
        renderer.material = mat;
        meshObject.transform.position = new Vector3(pos.x, 0, pos.y);
        meshObject.transform.parent = parent;

        SetVisible(false);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            lodMeshes[i].updateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex) lodMeshes[i].updateCallback += UpdateCollisionMesh;
        }

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshhold;
        
    }

    /// <summary>
    /// Generates the Height Map for the terrain Chunk
    /// </summary>
    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings, sampleCenter), OnHeightMapRecieved);
    }

    public Vector2 viewerPosition
    {
        get
        {
            return new Vector2(viewerPos.position.x, viewerPos.position.y);
        }
    }

    /// <summary>
    /// Call back for when the height map has been loaded up
    /// </summary>
    /// <param name="heightMapObject">The height Map</param>
    void OnHeightMapRecieved(object heightMapObject)
    {
        this.heightMapData = (HeightMap)heightMapObject;
        heightMapRecieved = true;

        UpdateTerrainChunk();
    }

    /// <summary>
    /// Updates the Terrain Chunk Information
    /// </summary>
    public void UpdateTerrainChunk()
    {
        if (!heightMapRecieved) return;

        float viewDistFromNearestEdge = MathF.Sqrt(bounds.SqrDistance(viewerPosition));
        bool wasVisible = isVisible();
        bool visible = viewDistFromNearestEdge <= maxViewDistance;

        if (visible)
        {
            int lodIndex = 0;

            for (int i = 0; i < detailLevels.Length - 1; i++)
            {
                if (viewDistFromNearestEdge > detailLevels[i].visibleDistanceThreshhold)
                {
                    lodIndex = i + 1;
                }
                else
                {
                    break;
                }
            }

            if (lodIndex != previousLODIndex)
            {
                LODMesh lODMesh = lodMeshes[lodIndex];
                if (lODMesh.hasMesh)
                {
                    filter.mesh = lODMesh.mesh;
                    previousLODIndex = lodIndex;
                }
                else if (!lODMesh.hasRequestedMesh)
                {
                    lODMesh.RequestMesh(heightMapData, meshSettings);
                }
            }
        }

        if (wasVisible != visible)
        {
            SetVisible(visible);
            if (OnVisibilityChanged != null)
            {
                OnVisibilityChanged(this, visible);
            }

        }
    }

    /// <summary>
    /// Updates the Collision Mesh of the Terrain Chunk
    /// </summary>
    public void UpdateCollisionMesh()
    {
        if (hasSetCollider) return;

        float sqrDistanceFromViewerToEdge = bounds.SqrDistance(viewerPosition);

        if (sqrDistanceFromViewerToEdge < detailLevels[colliderLODIndex].sqrVisibleDistThreshold)
        {
            if (!lodMeshes[colliderLODIndex].hasRequestedMesh) lodMeshes[colliderLODIndex].RequestMesh(heightMapData, meshSettings);
        }

        if (sqrDistanceFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
        {
            if (lodMeshes[colliderLODIndex].hasMesh) collider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
            hasSetCollider = true;
        }
    }

    public void SetVisible(bool isVisible)
    {
        meshObject.SetActive(isVisible);
    }

    public bool isVisible()
    {
        return meshObject.activeSelf;
    }
}

public class LODMesh
{
    public Mesh mesh;
    public bool hasRequestedMesh;
    public bool hasMesh;
    int lod;
    public event System.Action updateCallback;

    public LODMesh(int lod)
    {
        this.lod = lod;
    }

    void OnMeshDataRecieved(object meshDataObject)
    {
        mesh = ((MeshData)meshDataObject).CreateMesh();
        hasMesh = true;

        updateCallback();
    }

    public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
    {
        hasRequestedMesh = true;
        //mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, lod), OnMeshDataRecieved);
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numberOfSupportedLOD - 1)]
    public int lod;
    public float visibleDistanceThreshhold;

    public float sqrVisibleDistThreshold { get { return visibleDistanceThreshhold * visibleDistanceThreshhold; } }
}
