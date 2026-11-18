using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "ScriptableObjects/PerlinNoiseLayer")]
public class PerlinNoiseLayer : NoiseLayer
{
    public override void GenerateNoiseMap(int width, int height, int seed)
    {
        NoiseMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                NoiseMap[x, y] = Mathf.RoundToInt(Mathf.PerlinNoise((x * NoiseDensity) + seed, (y * NoiseDensity) + seed));
            }
        }
    }
    public override void GenerateLayeredNoiseMap(int width, int height, int seed)
    {
        if (!Layered) { GenerateNoiseMap(width, height, seed); return; }

        NoiseMap = new int[width, height];
        float[,] totals = new float[width, height];
        float [,] maxValues = new float[width, height];
        for (int o = 0; o < layers.Count; o++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float random = UnityEngine.Random.Range(0f, 100f);
                    totals[x, y] += Mathf.PerlinNoise((x * NoiseDensity * layers[0].frequency) + seed, (y * NoiseDensity * layers[0].frequency) + seed) * layers[o].amplitude;
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
