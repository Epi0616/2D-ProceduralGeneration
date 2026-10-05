using System.Runtime.CompilerServices;
using UnityEngine;

public class WorldTile
{
    [Range(0, 1)]
    private float tileValue;
    public float TileValue
    {
        get { return tileValue; }
        set {
            tileValue = value;
            UpdateTileType();
            }
    }


    public TileType tileType;
    public Vector2 worldPos;
    public int tileID;
    public TileVisual visual;
    public float airThreshold = 0.45f;

    public WorldTile(Vector2 worldPos, TileType type, float value, int tileID)
    {
        this.worldPos = worldPos;
        this.tileType = type;
        this.tileValue = value;
        this.tileID = tileID;
    }

    private void UpdateTileType()
    {
        //Debug.Log("Updating Tile Type");
        if (tileValue <= airThreshold) { tileType = TileType.Air; visual.SetColor(new Color(1, 1, 1, 0.2f)); }
        else { tileType = TileType.Dirt; visual.SetColor(Color.saddleBrown); }
    }
}


// Update to only store type as the data can do everything else
// No longer store the visual that can be handled separately
// Somehow make generation "passes" or rules
// Generate the general height first
// 0 - 25 air
// 25 - 50 grass
// 50 - 75 dirt
// 75 - 100 dirt/stone mix
// 100+ stone
//
// Then Add Noise pass?
// Potentially remove max/min depths from the SOs and make a new class for generation that holds a type(layer) and its specific depth range?
// As you reach a transition add a chance for the layer above to bleed in as long as its solid, don't want random air holes

// Remove stranded tiles pass and single holes pass at the end still
// Try other noise types also just make your own

