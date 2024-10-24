using UnityEngine;
using System.Threading;
using System;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode
    {
        NoiseMap,
        MeshMap,
        FallOffMap
    }

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMat;

    public const int mapChunkSize = 239;

    [Range(0, 6)]
    public int levelOfDetailEditor;

    public DrawMode drawMode;

    float[,] fallOffMap;

    public bool autoUpdate;

    Queue<MapThreadInfo<MapData>> mapThreadInfos = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshThreadInfos = new Queue<MapThreadInfo<MeshData>>();

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
            textureData.ApplyToMaterial(terrainMat);
        }
    }

    public MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData, center + noiseData.offset, terrainData.heightOffsetVal);
        if (terrainData.useFallOff)
        {
            if(fallOffMap == null) fallOffMap = FallOffGenerator.GenerateFallOffMap(mapChunkSize + 2, terrainData.fallOffCurve);

            for (int y = 0; y < mapChunkSize + 2; y++)
            {
                for (int x = 0; x < mapChunkSize + 2; x++)
                {
                    if (terrainData.useFallOff) noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - fallOffMap[x, y]);
                }
            }
        }

        return new MapData(noiseMap);
    }

    public void DrawMapInEditor(){
        textureData.UpdateMeshHeights(terrainMat, terrainData.minHeight, terrainData.maxHeight);
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap){
            Texture2D texture = TextureGenerator.CreateTextureFromHeightMap(mapData.heightMap);
            mapDisplay.DrawMap(texture);
        }else if(drawMode == DrawMode.MeshMap){
            mapDisplay.DrawMesh(MeshGenerator.GenerateMesh(mapData.heightMap, terrainData.heightMultiplier, terrainData.meshHeightCurve, levelOfDetailEditor));
        }else if(drawMode == DrawMode.FallOffMap)
        {
            Texture2D texture = TextureGenerator.CreateTextureFromHeightMap(FallOffGenerator.GenerateFallOffMap(mapChunkSize, terrainData.fallOffCurve));
            mapDisplay.DrawMap(texture);
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        textureData.UpdateMeshHeights(terrainMat, terrainData.minHeight, terrainData.maxHeight);

        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock(mapThreadInfos)
        {
            mapThreadInfos.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData data, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(data, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData data, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateMesh(data.heightMap, terrainData.heightMultiplier, terrainData.meshHeightCurve, lod);
        lock (meshThreadInfos)
        {
            meshThreadInfos.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        if(mapThreadInfos.Count > 0)
        {
            for (int i = 0; i < mapThreadInfos.Count; i++)
            {
                MapThreadInfo<MapData> queueElement = mapThreadInfos.Dequeue();
                queueElement.callback(queueElement.parameter);   
            }
        }

        if (meshThreadInfos.Count > 0)
        {
            for (int i = 0; i < meshThreadInfos.Count; i++)
            {
                MapThreadInfo<MeshData> queueElement = meshThreadInfos.Dequeue();
                queueElement.callback(queueElement.parameter);
            }
        }
    }

    void OnValidate()
    {
        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }

        if(textureData != null)
        {
            textureData.OnValuesUpdated -= OnValuesUpdated;
            textureData.OnValuesUpdated += OnValuesUpdated;
        }
    }

    public struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }

    }
}

public struct MapData{
    public readonly float[,] heightMap;

    public MapData(float[,] heightMap){
        this.heightMap = heightMap;
    }

}
