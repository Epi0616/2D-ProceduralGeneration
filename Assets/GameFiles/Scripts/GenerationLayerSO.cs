using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GenerationLayerSO : ScriptableObject
{
    [Range(0f, 1f)]
    public float MinY, MaxY;
    public virtual TileType GenerateTile() { return TileType.Air; }
    public virtual TileType GenerateTile(float NormalizedY) { return TileType.Air; }
    public virtual TileType GenerateTile(TileType current, int value) { return TileType.Air; }
}
