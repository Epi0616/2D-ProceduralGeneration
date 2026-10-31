using UnityEngine;

public class TileData
{
    public TileType type;
    public Color tileColour;
    public Sprite tileSprite;
    public bool isSolid;
    public bool canConvert;
    public bool isWall;
    public TileData(TileType type, Color tileColour, Sprite tileSprite, bool isSolid, bool canConvert, bool isWall)
    {
        this.type = type;
        this.tileColour = tileColour;
        this.tileSprite = tileSprite;
        this.isSolid = isSolid;
        this.canConvert = canConvert;
        this.isWall = isWall;
    }
}
