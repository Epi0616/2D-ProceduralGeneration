using UnityEngine;

public class TileData
{
    public TileType type;
    public Color tileColour;
    public Sprite tileSprite;
    public bool isSolid;
    public int minDepth;
    public int maxDepth;
    public TileData(TileType type, Color tileColour, Sprite tileSprite, bool isSolid, int minDepth, int maxDepth)
    {
        this.type = type;
        this.tileColour = tileColour;
        this.tileSprite = tileSprite;
        this.isSolid = isSolid;
        this.minDepth = minDepth;
        this.maxDepth = maxDepth;
    }
}
