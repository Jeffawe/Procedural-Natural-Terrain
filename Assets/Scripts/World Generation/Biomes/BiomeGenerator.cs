using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiomeGenerator : MonoBehaviour
{
    public static Color[,] GenerateBiomes(List<NoiseMapData> noiseMaps, BiomeData biomeData, bool checkMapSameSize = true)
    {
        float[,] firstMap = noiseMaps[0].noiseMap;
        int width = firstMap.GetLength(0);
        int height = firstMap.GetLength(1);

        Color[,] colorMap = new Color[width, height];
        BiomeType[,] biomeNames = new BiomeType[width, height]; // Keep track of biome names

        // First pass: Generate initial biome colors
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                foreach (var biome in biomeData.biomeSettings)
                {
                    if (IsBiomeAtPosition(biome, noiseMaps, x, y))
                    {
                        colorMap[x, y] = biome.color;
                        biomeNames[x, y] = biome.biomeType;
                        break;
                    }
                }
            }
        }

        // Second pass: Apply transition rules
        Color[,] finalColorMap = ApplyTransitionRules(colorMap, biomeNames, biomeData);

        return finalColorMap;
    }

    private static bool IsBiomeAtPosition(BiomeSettings biome, List<NoiseMapData> noiseMaps, int x, int y)
    {
        foreach (var condition in biome.conditions)
        {
            var noiseMap = noiseMaps.Find(n => n.mapType == condition.mapType)?.noiseMap;
            if (noiseMap == null) continue;

            float value = noiseMap[x, y];
            if (value < condition.minValue || value > condition.maxValue)
                return false;
        }

        return true;
    }

    private static Color[,] ApplyTransitionRules(Color[,] colorMap, BiomeType[,] biomeNames, BiomeData biomeData)
    {
        int width = colorMap.GetLength(0);
        int height = colorMap.GetLength(1);
        Color[,] newColorMap = new Color[width, height];
        Array.Copy(colorMap, newColorMap, colorMap.Length);

        if (biomeData.transitionRules == null) return colorMap;

        // Create a list to store all positions that need to be changed
        List<(int x, int y, Color color)> changesToApply = new List<(int x, int y, Color color)>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                foreach (var rule in biomeData.transitionRules)
                {
                    if (biomeNames[x, y] == rule.sourceBiome)
                    {
                        // Check if this position has the target biome within the radius
                        if (HasBiomeInRadius(biomeNames, x, y, width, height, rule.targetBiome, 1))
                        {
                            // Find replacement biome color
                            var replacementBiome = biomeData.biomeSettings.FirstOrDefault(b => b.biomeType == rule.replacementBiome);
                            if (replacementBiome != null)
                            {
                                // Add all positions within radius to the change list
                                AddPositionsInRadius(x, y, width, height, rule.transitionRadius,
                                    replacementBiome.color, biomeNames, rule.sourceBiome, changesToApply);
                            }
                        }
                    }
                }
            }
        }

        // Apply all changes at once
        foreach (var change in changesToApply)
        {
            newColorMap[change.x, change.y] = change.color;
        }

        return newColorMap;
    }

    private static void AddPositionsInRadius(int centerX, int centerY, int width, int height,
        int radius, Color newColor, BiomeType[,] biomeNames, BiomeType sourceBiomeName,
        List<(int x, int y, Color color)> changes)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                int newX = centerX + x;
                int newY = centerY + y;

                // Check if position is within bounds and is the source biome
                if (newX >= 0 && newX < width && newY >= 0 && newY < height &&
                    biomeNames[newX, newY] == sourceBiomeName)
                {
                    // Optional: Check if within circular radius instead of square
                    float distance = Mathf.Sqrt(x * x + y * y);
                    if (distance <= radius)
                    {
                        changes.Add((newX, newY, newColor));
                    }
                }
            }
        }
    }

    private static bool HasBiomeInRadius(BiomeType[,] biomeNames, int centerX, int centerY,
        int width, int height, BiomeType targetBiomeName, int checkRadius)
    {
        for (int x = -checkRadius; x <= checkRadius; x++)
        {
            for (int y = -checkRadius; y <= checkRadius; y++)
            {
                int newX = centerX + x;
                int newY = centerY + y;

                if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                {
                    if (biomeNames[newX, newY] == targetBiomeName)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /*
    private static Color[,] ApplyTransitionRules(Color[,] colorMap, BiomeType[,] biomeNames, BiomeData biomeData)
    {
        int width = colorMap.GetLength(0);
        int height = colorMap.GetLength(1);
        Color[,] newColorMap = new Color[width, height];
        Array.Copy(colorMap, newColorMap, colorMap.Length);

        if(biomeData.transitionRules == null) return colorMap;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                foreach (var rule in biomeData.transitionRules)
                {
                    if (biomeNames[x, y] == rule.sourceBiome)
                    {
                        // Check neighbors
                        if (HasNeighborBiome(biomeNames, x, y, width, height, rule.targetBiome))
                        {
                            // Find replacement biome color
                            var replacementBiome = biomeData.biomeSettings.FirstOrDefault(b => b.biomeType == rule.replacementBiome);
                            if (replacementBiome != null)
                            {
                                newColorMap[x, y] = replacementBiome.color;
                            }
                        }
                    }
                }
            }
        }

        return newColorMap;
    }

    private static bool HasNeighborBiome(BiomeType[,] biomeNames, int x, int y, int width, int height, BiomeType targetBiomeName)
    {
        // Check cardinal directions
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };

        for (int i = 0; i < 4; i++)
        {
            int newX = x + dx[i];
            int newY = y + dy[i];

            if (newX >= 0 && newX < width && newY >= 0 && newY < height)
            {
                if (biomeNames[newX, newY] == targetBiomeName)
                {
                    return true;
                }
            }
        }

        return false;
    }
    */
}
