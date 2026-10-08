using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/PerlinNoiseLayer")]
public class PerlinNoiseLayer : NoiseLayer
{
    public override void GenerateNoiseMap(int width, int height)
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
}
