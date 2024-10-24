using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public Noise.NormalizeMode normalizeMode;

    public int octaves, seed;
    public float noiseScale, lacunarity;

    [Range(0, 1)]
    public float persistance;

    public Vector2 offset;

    protected override void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        if (octaves < 0)
        {
            octaves = 1;
        }

        base.OnValidate();
    }
}
