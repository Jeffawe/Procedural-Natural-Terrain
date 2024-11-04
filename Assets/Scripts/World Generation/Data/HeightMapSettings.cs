using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;

    public float heightMultiplier;
    public AnimationCurve heightCurve;

    public bool useFallOff;

    [Tooltip("Curve for controlling the Falloff when useFallOff is on")]
    public AnimationCurve fallOffCurve;

    [Tooltip("Used to Fix height offsets when Normalize Mode is in Global")]
    [Range(0.5f, 2)]
    public float heightOffsetVal = 1;

    public float minHeight { get { return heightMultiplier * heightCurve.Evaluate(0); } }

    public float maxHeight { get { return heightMultiplier * heightCurve.Evaluate(1); } }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();

        base.OnValidate();
    }
#endif
}
