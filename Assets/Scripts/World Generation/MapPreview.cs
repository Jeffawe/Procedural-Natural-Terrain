using System.Collections.Generic;
using UnityEngine;

public class MapPreview : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        MeshMap,
        FallOffMap,
        BiomeMap,
        BiomeMeshMap,
        TemperatureMap,
        MoistureMap
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

    [Header("Biomes")]
    public BiomeData biomeSettings;
    public bool useBiomes;


    public Material mapMat;

    [Range(0, MeshSettings.numberOfSupportedLOD - 1)]
    public int levelOfDetailEditor;

    public bool autoUpdate;

    [Header("Biomes")]


    private List<NoiseMapData> noiseMaps = new List<NoiseMapData>();

    public void DrawTexture(Texture2D texture)
    {
        planeRenderer.sharedMaterial.mainTexture = texture;
        planeRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10;

        planeRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    public void DrawMesh(MeshData meshData)
    {
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
        float[,] temperature = null;
        float[,] moisture = null;

        if (useBiomes)
        {
            temperature = Noise.GenerateNoiseMap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings.temperatureNoiseSettings, Vector2.zero, heightMapSettings.heightOffsetVal);
            moisture = Noise.GenerateNoiseMap(meshSettings.numVerticesPerLine, meshSettings.numVerticesPerLine, heightMapSettings.moistureNoiseSettings, Vector2.zero, heightMapSettings.heightOffsetVal);

            AddOrUpdateNoiseMap(NoiseMapType.Temperature, temperature, heightMapSettings.temperatureNoiseSettings);
            AddOrUpdateNoiseMap(NoiseMapType.Moisture, moisture, heightMapSettings.moistureNoiseSettings); 
        }

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
        else if (drawMode == DrawMode.BiomeMap && useBiomes)
        {
            Texture2D texture = TextureGenerator.CreateTextureFromColorMap(BiomeGenerator.GenerateBiomes(noiseMaps, biomeSettings));
            DrawTexture(texture);
        }
        else if (drawMode == DrawMode.TemperatureMap || drawMode == DrawMode.MoistureMap)
        {
            float[,] arrayToUse = (drawMode == DrawMode.TemperatureMap) ? temperature : moisture;
            if (useBiomes)
            {
                Texture2D texture = TextureGenerator.CreateTextureFromHeightMap(new HeightMap(arrayToUse, 0, 1));
                DrawTexture(texture);
            }
            else
            {
                Texture2D texture = TextureGenerator.CreateTextureFromHeightMap(heightMap);
                DrawTexture(texture);
            }
        }
    }

    public void AddOrUpdateNoiseMap(NoiseMapType type, float[,] map, NoiseSettings settings)
    {
        // Find existing map
        var existingMap = noiseMaps.Find(x => x.mapType == type);

        if (existingMap != null)
        {
            // Update existing map
            existingMap.noiseMap = map;
            existingMap.settings = settings;
        }
        else
        {
            // Add new map
            noiseMaps.Add(new NoiseMapData(type, map, settings));
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
