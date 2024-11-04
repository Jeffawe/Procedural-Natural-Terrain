using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    public float meshScale = 1f;

    public const int numberOfSupportedLOD = 5;
    public const int numSupportedChunkSizes = 9;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    [Range(0, numSupportedChunkSizes - 1)]
    public int chunkSizeIndex;

    /// <summary>
    /// Number of vertices per line of a mesh rendered at the highest resolution (LOD = 0)
    /// Includes the 2 extra vertices that are excluded from the final mesh but are used in calculating normals
    /// </summary>
    public int numVerticesPerLine
    {
        get
        {
            return supportedChunkSizes[chunkSizeIndex] + 1;
        }
    }

    public float meshWorldSize
    {
        get
        {
            return (numVerticesPerLine - 3) * meshScale;
        }
    }
}
