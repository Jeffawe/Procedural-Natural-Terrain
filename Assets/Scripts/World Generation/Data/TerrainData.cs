using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public bool useFallOff;

    [Tooltip("Curve for controlling the Falloff when useFallOff is on")]
    public AnimationCurve fallOffCurve;

    public float uniformScale = 1f;

    public float heightMultiplier;
    public AnimationCurve meshHeightCurve;

    [Tooltip("Used to Fix height offsets when Normalize Mode is in Global")]
    [Range(0.5f, 2)]
    public float heightOffsetVal = 1;

    public float minHeight { get { return uniformScale * heightMultiplier * meshHeightCurve.Evaluate(0); } }

    public float maxHeight { get { return uniformScale * heightMultiplier * meshHeightCurve.Evaluate(1); } }
}
