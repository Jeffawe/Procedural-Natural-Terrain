using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        MeshMap,
        FallOffMap,
        BiomeMap
    }

    public DrawMode drawMode;

    [Header("Preview Renderer")]
    public Renderer planeRenderer;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    [Header("Setting Assets")]
    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureSettings;

    public Material mapMat;


    [Range(0, MeshSettings.numberOfSupportedLOD - 1)]
    public int levelOfDetailEditor;

    public bool autoUpdate;

    public void DrawTexture(Texture2D texture)
    {
        planeRenderer.sharedMaterial.mainTexture = texture;
        planeRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10;

        planeRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData){
        meshFilter.sharedMesh = meshData.CreateMesh();

        planeRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    /// <summary>
    /// Draws the map in the Editor when not in play mode (For testing purposes)
    /// </summary>
    public void DrawMapInEditor()
    {
        textureSettings.ApplyToMaterial(mapMat);
        textureSettings.UpdateMeshHeights(mapMat, heightMapSettings.minHeight, heightMapSettings.maxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings, Vector2.zero);

        if (drawMode == DrawMode.NoiseMap)
        {
            Texture2D texture = TextureGenerator.CreateTextureFromHeightMap(heightMap);
            DrawTexture(texture);
        }
        else if (drawMode == DrawMode.MeshMap)
        {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, levelOfDetailEditor));
        }
        else if (drawMode == DrawMode.FallOffMap)
        {
            Texture2D texture = TextureGenerator.CreateTextureFromHeightMap(new HeightMap(FallOffGenerator.GenerateFallOffMap(meshSettings.numVerticesPerLine, heightMapSettings.fallOffCurve), 0, 1));
            DrawTexture(texture);
        }
    }

    /// <summary>
    /// Redraws things when any value changes
    /// </summary>
    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
            textureSettings.ApplyToMaterial(mapMat);
        }
    }

    void OnValidate()
    {
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }

        if (textureSettings != null)
        {
            textureSettings.OnValuesUpdated -= OnValuesUpdated;
            textureSettings.OnValuesUpdated += OnValuesUpdated;
        }
    }
}
