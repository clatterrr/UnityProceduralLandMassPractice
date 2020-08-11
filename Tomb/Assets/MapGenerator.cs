using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { noiseMap,ColourMap,Mesh};
    public DrawMode drawMode;
    public float noiseScale;

    public int octaves;
    [Range(0,1)]
    public float persistance;
    public float lacunarity;
    public bool autoUpdate;

    public Vector2 offsets;
    public int seed;
    public float meshHeightMul;
    public AnimationCurve meshHeightCurve;
    public const int mapChunkSize = 241;
    [Range(0, 6)]
    public int levelOfDetail;

    public TerrainType[] regions;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();

    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData();
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.noiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMul, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
    }

    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback);
        };
        new Thread(threadStart).Start();
    }
    void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();
        lock(mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }
    private void Update()
    {
        if(mapDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0;i < mapDataThreadInfoQueue.Count;i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    public MapData GenerateMapData()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, noiseScale,octaves,persistance,lacunarity,offsets,seed);

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for(int y = 0;y < mapChunkSize; y++)
        {
            for(int x = 0;x < mapChunkSize; x++)
            {
                float currentHeight = noiseMap[x, y];
                for(int i = 0;i < regions.Length;i++)
                {
                    if(currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                        break;
                    }
                }
            }
          
        }
        return new MapData(noiseMap, colourMap);

        
    }

    private void OnValidate()
    {
        if (lacunarity < 1)
            lacunarity = 1;
    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;
        public MapThreadInfo(Action<T> callback,T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}
public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;
    public MapData (float[,] heightMap,Color[] colourMap)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}