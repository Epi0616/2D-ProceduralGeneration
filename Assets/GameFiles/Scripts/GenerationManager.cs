using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
public enum NoiseType { Perlin, Custom }
public class GenerationManager : MonoBehaviour
{
    //public InputActionReference next, save;
    [SerializeField] private int width = 1, height = 1;
    [SerializeField] private float cellSize;
    [SerializeField] private Transform originPoint;
    [SerializeField] private float noiseMagnification = 1, noisePower = 1;
    private float noiseOffsetX, noiseOffsetY;
    [SerializeField] private float Iterations, Delay, RemoveThreshold, AddThreshold;
    private Vector2 startPoint;
    
    [SerializeField] private GameObject tilePrefab;

    private WorldTile[,] worldTiles;
    private int[,] noiseMap;

    private bool finished = false;

    //private void OnEnable()
    //{
    //    next.action.Enable();
    //    save.action.Enable();
    //}
    //private void OnDisable()
    //{
    //    next.action.Disable();
    //    save.action.Disable();
    //}

    private void Start()
    {
        startPoint = originPoint.transform.position;
        startPoint.x += cellSize / 2;
        startPoint.y += cellSize / 2;
        worldTiles = new WorldTile[width,height];
        noiseMap = new int[width,height];
        noiseOffsetX = UnityEngine.Random.Range(-10f, 10f);
        noiseOffsetY = UnityEngine.Random.Range(-10f, 10f);
        StartCoroutine(GenerationCoroutine());
    }

    private IEnumerator GenerationCoroutine()
    {
        GenerateNoiseMap(noisePower);

        PopulateInitialWorld();
        yield return new WaitForSeconds(3);
        int current = 0;
        while (current < Iterations && !finished)
        {
            Debug.Log("Iteration: " + (current + 1));
            AddAndRemoveTiles();
            yield return new WaitForSeconds(Delay);
            current++;
        }
       
        RemoveAllAbandonedTiles();

        worldTiles[0, 0].visual.SetColor(Color.blue);
       // Debug.Log("Finished");

    }

    private void PopulateInitialWorld()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                worldTiles[x, y] = new WorldTile(new Vector2(startPoint.x + (cellSize * x), startPoint.y + (cellSize * y)), TileType.Dirt, 1, x + y);
                TileVisual visual = Instantiate(tilePrefab, worldTiles[x, y].worldPos, Quaternion.identity).GetComponent<TileVisual>();
                visual.SetSize(cellSize);        
                worldTiles[x, y].visual = visual;

                //worldTiles[x, y].TileValue = DetermineValueFromPerlin(x, y);
                worldTiles[x, y].TileValue = noiseMap[x, y];
            }
        }
    }

    private float DetermineValueFromPerlin(float x, float y)
    {
        float perlin = Mathf.PerlinNoise((x + noiseOffsetX) / noiseMagnification, (y + noiseOffsetY) / noiseMagnification);
        perlin = Mathf.Clamp01(perlin * noisePower);
        return perlin;
    }

    private void GenerateNoiseMap(float density)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int random = UnityEngine.Random.Range(0, 100);
                if (random > density)
                {
                    noiseMap[x, y] = 1;
                }
                else
                {
                    noiseMap[x, y] = 0;
                }
            }
        }
    }

    private void RemoveAllAbandonedTiles()
    {
        List<Vector2> ToBeRemoved = new List<Vector2>();
        List<Vector2> ToBeAdded = new List<Vector2>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Debug.Log("looping");
                int activeNeighbours = 0;
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                            continue;

                        int checkX = x + offsetX;
                        int checkY = y + offsetY;


                        if (checkX < 0 || checkX >= width ||
                            checkY < 0 || checkY >= height)
                            continue;

                        if (worldTiles[checkX, checkY].tileType == TileType.Dirt)
                        {
                            activeNeighbours++;
                        }
                    }
                }
                if (activeNeighbours == 0) { ToBeRemoved.Add(new Vector2(x, y)); }
                if (activeNeighbours == 8) { ToBeAdded.Add(new Vector2(x, y)); }
                //Debug.Log("Found at: " +  x + " , " + y);
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (ToBeRemoved.Contains(new Vector2(x, y)))
                {
                    worldTiles[x, y].TileValue = 0;
                }
                if (ToBeAdded.Contains(new Vector2(x, y)))
                {
                    worldTiles[x, y].TileValue = 1;
                }
            }
        }
    }

    private void AddAndRemoveTiles()
    {
        // Debug.Log("Checking For Isolation");
        List<Vector2> ToBeRemoved = new List<Vector2>();
        List<Vector2> ToBeAdded = new List<Vector2>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Debug.Log("looping");
                int activeNeighbours = 0;
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    for (int offsetY = -1; offsetY <= 1; offsetY++)
                    {
                        if (offsetX == 0 && offsetY == 0)
                            continue;

                        int checkX = x + offsetX;
                        int checkY = y + offsetY;


                        if (checkX < 0 || checkX >= width ||
                            checkY < 0 || checkY >= height)
                        {
                            activeNeighbours++;
                            continue;
                        }
                            

                        if (worldTiles[checkX, checkY].tileType == TileType.Dirt)
                        {
                            activeNeighbours++;
                        }
                    }
                }
                if (activeNeighbours <= RemoveThreshold) { ToBeRemoved.Add(new Vector2(x, y)); }
                if (activeNeighbours > AddThreshold && worldTiles[x, y].tileType == TileType.Air) { ToBeAdded.Add(new Vector2(x, y)); }

                //Debug.Log("Found at: " +  x + " , " + y);
            }
        }

        //if (ToBeAdded.Count == 0 && ToBeRemoved.Count == 0)
        //{
        //    Debug.Log("Exiting Early Generation Finished");
        //    finished = true;
        //    return;
        //}


        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (ToBeRemoved.Contains(new Vector2(x, y)))
                {
                    worldTiles[x, y].TileValue = 0;
                    continue;
                }
                if (ToBeAdded.Contains(new Vector2(x, y)))
                {
                    worldTiles[x, y].TileValue = 1;
                }
            }
        }
    }
}
