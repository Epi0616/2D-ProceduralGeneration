using UnityEngine;

public class GenerationOptions
{
    public int Iterations, width = 1, height = 1;
    public float cellSize = 1;
    public float noiseDensity = 50;
    public float Delay;
    public int RemoveThreshold, AddThreshold;
    public int HeightVariance = 1;

    public GenerationOptions(int width, int height, float cellSize, float noiseDensity, int Iterations, float Delay, int Remove, int Add, int heightVariance)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.noiseDensity = noiseDensity;
        this.Iterations = Iterations;
        this.Delay = Delay;
        this.RemoveThreshold = Remove;
        this.AddThreshold = Add;
        HeightVariance = heightVariance;
    }
}
