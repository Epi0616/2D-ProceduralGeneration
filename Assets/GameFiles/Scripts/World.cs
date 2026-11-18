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

    public int repeatIterations = 0;
    private int currentIterations = 0;
    public bool SeedDeterminesHeight = false;
    public int Seed;
    // World 0,0 is Bottom Left of the Grid


    private void Start()
    {
        Seed = Math.Clamp(Seed, 0, 1000);
        // Generate a Seed if it hasn't been set
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

        // Generate the Heightmap
        surfaceHeight = new int[generationOptions.width];
        GenerateHeightMap();

        StartCoroutine(GenerateWorld());
    }

    // Main Coroutine for Generation Steps
    private IEnumerator GenerateWorld()
    {
        Debug.Log("Generating World: " + currentIterations + " of " + repeatIterations);
        yield return new WaitForSeconds(2);

        // Generate the Base Layers
        foreach (GenerationLayerSO layer in baseLayers)
        {         
            Debug.Log("Generating Layer: " +  layer.name);
            GenerateLayer(layer);
            yield return new WaitForSeconds(generationOptions.Delay);
        }

        // Generate the Noise Layers
        foreach (NoiseLayer layer in noiseLayers)
        {
            Debug.Log("Generating Noise Layer: " + layer.name);
            // Check whether the Noise Layer is "Layered"
            if (layer.Layered)
            {
                layer.GenerateLayeredNoiseMap(generationOptions.width, generationOptions.height, Seed);
            }
            else
            {
                layer.GenerateNoiseMap(generationOptions.width, generationOptions.height, Seed);
            }            
            GenerateNoiseLayer(layer);

            // Iterate the Automata for the Noise Layer
            for (int i = 0; i < layer.Iterations; i++)
            {
                Debug.Log("Noise Layer: " + layer.name + " Iteration: " + i);
                IterateAutomata(generationOptions.AddThreshold, generationOptions.RemoveThreshold, layer);
                yield return new WaitForSeconds(generationOptions.Delay);
            }
            yield return new WaitForSeconds(generationOptions.Delay);
        }

        // Generate the Ore Veins
        foreach (OreLayer layer in oreLayers)
        {
            Debug.Log("Populating Ore Layer: " + layer.name);
            ManageOreGenerationForLayer(layer);
            yield return new WaitForSeconds(generationOptions.Delay);
        }

        // Convert Uncommon Tiles
        Debug.Log("Converting Tiles to Common Neighbour");
        ConvertTilesToNeighbours();
        yield return new WaitForSeconds(generationOptions.Delay);
       // ConvertTilesToNeighbours();        

        // Remove Stray Tiles
        Debug.Log("Adjusting Isolated Tiles");
        yield return new WaitForSeconds(generationOptions.Delay);
        AdjustIsolatedTiles();

        // Flood Fill to mark Sky tiles
        DetermineSkyConnectivity();

        // If place walls is enabled generate walls
        yield return new WaitForSeconds(generationOptions.Delay);
        if (generationOptions.UsesWalls) { PlaceWallsBasedOnSky(); }

        GrowGrass();

        yield return new WaitForSeconds(7.5f);
        currentIterations++;

        if (currentIterations < repeatIterations)
        {        
            ResetWorld();
        }
    }

    private void ResetWorld()
    {
        Seed = Random.Range(0, 1000);
       
        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                worldTiles[x, y].isSkyConnected = false;
                worldTiles[x, y].isAirExposed = false;
                worldTiles[x, y].type = generationOptions.initialTileType;           
                worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(worldTiles[x, y].type));
            }
        }
        GenerateHeightMap();
        StartCoroutine(GenerateWorld());
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

    // Use 1D Perlin to generate the height of each column in the world
    private void GenerateHeightMap()
    {
        if (SeedDeterminesHeight)
        {
            int baseHeight = Mathf.RoundToInt(generationOptions.SurfaceBaseHeight * generationOptions.height);
            for (int x = 0; x < generationOptions.width; x++)
            {
                float num = (float)x / (float)generationOptions.height;
                float noise = Mathf.PerlinNoise1D((num * (generationOptions.SurfaceFrequency +  (Seed * 0.003f)) + Seed)) * (generationOptions.SurfaceScale + (Seed * 0.0001f));
                surfaceHeight[x] = Mathf.RoundToInt(baseHeight + noise);

            }      
        }
        else
        {
            int baseHeight = Mathf.RoundToInt(generationOptions.SurfaceBaseHeight * generationOptions.height);
            for (int x = 0; x < generationOptions.width; x++)
            {
                float num = (float)x / (float)generationOptions.height;
                float noise = Mathf.PerlinNoise1D((num * (generationOptions.SurfaceFrequency + Seed) * generationOptions.SurfaceScale));
                surfaceHeight[x] = Mathf.RoundToInt(baseHeight + noise);

            }
        }
        
    }

    // Generate Base Layers
    private void GenerateLayer(GenerationLayerSO layer)
    {       
        for (int x = 0; x < generationOptions.width; x++)
        {
            // For each colum determine the y constraints where the layer operates, based on whether the layer is "Surface Based"
            int minY, maxY;
            if (layer.SurfaceBased)
            {
                int surfaceY = surfaceHeight[x];
                minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
                if (maxY > generationOptions.height) { maxY = generationOptions.height; }
            }
            else
            {
                minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
            }

            for (int y = minY; y < maxY; y++)
            {
                // Select the tile type to be generated
                TileType newType = layer.GenerateTile(NormalisedY(y));
                // Set the new type
                worldTiles[x, y].type = newType;
                // Update the tile visual using the tile attributes from the dictionary
                worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(newType));
            }
        }
    }

    // Apply Noise from Noise Layers
    private void GenerateNoiseLayer(NoiseLayer layer)
    {
        for (int x = 0; x < generationOptions.width; x++)
        {
            // For each colum determine the y constraints where the layer operates, based on whether the layer is "Surface Based"
            int minY, maxY;
            if (layer.SurfaceBased)
            {
                int surfaceY = surfaceHeight[x];
                minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
                if (maxY > generationOptions.height) { maxY = generationOptions.height; }
            }
            else
            {
                minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
            }
            for (int y = minY; y < maxY; y++)
            {
                // Determine whether the tile should be "Solid" or not
                TileType newType = layer.ApplyNoise(worldTiles[x, y].type, layer.NoiseMap[x, y]);
                worldTiles[x, y].type = newType;
                worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(newType));
            }
        }
    }

    // Find a suitable location to begin ore generation
    private void ManageOreGenerationForLayer(OreLayer layer)
    {
        for (int i = 0; i < layer.NumberOfVeinGenerationAttempts; i++)
        {
            Vector2Int veinStartPos = Vector2Int.zero;
            bool startFound = false;
            int iterations = 0;
            // Iterations are capped to prevent impossible loops
            while (!startFound && iterations < 25)
            {
                // Select a random column to generate in
                int x = Random.Range(0, generationOptions.width);

                // For each colum determine the y constraints where the layer operates, based on whether the layer is "Surface Based"
                int minY, maxY;
                if (layer.SurfaceBased)
                {
                    int surfaceY = surfaceHeight[x];
                    minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                    maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
                    if (maxY > generationOptions.height) { maxY = generationOptions.height; }
                }
                else
                {
                    minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                    maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
                }

                // Pick a random height to select as a start position
                veinStartPos = new Vector2Int(x, (int)Random.Range(minY, maxY));

                // If the position is solid exit the loop a start position has been found
                if (isPositionSolid(veinStartPos)) { startFound = true; }
                iterations++;
            }

            GenerateOreLayer(layer, veinStartPos);

        }
    }

    // Commonly used check function, returns whether the tile type in the specified position has the solid tag
    private bool isPositionSolid(Vector2Int pos)
    {
        if (isPositionValid(pos))
        {
            return (TileDictionary.Instance.FetchTileDataByType(worldTiles[pos.x, pos.y].type).isSolid);
        }
        return false;
    }

    // Commonly used validation function, returns whether the specified position is within the world bounds
    private bool isPositionValid(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= generationOptions.width || pos.y < 0 || pos.y >= generationOptions.height)
        {
            return false;
        }
        return true;
    }

    // Generate a vein of Ore 
    private void GenerateOreLayer(OreLayer layer, Vector2Int startPos)
    {
        List<Vector2Int> OrePos = new List<Vector2Int>();
        List<Vector2Int> OreNeighbours = new List<Vector2Int>();

        // Select a random vein size from the layer
        int veinSize = Random.Range(layer.MinTilesPerVein, layer.MaxTilesPerVein);

        // Add starting position
        OrePos.Add(startPos);

        // Check the neighbours of the starting position, if solid add them as valid ore spots
        foreach (Vector2Int pos in FetchNeighboursCardinal(startPos))
        {
            if (OrePos.Contains(pos)) { continue; }
            if (OreNeighbours.Contains(pos)) { continue; }
            if (isPositionSolid(pos)) { OreNeighbours.Add(pos); }
        }

        for (int i = 0; i < veinSize; i++)
        {
            // If there are no valid ore placements exit early
            if (OreNeighbours.Count <= 0) { break; }

            // Select a random spot as a backup
            Vector2Int newOre = OreNeighbours[Random.Range(0, OreNeighbours.Count)];

            float totalWeight = 0;

            // Pick a new ore position from the possible neighbours from their weightings
            foreach (Vector2Int pos in OreNeighbours)
            {
                totalWeight += GetOreGenWeight(layer, pos, OrePos, startPos);
            }
            float random = Random.value * totalWeight;

            foreach (Vector2Int pos in OreNeighbours)
            {
                random -= GetOreGenWeight(layer, pos, OrePos, startPos);
                if (random < 0f) { newOre = pos; break; }
            }
            
            // Adjust lists with new ore piece
            OrePos.Add(newOre);
            OreNeighbours.Remove(newOre);

            // Add the new neighbours to the potential ore list
            foreach (Vector2Int pos in FetchNeighboursCardinal(newOre))
            {
                if (OrePos.Contains(pos)) { continue; }
                if (OreNeighbours.Contains(pos)) { continue; }
                if (isPositionSolid(pos)) { OreNeighbours.Add(pos); }
            }
        }

        // Place all the Ore Tiles
        foreach (Vector2Int pos in OrePos)
        {
            TileType type = layer.GenerateTile();
            worldTiles[pos.x, pos.y].type = type;
            worldVisuals[pos.x, pos.y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(type));
        }
    }

    // Commonly used function, returns the 4 adjacent positions to the supplied "pos"
    private List<Vector2Int> FetchNeighboursCardinal(Vector2Int pos)
    {
        List<Vector2Int> list =  new List<Vector2Int>();
        list.Add(new Vector2Int(pos.x + 1, pos.y));
        list.Add(new Vector2Int(pos.x - 1, pos.y));
        list.Add(new Vector2Int(pos.x, pos.y + 1));
        list.Add(new Vector2Int(pos.x, pos.y - 1));
        return list;
    }
    
    // Commonly used function, returns the 3x3 grid to the supplied "pos"
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

    // Determine the weight a potential ore position should have based on the number of neighbours and a slight random adjustment
    private float GetOreGenWeight(OreLayer layer, Vector2Int pos, List<Vector2Int> ores, Vector2Int startPos)
    {
        float numOreNeighbours = 0;
        foreach (Vector2Int i in FetchNeighboursCompass(pos))
        {
            if (ores.Contains(i)) { numOreNeighbours++; }
        }
        return 1f + numOreNeighbours * 0.5f * Random.Range(0.7f, 1.3f);
    }

    // UNUSED - Old Ore Method that functioned using Drunkard's Walk
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
            TileType type = layer.GenerateTile();
            worldTiles[pos.x, pos.y].type = type;
            worldVisuals[pos.x, pos.y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(type));
        }
    }

    // UNUSED - Function was used by old Ore Generation
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

    // UNUSED - Function was used by old Ore Generation
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

    // Iterate a Cellular Automata on the Noise Layer
    private void IterateAutomata(int Add, int Remove, NoiseLayer layer)
    {
        // Set up lists of tiles to be changed and stored data
        List<Vector2> ToBeRemoved = new List<Vector2>();
        List<Vector2> ToBeAdded = new List<Vector2>();
        List<TileType> options = new List<TileType>();
        TileType?[,] neighbourTypes = new TileType?[generationOptions.width, generationOptions.height];
        //TileType? neighbourType = null;

        for (int x = 0; x < generationOptions.width; x++)
        {
            // For each colum determine the y constraints where the layer operates, based on whether the layer is "Surface Based"
            int minY, maxY;
            if (layer.SurfaceBased)
            {
                int surfaceY = surfaceHeight[x];
                minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
                if (maxY > generationOptions.height) { maxY = generationOptions.height; }
            }
            else
            {
                minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
            }
            for (int y = minY; y < maxY; y++)
            {      
                int activeNeighbours = 0;
                options.Clear();

                // Fetch the adjacent tiles and count active neighbours
                foreach (Vector2Int pos in FetchNeighboursCompass(new Vector2Int(x, y)))
                {
                    if (pos == new Vector2Int(x, y)) { continue; }
                    if (!isPositionValid(pos)) { activeNeighbours++; continue; }
                    if (isPositionSolid(pos))
                    {
                        options.Add(worldTiles[pos.x, pos.y].type);
                        activeNeighbours++;
                    }
                }
       
                if (options.Count > 0)
                {
                    int random = Random.Range(0, options.Count);
                    neighbourTypes[x, y] = options[random];
                }
                
                // If the number of active neighbours is less than or equal to "Remove" add it to the remove list
                // If the number of active neighbours is more than "Add" and the tile isn't solid add it to the add list
                if (activeNeighbours <= Remove) { ToBeRemoved.Add(new Vector2(x, y)); }
                if (activeNeighbours > Add && !TileDictionary.Instance.FetchTileDataByType(worldTiles[x, y].type).isSolid) { ToBeAdded.Add(new Vector2(x, y)); }

      
            }
        }

        // Apply the changes found through the iteration
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

    // Similar Process to Iterate Automata, if a tile has no active neighbours convert it to air
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
                foreach (Vector2Int pos in FetchNeighboursCompass(new Vector2Int(x, y)))
                {
                    if (!isPositionValid(pos)) { activeNeighbours++; continue; }
                    if (isPositionSolid(pos))
                    {
                        activeNeighbours++;
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
    
    private void ConvertTilesToNeighbours() 
    {
        // Setup Lists and neighbourTypes array
        NeighbourhoodTypes[,] neighbourTypes = new NeighbourhoodTypes[generationOptions.width, generationOptions.height];
        int adjusted = 0;
        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                // Fetch the neighbours and store their types
                List<Vector2Int> neighbours = FetchNeighboursCompass(new Vector2Int(x, y));
                neighbourTypes[x, y].types = new TileType[neighbours.Count];
                for (int i = 0; i < neighbours.Count; i++)
                {
                    if (neighbours[i].x < 0 || neighbours[i].x >= generationOptions.width ||
                            neighbours[i].y < 0 || neighbours[i].y >= generationOptions.height)
                    {
                        continue;
                    }
                    
                    neighbourTypes[x, y].types[i] = worldTiles[neighbours[i].x, neighbours[i].y].type;
                }
                // Determine the most common type neighbour
                neighbourTypes[x, y].mostCommonType = DetermineMostCommonType(neighbourTypes[x, y].types, out neighbourTypes[x, y].allSame);

                // If the target cell isn't of the most common type and can be converted, change the type
                if (worldTiles[x, y].type != neighbourTypes[x, y].mostCommonType && TileDictionary.Instance.FetchTileDataByType(worldTiles[x, y].type).canConvert)
                {
                    adjusted++;
                    worldTiles[x, y].type = neighbourTypes[x, y].mostCommonType;
                    worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(neighbourTypes[x, y].mostCommonType));
                }
            }
        }
        
        Debug.Log("Adjusted by ConvertToNeighbour: " + adjusted);
    }

    // Used for transition layers, determines how far up the world in the 0 - 1 range a tile is
    private float NormalisedY(int y) 
    {
        return (float)y / (generationOptions.height - 1);
    }

    private float NormalisedY(int x, int y)
    {
        return (float)y / surfaceHeight[x];
    } 

    private TileType DetermineMostCommonType(TileType[] types, out bool same)
    {
        // Setup a dictionary to store the type and number of times it appears
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
        // Return the Debug type in case of error
        if (mostCommon == null)
        {
            mostCommon = TileType.Debug;
        }
        // If the number of dictionary elements is 1, all types are the same
        if (counts.Count == 1) { same = true; }
        return mostCommon.Value;
    }

    private void PlaceWallsBasedOnSky()
    {    
        foreach (GenerationLayerSO layer in baseLayers)
        {
            for (int x = 0; x < generationOptions.width; x++)
            {
                // For each colum determine the y constraints where the layer operates, based on whether the layer is "Surface Based"
                int minY, maxY;
                if (layer.SurfaceBased)
                {
                    int surfaceY = surfaceHeight[x];
                    minY = Mathf.RoundToInt(layer.MinY * surfaceY);
                    maxY = Mathf.RoundToInt(layer.MaxY * surfaceY);
                    if (maxY > generationOptions.height) { maxY = generationOptions.height; }
                }
                else
                {
                    minY = Mathf.RoundToInt(layer.MinY * generationOptions.height);
                    maxY = Mathf.RoundToInt(layer.MaxY * generationOptions.height);
                }

                for (int y = minY; y < maxY; y++)
                {
                    if (isPositionSolid(new Vector2Int(x, y))) { continue; }
                    
                    // If the tile isn't solid and isn't connected to the sky, convert it to a wall
                    if (!worldTiles[x, y].isSkyConnected || NormalisedY(x, y) < 0.82f)
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
        Queue<Vector2Int> floodFillQueue = new Queue<Vector2Int>();
        
        // Populate the queue with the top row of world tiles
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
            // Fetch a new tile to check from
            Vector2Int current = floodFillQueue.Dequeue();

            // Fetch the tile's neighbours
            foreach (Vector2Int pos in FetchNeighboursCompass(current))
            {
                // As long as the tile isn't solid, is valid and hasn't already been marked it is now marked as SkyConnected 
                if (isPositionSolid(pos)) { continue; }

                if (!isPositionValid(pos)) { continue; }

                if (worldTiles[pos.x, pos.y].isSkyConnected) { continue; }

                worldTiles[pos.x, pos.y].isSkyConnected = true;

                // The tile's neighbours are marked as AirExposed
                foreach (Vector2Int adj in FetchNeighboursCardinal(pos))
                {
                    if (!isPositionValid(adj)) { continue; }
                    worldTiles[adj.x, adj.y].isAirExposed = true;
                }

                // Enqueue the new sky tile
                floodFillQueue.Enqueue(pos);
            }
        }
    }

    private void GrowGrass()
    {
        for (int x = 0; x < generationOptions.width; x++)
        {
            for (int y = 0; y < generationOptions.height; y++)
            {
                // If the tile is airexposed and Dirt, it can grow grass
                if (worldTiles[x, y].isAirExposed && worldTiles[x, y].type == TileType.Dirt && NormalisedY(x, y) > 0.82f)
                {
                    // If there is any air left, right or above the tile, grow grass immediately and skip the rest of the loop
                    if (!isPositionSolid(new Vector2Int(x, y + 1)) || !isPositionSolid(new Vector2Int(x + 1, y)) || !isPositionSolid(new Vector2Int(x - 1, y)))
                    {
                        worldTiles[x, y].type = TileType.Grass;
                        worldVisuals[x, y].UpdateVisualFromTileData(TileDictionary.Instance.FetchTileDataByType(TileType.Grass));
                        continue;
                    }

                    // If the positions below the tile are air do not grow grass and skip the loop
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
    // Struct used for converting tiles
    struct NeighbourhoodTypes
    {
        public TileType[] types;
        public TileType mostCommonType;
        public bool allSame;
    }
}
