using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/TransitionLayer")]
public class TransitionLayer : GenerationLayerSO
{
    public GenerationLayer FromLayer;
    public GenerationLayer ToLayer;

    public override TileType GenerateTile(float NormalizedY)
    {
        // Where within the min/max range is the given tile's Y
        float t = Mathf.InverseLerp(MinY, MaxY, NormalizedY);
        t = Mathf.SmoothStep(0f, 1f, t);
        
        if (Random.value < t)
        {
            return FromLayer.GenerateTile(NormalizedY);
        }
        return ToLayer.GenerateTile(NormalizedY);
    }

    public override TileType GenerateWallTile(float NormalizedY)
    {
        float t = Mathf.InverseLerp(MinY, MaxY, NormalizedY);
        t = Mathf.SmoothStep(0f, 1f, t);

        if (Random.value < t)
        {
            return FromLayer.GenerateWallTile(NormalizedY);
        }
        return ToLayer.GenerateWallTile(NormalizedY);
    }
}

// The further through the min/max range the higher t is
// meaning its more likely to generate a tile from the previous layer
