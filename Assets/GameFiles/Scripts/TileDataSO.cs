using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/TileDataSO")]
public class TileDataSO : ScriptableObject
{
    public TileType type;
    public Color tileColour;
    public Sprite tileSprite;
    public bool isSolid;
    public bool canConvert = true;
    public bool isWall = false;

    public TileData Create()
    {
        return new TileData(type, tileColour, tileSprite, isSolid, canConvert, isWall);
    }
}
