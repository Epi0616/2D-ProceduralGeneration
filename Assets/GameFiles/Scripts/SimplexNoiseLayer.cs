using UnityEngine;
using SimplexNoise;
using Unity.Mathematics;

[CreateAssetMenu(menuName = "ScriptableObjects/SimplexNoiseLayer")]
public class SimplexNoiseLayer : NoiseLayer
{
    public override void GenerateNoiseMap(int width, int height, int seed)
    {
        NoiseMap = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //NoiseMap[x, y] = Mathf.RoundToInt(SimplexMap[math.clamp(x + seed, 0, width), math.clamp(y + seed, 0, height)]);
                NoiseMap[x, y] = Mathf.RoundToInt((Noise.CalcPixel2D(x, y, NoiseDensity) / 256));
                //Debug.Log(NoiseMap[x, y]);               
            }
        }
        
    }
    public override void GenerateLayeredNoiseMap(int width, int height, int seed)
    {
        if (!Layered) { GenerateNoiseMap(width, height, seed); return; }

        NoiseMap = new int[width, height];
        float[,] totals = new float[width, height];
        float[,] maxValues = new float[width, height];
        for (int o = 0; o < layers.Count; o++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float random = UnityEngine.Random.Range(0f, 100f);
                    totals[x, y] += (Noise.CalcPixel2D(Mathf.RoundToInt(x * layers[o].frequency), Mathf.RoundToInt(y * layers[o].frequency), NoiseDensity) / 256) * layers[o].amplitude;
                    maxValues[x, y] += layers[o].amplitude;

                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                NoiseMap[x, y] = Mathf.RoundToInt(totals[x, y] / maxValues[x, y]);
            }
        }
    }
}
