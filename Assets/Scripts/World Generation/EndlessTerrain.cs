using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MapGenerator))]
public class EndlessTerrain : MonoBehaviour
{
    const float viewerThresholdForUpdate = 25f;
    const float sqrViewerThreshold = viewerThresholdForUpdate * viewerThresholdForUpdate;

    public TerrainData terrainData;

    public Transform viewer;
    public static float maxViewDistance;
    public static Vector2 viewPos;
    Vector2 viewPosOld;
    public Material mapMat;
    public LODInfo[] detailLevels;

    int chunkSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksLastUpdate = new List<TerrainChunk>();

    static MapGenerator mapGenerator;

    void Start(){
        mapGenerator = GetComponent<MapGenerator>();

        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshhold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks(){
        for (int i = 0; i < terrainChunksLastUpdate.Count; i++)
        {
            terrainChunksLastUpdate[i].SetVisible(false);
        }

        terrainChunksLastUpdate.Clear();

        int currentChunkX = Mathf.RoundToInt(viewPos.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewPos.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDistance; yOffset < chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset < chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if(terrainChunkDict.ContainsKey(viewedChunkCoord)){
                    terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                }else{
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMat));
                }
            }
        }
    }

    void Update(){
        viewPos = new Vector2(viewer.position.x, viewer.position.z) / terrainData.uniformScale;

        if((viewPosOld - viewPos).sqrMagnitude > sqrViewerThreshold)
        {
            viewPosOld = viewPos;
            UpdateVisibleChunks();
        }

    }

    public class TerrainChunk{
        GameObject meshObject;
        Vector2 pos;
        Bounds bounds;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        LODMesh collisionLODMesh;

        MeshRenderer renderer;
        MeshFilter filter;
        MeshCollider collider;

        MapData mapData;
        bool mapDataRecieved;
        int previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material mat){
            this.detailLevels = detailLevels;

            pos = coord * size;
            bounds  = new Bounds(pos, Vector2.one * size);
            Vector3 pos3D = new Vector3(pos.x, 0, pos.y);
            meshObject = new GameObject("Terrain Object");
            renderer = meshObject.AddComponent<MeshRenderer>();
            filter = meshObject.AddComponent<MeshFilter>();
            collider = meshObject.AddComponent<MeshCollider>();
            renderer.material = mat;
            meshObject.transform.position = pos3D * mapGenerator.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGenerator.terrainData.uniformScale;

            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGenerator.RequestMapData(pos, OnMapDataRecieved);
        }

        void OnMapDataRecieved(MapData mapData)
        {
            this.mapData = mapData;
            mapDataRecieved = true;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk(){
            if (!mapDataRecieved) return;

            float viewDistFromNearestEdge = MathF.Sqrt(bounds.SqrDistance(viewPos));
            bool visible = viewDistFromNearestEdge <= maxViewDistance;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if(viewDistFromNearestEdge > detailLevels[i].visibleDistanceThreshhold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if(lodIndex != previousLODIndex)
                {
                    LODMesh lODMesh = lodMeshes[lodIndex];
                    if (lODMesh.hasMesh)
                    {
                        filter.mesh = lODMesh.mesh;
                        previousLODIndex = lodIndex;
                    }else if(!lODMesh.hasRequestedMesh){
                        lODMesh.RequestMesh(mapData);
                    }
                }

                if(lodIndex == 0)
                {
                    if (collisionLODMesh.hasMesh)
                    {
                        collider.sharedMesh = collisionLODMesh.mesh;
                    }
                    else if(!collisionLODMesh.hasRequestedMesh)
                    {
                        collisionLODMesh.RequestMesh(mapData);
                    }
                }
                terrainChunksLastUpdate.Add(this);
            }

            SetVisible(visible);
        }

        public void SetVisible(bool isVisible){
            meshObject.SetActive(isVisible);
        }

        public bool isVisible(){
            return meshObject.activeSelf;
        }
    }

    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataRecieved(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataRecieved);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshhold;
        public bool useForCollider;
    }

}
