using UnityEngine;

[System.Serializable]
public class BiomeTransitionRule
{
    public BiomeType sourceBiome;
    public BiomeType targetBiome;
    public BiomeType replacementBiome;
    [Tooltip("How many pixels out to replace")]
    public int transitionRadius;
}

[System.Serializable]
public class BiomeParameter
{
    public NoiseMapType mapType;  // e.g., "Temperature", "Moisture", "Elevation"
    [Range(0f, 1f)] public float minValue;
    [Range(0f, 1f)] public float maxValue;
}

public enum NoiseMapType
{
    Temperature,
    Moisture
}

public enum BiomeType
{
    Desert,
    Forests,
    Snow,
    Grasslands,
    Mountains
}

[System.Serializable]
public class NoiseMapData
{
    public NoiseMapType mapType;
    public float[,] noiseMap;
    public NoiseSettings settings; // Your noise settings for this map

    public NoiseMapData(NoiseMapType mapType, float[,] noiseMap, NoiseSettings noiseSettings)
    {
        this.mapType = mapType;
        this.noiseMap = noiseMap;
        this.settings = noiseSettings;
    }
}

