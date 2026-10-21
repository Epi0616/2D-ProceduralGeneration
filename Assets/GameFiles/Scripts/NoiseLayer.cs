using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/NoiseLayer")]
public class NoiseLayer : GenerationLayerSO
{
    public TileType NoiseType;
    public int[,] NoiseMap;
    public float NoiseDensity;
    public int Iterations;

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

    public virtual TileType ApplyNoise(TileType current, int value)
    {
        if (value == 1) return NoiseType;
        return current;
    }
}
