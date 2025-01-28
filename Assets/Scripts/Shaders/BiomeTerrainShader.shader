Shader "Custom/BiomeTerrainShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 8;
        const static float epsilon = 1e-4;
        const static int maxBiomeCount = 10;
        
        int layerCount;
        float3 baseColors[maxLayerCount];
        float baseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColorStrengths[maxLayerCount];
        float baseTextureScales[maxLayerCount];

        float minHeight;
        float maxHeight;

        int biomeCount;
        float3 biomeColors[maxBiomeCount]; // Assuming a maximum of 10 biomes for now
        int biomeTextureCount[maxBiomeCount]; // Texture count for each biome
        float biomeStartHeights[maxBiomeCount * maxLayerCount];
        float biomeBlends[maxBiomeCount * maxLayerCount];
        float biomeColorStrengths[maxBiomeCount * maxLayerCount];
        float biomeTextureScales[maxBiomeCount * maxLayerCount];
        float3 biomeBaseColors[maxBiomeCount * maxLayerCount];

        sampler2D _BiomeTexture;

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        float InverseLerp(float a, float b, float value){
            return saturate((value - a)/(b - a));
        }

        // Function to sample the biome texture to get the biome index
        int GetBiomeIndex(float2 uv)
        {
            // Sample the texture and convert the color to a biome index
            float4 colorSample = tex2D(_BiomeTexture, uv);
            
            // For simplicity, we assume each color corresponds to a biome index
            for (int i = 0; i < maxBiomeCount; i++)
            {
                if (length(biomeBaseColors[i] - colorSample.rgb) < epsilon)
                {
                    return i;
                }
            }
            return 0; // Default biome index if no match
        }

        float3 TriPlanar(float3 worldPos, float scale, float3 blendAxis, int textureIndex){
            float3 scaledWorldPos = worldPos / scale;

            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxis.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxis.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxis.z;

            return xProjection + yProjection + zProjection;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Get UV coordinates for the biome texture based on world position
            float2 biomeUV = float2(IN.worldPos.x, IN.worldPos.z); // Scale UV if needed

            int biomeIndex = GetBiomeIndex(biomeUV);

            // Calculate height percent for biome layer selection
            float heightPercent = InverseLerp(minHeight, maxHeight, IN.worldPos.y);
            float3 blendAxis = abs(IN.worldNormal);
            blendAxis /= blendAxis.x + blendAxis.y + blendAxis.z;

            // Loop through layers for the current biome
            for (int layer = 0; layer < maxLayerCount; layer++)
            {
                int index = biomeIndex * maxLayerCount + layer;

                float drawStrength = InverseLerp(-biomeBlends[index]/2 - epsilon, 
                                                 biomeBlends[index]/2, 
                                                 heightPercent - biomeStartHeights[index]);

                float3 baseColor = biomeBaseColors[index] * biomeColorStrengths[index];
                float3 textureColor = TriPlanar(IN.worldPos, biomeTextureScales[index], blendAxis, layer) * (1 - biomeColorStrengths[index]);
                
                o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
            }
        }

        // void surf (Input IN, inout SurfaceOutputStandard o)
        // {
        //     float heightPercent = InverseLerp(minHeight, maxHeight, IN.worldPos.y);
        //     float3 blendAxis = abs(IN.worldNormal);
        //     blendAxis /= blendAxis.x + blendAxis.y + blendAxis.z;
        //     for (int i = 0; i < layerCount; i++){
        //         float drawStrength = InverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]);

        //         float3 baseColor = baseColors[i] * baseColorStrengths[i]; 
        //         float3 textureColor = TriPlanar(IN.worldPos, baseTextureScales[i], blendAxis, i) * (1 - baseColorStrengths[i]);
        //         o.Albedo = o.Albedo * (1 - drawStrength) + (baseColor + textureColor) * drawStrength;
        //     }


        //     o.Albedo = xProjection + yProjection + zProjection;

            //o.Albedo = float3(0,1,0);
        //}
        ENDCG
    }
    FallBack "Diffuse"
}
