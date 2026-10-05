using UnityEngine;
[CreateAssetMenu(menuName = "ScriptableObjects/GenerationOptionsSO")]
public class GenerationOptionsSO : ScriptableObject 
{
    public int width = 1, height = 1;
    public float cellSize = 1;
    //public float NoiseDensity = 50;
    //public int Iterations;
    public float Delay;
    public int RemoveThreshold, AddThreshold;
    public TileType initialTileType;
}
