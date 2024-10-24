using UnityEngine;

static public class Noise
{
    public enum NormalizeMode
    {
        Local,
        Global
    }

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseData noiseData, Vector2 offset, float heightOffsetVal)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(noiseData.seed);
        Vector2[] octaveOffset = new Vector2[noiseData.octaves];
        float amplitude = 1;
        float frequency = 1;

        float maxGlobalPossibleHeight = 0;

        for (int i = 0; i < noiseData.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;

            octaveOffset[i] = new Vector2(offsetX, offsetY);

            maxGlobalPossibleHeight += amplitude;
            amplitude *= noiseData.persistance;
        }

        noiseData.noiseScale = Mathf.Max(noiseData.noiseScale, 0.001f);

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < noiseData.octaves; i++)
                {
                    float samplex = (x - halfWidth + octaveOffset[i].x) / noiseData.noiseScale * frequency;
                    float sampley = (y - halfHeight + octaveOffset[i].y) / noiseData.noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(samplex, sampley) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= noiseData.persistance;
                    frequency *= noiseData.lacunarity;
                }

                if(noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                if(noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;

            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if(noiseData.normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxGlobalPossibleHeight * heightOffsetVal);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }

            }
        }

        return noiseMap;

    }
}
