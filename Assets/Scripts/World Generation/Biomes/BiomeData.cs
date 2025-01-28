using UnityEngine;

[CreateAssetMenu(menuName = "Procedural System/Biome Data")]
public class BiomeData : UpdatableData
{
    public BiomeSettings[] biomeSettings;
    public BiomeTransitionRule[] transitionRules;
}

[System.Serializable]
public class BiomeSettings
{
    public string biomeName;
    public BiomeType biomeType;
    public BiomeTextureLayer textureLayers;
    public Color color;  // For now, we'll keep using grayscale
    public BiomeParameter[] conditions;
}