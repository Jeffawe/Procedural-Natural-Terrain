using UnityEngine;

[CreateAssetMenu()]
public class BiomeSettings : UpdatableData
{
    public float temperature;
    public float moisture;
    public TextureData textureSettings;
    public AnimationCurve blendCurve;
}
