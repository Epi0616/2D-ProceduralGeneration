using UnityEngine;

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
               // Debug.Log(Mathf.RoundToInt(Mathf.PerlinNoise((x * NoiseDensity) + seed, (y * NoiseDensity) + seed)));
                //Debug.Log(Mathf.PerlinNoise((x * NoiseDensity) + seed, (y * NoiseDensity) + seed));
            }
        }
    }
}
