using System.Linq;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    const int textureSize = 512;
    const TextureFormat textureFormat = TextureFormat.RGB565;

    public Layer[] layers;

    float savedMinHeight;
    float savedMaxHeight; 

    public void ApplyToMaterial(Material mat)
    {
        mat.SetInt("layerCount", layers.Length);
        mat.SetColorArray("baseColors", layers.Select(x => (x.tint)).ToArray());
        mat.SetFloatArray("baseStartHeights", layers.Select(x => (x.startHeight)).ToArray());
        mat.SetFloatArray("baseBlends", layers.Select(x => (x.blendStrength)).ToArray());
        mat.SetFloatArray("baseColorStrengths", layers.Select(x => (x.tintStrength)).ToArray());
        mat.SetFloatArray("baseTextureScales", layers.Select(x => (x.textureScale)).ToArray());

        Texture2DArray texture2DArray = GenerateTextureArrray(layers.Select(x => x.texture).ToArray());
        mat.SetTexture("baseTextures", texture2DArray);

        UpdateMeshHeights(mat, savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material mat, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        mat.SetFloat("minHeight", minHeight);
        mat.SetFloat("maxHeight", maxHeight);
    }

    Texture2DArray GenerateTextureArrray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(textureSize, textureSize, textures.Length, textureFormat, true);

        for (int i = 0; i < textures.Length; i++)
        {
            // Check if the current texture is not null before setting pixels
            if (textures[i] != null)
            {
                textureArray.SetPixels(textures[i].GetPixels(), i);
            }
            else
            {
                Debug.LogError($"Texture at index {i} is null. Please ensure all textures are assigned.");
            }
        }

        textureArray.Apply();
        return textureArray;
    }

    public Texture2D PackFloatArrayIntoTexture(float[] data, int maxLayerCount)
    {
        int width = maxLayerCount;  // Assuming the array is of size maxLayerCount
        Texture2D texture = new Texture2D(width, 1, TextureFormat.RFloat, false);

        for (int i = 0; i < width; i++)
        {
            float value = i < data.Length ? data[i] : 0;
            texture.SetPixel(i, 0, new Color(value, 0, 0, 0));  // Store the value in the R channel
        }

        texture.Apply();
        return texture;
    }

    public Texture2D PackFloat3ArrayIntoTexture(Vector3[] data, int maxLayerCount)
    {
        int width = maxLayerCount;
        Texture2D texture = new Texture2D(width, 1, TextureFormat.RGBAFloat, false);

        for (int i = 0; i < width; i++)
        {
            Vector3 color = i < data.Length ? data[i] : new Vector3(0, 0, 0);
            texture.SetPixel(i, 0, new Color(color.x, color.y, color.z, 0));  // Store the float3 as RGB
        }

        texture.Apply();
        return texture;
    }


    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;

        [Range(0f, 1f)]
        public float tintStrength;

        [Range(0f, 1f)]
        public float startHeight;

        [Range(0f, 1f)]
        public float blendStrength;
        public float textureScale;

    }
}
