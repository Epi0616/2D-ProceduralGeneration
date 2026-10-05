using UnityEngine;

public class Tile 
{
    public TileType type;
    public Vector2Int GridPos;
    public Vector2 WorldPos;

    public Tile(TileType type, Vector2Int gridPos, Vector2 worldPos)
    {
        this.type = type;
        GridPos = gridPos;
        WorldPos = worldPos;
    }
    public Tile(TileType type)
    {
        this.type = type;
    }
    public Tile() { }
}
