using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/GenerationLayer")]
public class GenerationLayer : GenerationLayerSO
{
    public List<TileGenerationData> TilesInLayer;
    public TileType WallTile;
    public override TileType GenerateTile(float NormalizedY)
    {
        return SelectRandomTile();
    }
    public override TileType GenerateWallTile(float NormalizedY)
    {
        return WallTile;
    }
    private TileType SelectRandomTile()
    {
        // Add all weightings
        int totalWeight = 0;
        foreach (TileGenerationData data in TilesInLayer)
        {
            totalWeight += data.weight;
        }
        // Generate a random 0 - 1 value
        float random = Random.value * totalWeight;

        foreach (TileGenerationData data in TilesInLayer)
        {
            random -= data.weight;
            if (random <= 0) { return data.TileType; }
        }

        return TilesInLayer[0].TileType;
    }
}
