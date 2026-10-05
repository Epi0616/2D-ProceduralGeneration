using UnityEngine;

public class TileVisual : MonoBehaviour
{
    public SpriteRenderer sprite;
    public TileType tileType;
    public BoxCollider2D tileCollider;
    public void SetColor(Color colour)
    {
        sprite.color = colour;
    }
    public void SetSize(float size)
    {
        transform.localScale *= size;
    }

    public void UpdateVisualFromTileData(TileData data)
    {
        tileType = data.type;
        SetColor(data.tileColour);
        sprite.sprite = data.tileSprite;
        UpdateCollision(data.isSolid);
    }

    public void UpdateCollision(bool isEnabled)
    {
        tileCollider.enabled = isEnabled;
    }
}
