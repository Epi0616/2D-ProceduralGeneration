using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/NoiseLayer")]
public class NoiseLayer : GenerationLayerSO
{
    public TileType NoiseType;
    public int[,] NoiseMap;
    public float NoiseDensity;
    public int Iterations;
    public bool Layered;
    public List<LayeredNoiseValueSet> layers;

    public virtual void GenerateNoiseMap(int width, int height, int seed)
    { 
        NoiseMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int random = UnityEngine.Random.Range(0, 100);
                if (random > NoiseDensity)
                {
                    NoiseMap[x, y] = 1;
                }
                else
                {
                    NoiseMap[x, y] = 0;
                }
            }
        }
    }

    public virtual void GenerateLayeredNoiseMap(int width, int height, int seed)
    {
        if (!Layered) { GenerateNoiseMap(width, height, seed); return; }

        NoiseMap = new int[width, height];
        float[,] totals = new float[width, height];

        for (int o  = 0; o < layers.Count; o++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float random = UnityEngine.Random.Range(0f, 100f);
                    totals[x, y] += random * layers[o].amplitude;


                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float random = totals[x, y];
                if (random > NoiseDensity)
                {
                    NoiseMap[x, y] = 1;
                }
                else
                {
                    NoiseMap[x, y] = 0;
                }
            }
        }
    }

    public virtual TileType ApplyNoise(TileType current, int value)
    {
        if (value == 1) return NoiseType;
        return current;
    }
}
[System.Serializable]
public struct LayeredNoiseValueSet
{
    public float frequency;
    public float amplitude;
}