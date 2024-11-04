using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TerrainUtils;

[RequireComponent(typeof(ThreadedDataRequester))]
public class EndlessTerrain : MonoBehaviour
{
    const float viewerThresholdForUpdate = 25f;
    const float sqrViewerThreshold = viewerThresholdForUpdate * viewerThresholdForUpdate;

    public int colliderLODIndex;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;

    public Transform viewer;

    Vector2 viewPos;
    Vector2 viewPosOld;
    public Material mapMat;
    public LODInfo[] detailLevels;

    float meshWorldSize;
    int chunksVisibleInViewDistance;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    public List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    void Start(){
        float maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshhold;
        meshWorldSize = meshSettings.meshWorldSize;
        chunksVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / meshWorldSize);

        UpdateVisibleChunks();
    }

    /// <summary>
    /// Updates all visible Terrain Chunks
    /// </summary>
    void UpdateVisibleChunks(){
        HashSet<Vector2> alreadyUpdatedChunkCOORD = new HashSet<Vector2>();
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCOORD.Add(visibleTerrainChunks[i].coordinate);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }


        int currentChunkX = Mathf.RoundToInt(viewPos.x / meshWorldSize);
        int currentChunkY = Mathf.RoundToInt(viewPos.y / meshWorldSize);

        for (int yOffset = -chunksVisibleInViewDistance; yOffset < chunksVisibleInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksVisibleInViewDistance; xOffset < chunksVisibleInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if (!alreadyUpdatedChunkCOORD.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMat);
                        terrainChunkDict.Add(viewedChunkCoord, newChunk);
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Subscribable function to change visibility of terrain chunk
    /// </summary>
    /// <param name="terrainChunk">The terrain chunk</param>
    /// <param name="isVisible">If it is visible or not</param>
    void OnTerrainChunkVisibilityChanged(TerrainChunk terrainChunk, bool isVisible)
    {
        if(isVisible)
        {
            visibleTerrainChunks.Add(terrainChunk);
        }
        else
        {
            visibleTerrainChunks.Remove(terrainChunk);
        }
    }

    void Update(){
        viewPos = new Vector2(viewer.position.x, viewer.position.z);

        if((viewPosOld - viewPos).sqrMagnitude > sqrViewerThreshold)
        {
            viewPosOld = viewPos;
            UpdateVisibleChunks();
        }

        if(viewPos != viewPosOld)
        {
            foreach (TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }
    }
}
