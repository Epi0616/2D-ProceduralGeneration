using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

using Random = UnityEngine.Random;

public class World : MonoBehaviour
{
    public Tile[,] worldTiles;
    public TileVisual[,] worldVisuals;
    public GenerationOptionsSO generationOptions;
    [SerializeField] private GameObject tilePrefab;
    public Transform startPosition;
    private Vector2 startPoint;

    [SerializeField] private List<GenerationLayerSO> baseLayers;
    [SerializeField] private List<NoiseLayer> noiseLayers;
    [SerializeField] private List<OreLayer> oreLayers;
    private int[] surfaceHeight;

    

    public int Seed;
    // World 0,0 is Bottom Left of the Grid


    private void Start()
    {
        if (Seed == 0)
        {
            Seed = Random.Range(0, 1000);
        }

        
        // Set the Start Point for generation accounting for cellsize
        startPoint = startPosition.transform.position;
        startPoint.x += generationOptions.cellSize / 2;
        startPoint.y += generationOptions.cellSize / 2;

        // Initialize the World Array and fill it with a tile Type
        worldTiles = new Tile[generationOptions.width, generationOptions.height];
        worldVisuals = new TileVisual[generationOptions.width, generationOptions.height];
        PopulateInitialWorld(generationOptions.initialTileType);

        surfaceHeight = new int[generationOptions.width];
        GenerateHeightMap();

        StartCoroutine(GenerateWorld());
    }

    // Main Coroutine for Generation Steps
    private IEnumerator GenerateWorld()
    {
        yield return new WaitForSeconds(2);

        foreach (GenerationLayerSO layer in baseLayers)
        {         
            Debug.Log("Generating Layer: " +  layer.name);
            GenerateLayer(layer);
            yield return new WaitForSeconds(generationOptions.Delay);
        }

        foreach (NoiseLayer layer in noiseLayers)
        {
            Debug.Log("Generating Noise Layer: " + layer.name);
            if (layer.Layered)
            {
                layer.GenerateLayeredNoiseMap(generationOptions.width, generationOptions.height, Seed);
            }
            else
            {
                layer.GenerateNoiseMap(generationOptions.width, generationOptions.height, Seed);
            }            
            GenerateNoiseLayer(layer);
            for (int i = 0; i < layer.Iterations; i++)
            {
                Debug.Log("Noise Layer: " + layer.name + " Iteration: " + i);
                IterateAutomata(generationOptions.AddThreshold, generationOptions.RemoveThreshold, layer);
                yield return new WaitForSeconds(generationOptions.Delay);
            }
            yield return new WaitForSeconds(generationOptions.Delay);
        }

        foreach (OreLayer layer in oreLayers)
        {
            Debug.Log("Populating Ore Layer: " + layer.name);
            ManageOreGenerationForLayer(layer);
            yield return new WaitForSeconds(generationOptions.Delay);
        }

        //yield return new WaitForSeconds(generationOptions.Delay);
        //if (generationOptions.UsesWalls) { PlaceWalls(); }

        yield return new WaitForSeconds(generationOptions.Delay);

        Debug.Log("Converting Tiles to Common Neighbour");
        ConvertTilesToNeighbours();
        yield return new WaitForSeconds(generationOptions.Delay);
        ConvertTilesToNeighbours();        

        Debug.Log("Adjusting Isolated Tiles");
        yield return new WaitForSeconds(generationOptions.Delay);
        AdjustIsolatedTiles();

        DetermineSkyConnectivity();

        yield return new WaitForSeconds(generationOptions.Delay);
        if (generationOptions.UsesWalls) { PlaceWallsBasedOnSky(); }

        GrowGrass();
    }


    // Fill The World with Tiles
    private void PopulateInitialWorld(TileType fillType)
    {
        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                worldTiles[x, y] = new Tile(fillType, new Vector2Int(x, y), new Vector2(startPoint.x + (generationOptions.cellSize * x), startPoint.y + (generationOptions.cellSize * y)));
                worldVisuals[x, y] = Instantiate(tilePrefab, new Vector2(startPoint.x + (generationOptions.cellSize * x), startPoint.y + (generationOptions.cellSize * y)), Quaternion.identity).GetComponent<TileVisual>();
                worldVisuals[x, y].SetSize(generationOptions.cellSize);
                worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(worldTiles[x, y].type));
            }
        }
    }

    private void GenerateHeightMap()
    {
        int baseHeight = Mathf.RoundToInt(generationOptions.SurfaceBaseHeight * generationOptions.height);
        for (int x = 0; x < generationOptions.width; x++)
        {
            float num = (float)x / (float)generationOptions.height;
            float noise = Mathf.PerlinNoise1D((num * generationOptions.SurfaceFrequency) + Seed) * generationOptions.SurfaceScale;
            //Debug.Log(num);
            //Debug.Log(noise);
            surfaceHeight[x] = Mathf.RoundToInt(baseHeight + noise);
            //Debug.Log(surfaceHeight[x]);

        }
    }

    //private void GenerateNoiseMap(float width, float height,float density)
    //{
    //    for (int x = 0; x < width; x++)
    //    {
    //        for (int y = 0; y < height; y++)
    //        {
    //            int random = UnityEngine.Random.Range(0, 100);
    //            if (random > density)
    //            {
    //                noiseMap[x, y] = 1;
    //            }
    //            else
    //            {
    //                noiseMap[x, y] = 0;
    //            }
    //        }
    //    }
    //}

    private void GenerateLayerOld(GenerationLayerSO layer)
    {
        int minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
        int maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height); 

        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = minY;  y < maxY; y++)
            {
                TileType newType = layer.GenerateTile(NormalisedY(y));
                worldTiles[x, y].type = newType;
                worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(newType));
            }
        }
    }

    private void GenerateLayer(GenerationLayerSO layer)
    {       
        for (int x = 0; x < generationOptions.width; x++)
        {
            int minY, maxY;
            if (layer.SurfaceBased)
            {
                int surfaceY = surfaceHeight[x];
                minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
            }
            else
            {
                minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
            }

            for (int y = minY; y < maxY; y++)
            {
                TileType newType = layer.GenerateTile(NormalisedY(y));
                worldTiles[x, y].type = newType;
                worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(newType));
            }
        }
    }

    private void GenerateNoiseLayer(NoiseLayer layer)
    {

        for (int x = 0; x < generationOptions.width; x++)
        {
            int minY, maxY;
            if (layer.SurfaceBased)
            {
                int surfaceY = surfaceHeight[x];
                minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
            }
            else
            {
                minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
            }
            for (int y = minY; y < maxY; y++)
            {
                TileType newType = layer.ApplyNoise(worldTiles[x, y].type, layer.NoiseMap[x, y]);
                worldTiles[x, y].type = newType;
                worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(newType));
            }
        }
    }

    private void ManageOreGenerationForLayer(OreLayer layer)
    {
        for (int i = 0; i < layer.NumberOfVeinGenerationAttempts; i++)
        {
            Vector2Int veinStartPos = Vector2Int.zero;
            bool startFound = false;
            while (!startFound)
            {
                int x = Random.Range(0, generationOptions.width);
                int minY, maxY;
                if (layer.SurfaceBased)
                {
                    int surfaceY = surfaceHeight[x];
                    minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                    maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
                }
                else
                {
                    minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                    maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
                }
                veinStartPos = new Vector2Int(x, (int)Random.Range(minY, maxY));
                if (isPositionSolid(veinStartPos)) { startFound = true; } 
            }

            //GenerateOreVeinRandom(layer, veinStartPos);
            GenerateOreLayerFrontier(layer, veinStartPos);

        }
    }

    private bool isPositionSolid(Vector2Int pos)
    {
        if (isPositionValid(pos))
        {
            return (TileDictionary.Instance.FetchTileDataByType(worldTiles[pos.x, pos.y].type).isSolid);
        }
        return false;
    }

    private bool isPositionValid(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= generationOptions.width || pos.y < 0 || pos.y >= generationOptions.height)
        {
            return false;
        }
        return true;
    }

    private void GenerateOreLayerFrontier(OreLayer layer, Vector2Int startPos)
    {
        List<Vector2Int> OrePos = new List<Vector2Int>();
        List<Vector2Int> FrontierNeighbours = new List<Vector2Int>();
        int veinSize = Random.Range(layer.MinTilesPerVein, layer.MaxTilesPerVein);

        OrePos.Add(startPos);
        foreach (Vector2Int pos in FetchNeighboursCardinal(startPos))
        {
            if (OrePos.Contains(pos)) { continue; }
            if (FrontierNeighbours.Contains(pos)) { continue; }
            if (isPositionSolid(pos)) { FrontierNeighbours.Add(pos); }
        }

        for (int i = 0; i < veinSize; i++)
        {
            if (FrontierNeighbours.Count <= 0) { break; }

            Vector2Int newOre = FrontierNeighbours[Random.Range(0, FrontierNeighbours.Count)]; ;
            float totalWeight = 0;

            foreach (Vector2Int pos in FrontierNeighbours)
            {
                totalWeight += GetOreGenWeight(layer, pos, OrePos, startPos);
            }
            float random = Random.value * totalWeight;

            foreach (Vector2Int pos in FrontierNeighbours)
            {
                random -= GetOreGenWeight(layer, pos, OrePos, startPos);
                if (random < 0f) { newOre = pos; break; }
            }

            OrePos.Add(newOre);
            FrontierNeighbours.Remove(newOre);
            foreach (Vector2Int pos in FetchNeighboursCardinal(newOre))
            {
                if (OrePos.Contains(pos)) { continue; }
                if (FrontierNeighbours.Contains(pos)) { continue; }
                if (isPositionSolid(pos)) { FrontierNeighbours.Add(pos); }
            }
        }

        foreach (Vector2Int pos in OrePos)
        {
            ConvertTileToOre(layer, pos);
        }
    }

    private List<Vector2Int> FetchNeighboursCardinal(Vector2Int pos)
    {
        List<Vector2Int> list =  new List<Vector2Int>();
        list.Add(new Vector2Int(pos.x + 1, pos.y));
        list.Add(new Vector2Int(pos.x - 1, pos.y));
        list.Add(new Vector2Int(pos.x, pos.y + 1));
        list.Add(new Vector2Int(pos.x, pos.y - 1));
        return list;
    }
    
    private List<Vector2Int> FetchNeighboursCompass(Vector2Int pos)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                list.Add(new Vector2Int(pos.x + offsetX, pos.y + offsetY));
            }
        }

        return list;
    }

    private float GetOreGenWeight(OreLayer layer, Vector2Int pos, List<Vector2Int> ores, Vector2Int startPos)
    {
        float numOreNeighbours = 0;
        foreach (Vector2Int i in FetchNeighboursCompass(pos))
        {
            if (ores.Contains(i)) { numOreNeighbours++; }
        }
        return 1f + numOreNeighbours * 0.5f * Random.Range(0.7f, 1.3f);
    }

    private void ConvertTileToOre(OreLayer layer, Vector2Int pos)
    {
        //Debug.Log("Converting");
        TileType type = layer.GenerateTile();
        //Debug.Log("Selected Type: " + type.ToString());
        worldTiles[pos.x, pos.y].type = type;
        worldVisuals[pos.x, pos.y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(type));
    }


    private void GenerateOreVeinRandom(OreLayer layer, Vector2Int startPos)
    {
        int veinSize = Random.Range(layer.MinTilesPerVein, layer.MaxTilesPerVein);
        Vector2Int currentPos = startPos;
        HashSet<Vector2Int> alreadyConvertedTiles = new HashSet<Vector2Int>();
        for (int i = 0; i < veinSize; i++)
        {
            alreadyConvertedTiles.Add(currentPos);
            bool directionChosen = false;
            int iterations = 0;
            Vector2Int chosenDir;
            while (!directionChosen && iterations < 10)
            {
                iterations++;
                chosenDir = PickRandomDirection(currentPos);
                if (ValidateChosenNewDirection(layer, chosenDir, startPos, alreadyConvertedTiles))
                {
                    currentPos = chosenDir;
                    directionChosen = true;
                }
            }
            if (!directionChosen) {  break; } //Debug.LogError("Failed To Find Valid Ore Direction");

        }

        foreach (Vector2Int pos in alreadyConvertedTiles)
        {
            ConvertTileToOre(layer, pos);
        }
    }

    private bool ValidateChosenNewDirection(OreLayer layer, Vector2Int proposedPos, Vector2Int startPos, HashSet<Vector2Int> alreadyConverted)
    {
        if (alreadyConverted.Contains(proposedPos)) { return false; }
        if (Vector2Int.Distance(proposedPos, startPos) > layer.MaxDistanceFromStart) { return false; }
        if (!isPositionSolid(proposedPos)) { return false; }
        if (proposedPos.x < 0 || proposedPos.x >= generationOptions.width || proposedPos.y < 0 || proposedPos.y >= generationOptions.height)
        {
            return false;
        }

        return true;
    }

    private Vector2Int PickRandomDirection(Vector2Int pos)
    {
        int dir = 1;
        int axis = 1;
        float random = Random.value;
        if (random < 0.5f) { dir = -1; }
        random = Random.value;
        if (random < 0.5f) { axis = -1; }
        switch (axis)
        {
            case -1:
                {
                    pos.x += dir;
                    break;
                }
            case 1:
                {
                    pos.y += dir;
                    break;
                }
        }
        return pos;
    }

    private void IterateAutomata(int Add, int Remove, NoiseLayer layer)
    {
        
        List<Vector2> ToBeRemoved = new List<Vector2>();
        List<Vector2> ToBeAdded = new List<Vector2>();
        List<TileType> options = new List<TileType>();
        TileType?[,] neighbourTypes = new TileType?[generationOptions.width, generationOptions.height];
        //TileType? neighbourType = null;

        for (int x = 0; x < generationOptions.width; x++)
        {
            int minY, maxY;
            if (layer.SurfaceBased)
            {
                int surfaceY = surfaceHeight[x];
                minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
            }
            else
            {
                minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
            }
            for (int y = minY; y < maxY; y++)
            {
                //Debug.Log("looping");
                int activeNeighbours = 0;
                options.Clear();
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                            continue;

                        int checkX = x + offsetX;
                        int checkY = y + offsetY;


                        if (checkX < 0 || checkX >= generationOptions.width ||
                            checkY < 0 || checkY >= generationOptions.height)
                        {
                            activeNeighbours++;
                            continue;
                        }


                        if (isPositionSolid(new Vector2Int(checkX, checkY)))
                        {
                            options.Add(worldTiles[checkX, checkY].type);
                            activeNeighbours++;
                        }
                    }
                }
                if (options.Count > 0)
                {
                    int random = Random.Range(0, options.Count);
                    neighbourTypes[x, y] = options[random];
                }
                

                if (activeNeighbours <= Remove) { ToBeRemoved.Add(new Vector2(x, y)); }
                if (activeNeighbours > Add && !TileDictionary.Instance.FetchTileDataByType(worldTiles[x, y].type).isSolid) { ToBeAdded.Add(new Vector2(x, y)); }

                //Debug.Log("Found at: " +  x + " , " + y);
            }
        }

        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                if (ToBeRemoved.Contains(new Vector2(x, y)))
                {
                    worldTiles[x, y].type = TileType.Air;
                    worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(layer.NoiseType));
                    continue;
                }
                if (ToBeAdded.Contains(new Vector2(x, y)) && neighbourTypes[x, y] != null)
                {
                    worldTiles[x, y].type = neighbourTypes[x, y].Value;
                    worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(neighbourTypes[x, y].Value));
                }
            }
        }
    }

    private void AdjustIsolatedTiles()
    {
        int adjusted = 0;
        List<Vector2> ToBeAdjusted = new List<Vector2>();
        TileType?[,] neighbourTypes = new TileType?[generationOptions.width, generationOptions.height];

        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                int activeNeighbours = 0;
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                            continue;

                        int checkX = x + offsetX;
                        int checkY = y + offsetY;


                        if (checkX < 0 || checkX >= generationOptions.width ||
                            checkY < 0 || checkY >= generationOptions.height)
                        {
                            activeNeighbours++;
                            break;
                        }

                        TileType neighbourType = worldTiles[checkX, checkY].type;

                        if (TileDictionary.Instance.FetchTileDataByType(neighbourType).isSolid)
                        {
                            activeNeighbours++;
                        }
                    }
              
                }
                
                
                if (activeNeighbours <= 0 && !TileDictionary.Instance.FetchTileDataByType(worldTiles[x, y].type).isWall) { neighbourTypes[x, y] = TileType.Air; ToBeAdjusted.Add(new Vector2(x, y)); }
            }
        }

        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                if (ToBeAdjusted.Contains(new Vector2(x, y)) && neighbourTypes[x, y] != null)
                {
                    //Debug.Log("Adjusted Tile: " + x + ", " + y);
                    adjusted++;
                    worldTiles[x, y].type = neighbourTypes[x, y].Value;
                    worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(neighbourTypes[x, y].Value));
                }               
            }
        }

        Debug.Log("Adjusted by AdjustIsolated: " + adjusted);
    }
    // TBC If no tiles in the neighbourhood share a type set the tile to the most common neighbour
    private void ConvertTilesToNeighbours() 
    {
        List<Vector2> ToBeAdjusted = new List<Vector2>();
        NeighbourhoodTypes[,] neighbourTypes = new NeighbourhoodTypes[generationOptions.width, generationOptions.height];
        int adjusted = 0;
        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                
                List<Vector2Int> neighbours = FetchNeighboursCompass(new Vector2Int(x, y));
                neighbourTypes[x, y].types = new TileType[neighbours.Count];
                for (int i = 0; i < neighbours.Count; i++)
                {
                    if (neighbours[i].x < 0 || neighbours[i].x >= generationOptions.width ||
                            neighbours[i].y < 0 || neighbours[i].y >= generationOptions.height)
                    {
                        continue;
                    }
                    if (TileDictionary.Instance.FetchTileDataByType(worldTiles[neighbours[i].x, neighbours[i].y].type).isSolid)
                    {
                        
                    }
                    neighbourTypes[x, y].types[i] = worldTiles[neighbours[i].x, neighbours[i].y].type;
                }
                neighbourTypes[x, y].mostCommonType = DetermineMostCommonType(neighbourTypes[x, y].types, out neighbourTypes[x, y].allSame);

                if (worldTiles[x, y].type != neighbourTypes[x, y].mostCommonType && TileDictionary.Instance.FetchTileDataByType(worldTiles[x, y].type).canConvert)
                {
                    adjusted++;
                    worldTiles[x, y].type = neighbourTypes[x, y].mostCommonType;
                    worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(neighbourTypes[x, y].mostCommonType));
                }
            }
        }
        if (ToBeAdjusted.Count > 0)
        {
            for (int x = 0; x < generationOptions.width; x++)
            {
                for (int y = 0; y < generationOptions.height; y++)
                {
                    if (ToBeAdjusted.Contains(new Vector2(x, y)))
                    {
                        //Debug.Log("Adjusted Tile: " + x + ", " + y);
                        worldTiles[x, y].type = neighbourTypes[x, y].mostCommonType;
                        worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(neighbourTypes[x, y].mostCommonType));
                    }
                }
            }
        }
        Debug.Log("Adjusted by ConvertToNeighbour: " + adjusted);
    }
    // Check Neighbours
    // Tally Active Neighbours to remove floating pixels in air
    // Determine the most common type in each neighbourhood
    // Determine if all tiles are the same type
    // If the selected tile is not the most common one change it

    private bool CanTileSeeSun(Vector2Int tile)
    {
        for (int y = tile.y; y < generationOptions.height; y++)
        {
            if (TileDictionary.Instance.FetchTileDataByType(worldTiles[tile.x, y].type).isSolid)
            {
                return false;
            }
        }
        return true;
    }

    private float NormalisedY(int y) // Used for transition layers to work out how far between two layers a chosen tile is
    {
        return (float)y / (generationOptions.height - 1);
    }

    private TileType DetermineMostCommonType(TileType[] types, out bool same)
    {
        Dictionary<TileType, int> counts = new Dictionary<TileType, int>();
        TileType? mostCommon = null;
        int currentMax = 0;
        same = false;
        foreach (TileType type in types)
        {
            counts.TryGetValue(type, out int count);
            count++;
            counts[type] = count;

            if (count > currentMax)
            {
                mostCommon = type;
                currentMax = count;
            }
        }

        if (mostCommon == null)
        {
            mostCommon = TileType.Debug;
        }
        if (counts.Count == 1) { same = true; }
        return mostCommon.Value;
    }

    private void PlaceWalls()
    {
        foreach (GenerationLayerSO layer in baseLayers)
        {
            for (int x = 0; x < generationOptions.width; x++)
            {
                int minY, maxY;
                if (layer.SurfaceBased)
                {
                    int surfaceY = surfaceHeight[x];
                    minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                    maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
                }
                else
                {
                    minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                    maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
                }

                for (int y = minY; y < maxY; y++)
                {
                    if (!TileDictionary.Instance.FetchTileDataByType(worldTiles[x, y].type).isSolid
                        && DoesTileHaveSolidTileInColumnAbove(new Vector2Int(x, y), 150) 
                        && !DoesTileHaveTargetTileInColumAbove(new Vector2Int(x, y), TileType.Grass, 15))
                    {
                        Debug.Log("Placing Wall");
                        TileType newType = layer.GenerateWallTile(NormalisedY(y));
                        worldTiles[x, y].type = newType;
                        worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(newType));
                    }

                    
                }
            }
        }
    }

    private void PlaceWallsBasedOnSky()
    {
        

        foreach (GenerationLayerSO layer in baseLayers)
        {
            for (int x = 0; x < generationOptions.width; x++)
            {
                int minY, maxY;
                if (layer.SurfaceBased)
                {
                    int surfaceY = surfaceHeight[x];
                    minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                    maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
                }
                else
                {
                    minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                    maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
                }

                for (int y = minY; y < maxY; y++)
                {
                    if (isPositionSolid(new Vector2Int(x, y))) { continue; }
                    
                    if (!worldTiles[x, y].isSkyConnected)
                    {
                        TileType newType = layer.GenerateWallTile(NormalisedY(y));
                        worldTiles[x, y].type = newType;
                        worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(newType));
                    }
                }
            }
        }
    }

    private void DetermineSkyConnectivity()
    {
        int[,] fillDirections = { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } };


        Queue<Vector2Int> floodFillQueue = new Queue<Vector2Int>();
        
        for (int x = 0; x < generationOptions.width; x++)
        {
            if (!isPositionSolid(new Vector2Int(x, generationOptions.height - 1)))
            {
                worldTiles[x, generationOptions.height - 1].isSkyConnected = true;
                floodFillQueue.Enqueue(new Vector2Int(x, generationOptions.height - 1));
            }
        }

        while (floodFillQueue.Count > 0)
        {
            Vector2Int current = floodFillQueue.Dequeue();

            for (int i = 0; i < 4; i++)
            {
                int nextX = current.x + fillDirections[i, 0];
                int nextY = current.y + fillDirections[i, 1];

                if (isPositionSolid(new Vector2Int(nextX, nextY))) { continue; }

                if (!isPositionValid(new Vector2Int(nextX, nextY))) { continue; }

                if (worldTiles[nextX, nextY].isSkyConnected) { continue; }

                worldTiles[nextX, nextY].isSkyConnected = true;
                foreach (Vector2Int pos in FetchNeighboursCardinal(new Vector2Int(nextX, nextY)))
                {
                    if (!isPositionValid(pos)) { continue; }
                    worldTiles[pos.x, pos.y].isAirExposed = true;
                }
                floodFillQueue.Enqueue(new Vector2Int(nextX, nextY));
            }
        }
    }

    private bool DoesTileHaveSolidTileInColumnAbove(Vector2Int pos)
    {
        for (int y = pos.y; y < generationOptions.height; y++)
        {
            if (isPositionSolid(new Vector2Int(pos.x, y)))
            {
                return true;
            }
        }
        return false;
    }

    private bool DoesTileHaveSolidTileInColumnAbove(Vector2Int pos, int checkLength)
    {
        for (int y = pos.y; y < pos.y + checkLength; y++)
        {
            if (isPositionSolid(new Vector2Int(pos.x, y)))
            {
                return true;
            }
        }
        return false;
    }

    private bool DoesTileHaveTargetTileInColumAbove(Vector2Int pos, TileType target)
    {
        for (int y = pos.y; y < generationOptions.height; y++)
        {
            if (worldTiles[pos.x, y].type == target)
            {
                return true;
            }
        }
        return false;
    }
    private bool DoesTileHaveTargetTileInColumAbove(Vector2Int pos, TileType target, int checkLength)
    {
        for (int y = pos.y; y < pos.y + checkLength; y++)
        {
            if (isPositionSolid(new Vector2Int(pos.x, y)))
            {
                if (worldTiles[pos.x, y].type == target)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        return false;
    }

    private void GrowGrass()
    {
        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                if (worldTiles[x, y].isAirExposed && worldTiles[x, y].type == TileType.Dirt)
                {
                    if (!isPositionSolid(new Vector2Int(x, y + 1)) || !isPositionSolid(new Vector2Int(x + 1, y)) || !isPositionSolid(new Vector2Int(x - 1, y)))
                    {
                        worldTiles[x, y].type = TileType.Grass;
                        worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(TileType.Grass));
                        continue;
                    }


                    if (isPositionValid(new Vector2Int(x, y - 1)) && isPositionValid(new Vector2Int(x, y - 2)))
                    {
                        if (!isPositionSolid(new Vector2Int(x, y - 1)))
                        {
                            continue;
                        }
                        if (!isPositionSolid(new Vector2Int(x, y - 2)))
                        {
                            continue;
                        }
                    }
                    worldTiles[x, y].type = TileType.Grass;
                    worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(TileType.Grass));
                }
            }
        }
    }

    struct NeighbourhoodTypes
    {
        public TileType[] types;
        public TileType mostCommonType;
        public bool allSame;
    }
}
