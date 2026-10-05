using System.Collections.Generic;
using UnityEngine;

public enum TileType { Dirt, Stone, Air, Grass, Gold, Silver, Copper }

public class TileDictionary : MonoBehaviour
{
    public static TileDictionary Instance;
    private readonly Dictionary<TileType, TileData> database = new Dictionary<TileType, TileData>();

    [SerializeField] private TileDataSO[] TileDataScriptableObjects;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {          
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        PopulateDictionary();
    }

    private void PopulateDictionary()
    {
        for (int i = 0; i < TileDataScriptableObjects.Length; i++)
        {
            database.Add(TileDataScriptableObjects[i].type, TileDataScriptableObjects[i].Create());
        }
    }    

    public TileData FetchTileDataByType(TileType type)
    {
        if (database.ContainsKey(type)) { return  database[type]; }

        Debug.LogError("Invalid Tile Data Fetch");
        return null;
    }
}
