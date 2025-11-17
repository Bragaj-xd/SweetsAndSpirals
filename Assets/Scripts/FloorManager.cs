using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject ladderPrefab;
    public GameObject snakePrefab;
    public int width = 10;
    public int height = 10;
    public float tileSize = 2f;

    [HideInInspector] public Tile[,] tiles;
    public List<GameObject> ladders = new List<GameObject>();
    public List<GameObject> snakes = new List<GameObject>();

    void Awake()
    {
        GenerateFloor();
        GenerateLadders();
        GenerateSnakes();
    }

    void GenerateFloor()
    {
        tiles = new Tile[width, height];

        int idCounter = 0; // counter for unique tile IDs

        for (int z = 0; z < height; z++)
        {
            // determine direction: even row → left to right, odd row → right to left
            bool leftToRight = (z % 2 == 0);

            if (leftToRight)
            {
                for (int x = 0; x < width; x++)
                {
                    CreateTile(x, z, ref idCounter);
                }
            }
            else
            {
                for (int x = width - 1; x >= 0; x--)
                {
                    CreateTile(x, z, ref idCounter);
                }
            }
        }

        Debug.Log($"Generated {idCounter} tiles in zigzag pattern.");
    }

    void CreateTile(int x, int z, ref int idCounter)
    {
        Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize);
        GameObject newTile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
        newTile.name = $"Tile_{x}_{z}";

        Tile tile = newTile.GetComponent<Tile>();
        tiles[x, z] = tile;

        // Assign unique ID
        tile.tileID = idCounter;
        idCounter++;
    }

    public Tile GetTileAtPosition(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x / tileSize);
        int z = Mathf.FloorToInt(worldPos.z / tileSize);

        if (x >= 0 && x < width && z >= 0 && z < height)
            return tiles[x, z];
        return null;
    }

    void GenerateLadders()
    {
        int ladderCount = Random.Range(5, 10);
        Debug.Log(ladderCount);
        int randomTileStart;
        for (int i = 0; i < ladderCount; i++)
        {
            for(int y = 0; y < 10; y++)
            {
                // tileID range: 0 .. tiles.Length-1
                randomTileStart = Random.Range(5, tiles.Length - 10);
                // find start tile by ID

                Tile startTile = FindStartTile(randomTileStart);
                if (startTile == null)
                {
                    Debug.Log($"No valid start tile at {randomTileStart} " + y);
                    
                    
                    continue;
                }
            
        


                // pick an end tile ID some steps ahead; clamp so it stays in range
                int minEnd = randomTileStart + 10;
                int maxEnd = randomTileStart + 25;
                minEnd = Mathf.Clamp(minEnd, 0, tiles.Length - 10);
                maxEnd = Mathf.Clamp(maxEnd, 0, tiles.Length - 10);
                if (minEnd > maxEnd) { int tmp = minEnd; minEnd = maxEnd; maxEnd = tmp; }

                int randomTileEnd = Random.Range(minEnd, maxEnd + 1); // inclusive upper bound

                // find end tile by ID
                Tile endTile = null;
                foreach (Tile ts in tiles)
                {
                    if (ts == null) continue;
                    if (ts.tileID == randomTileEnd)
                    {
                        endTile = ts;
                        Debug.Log(endTile);
                        break;
                    }
                }

                if (endTile == null) continue; // no matching end tile found — skip

                // mark functions
                Debug.Log(startTile);
                startTile.tileFunction = 1;
                endTile.tileFunction = 2;

                // instantiate ladder at the ladder position child of the start tile
                Vector3 pos = startTile.GetComponentInChildren<LadderPos>().transform.position;
                GameObject newLadder = Instantiate(ladderPrefab, pos, Quaternion.identity, transform);

                // set ladder IDs on Ladder component
                Ladder ladderComp = newLadder.GetComponent<Ladder>();
                if (ladderComp != null)
                {
                    ladderComp.startTile = startTile.tileID;
                    ladderComp.endTile = endTile.tileID;
                }

                // compute direct direction from start to end
                Vector3 targetDirection = endTile.transform.position - startTile.transform.position;
                if (targetDirection.sqrMagnitude <= Mathf.Epsilon) continue;

                // use LookRotation to face the target
                Quaternion look = Quaternion.LookRotation(targetDirection.normalized);

                // APPLY A YAW OFFSET if your ladder model's forward axis is rotated.
                // The common fix when things are off by -90° yaw is to multiply by this:
                Quaternion yawOffset = Quaternion.Euler(0f, -90f, 0f);
                // If it's off by +90°, change to Quaternion.Euler(0f, 90f, 0f).

                newLadder.transform.rotation = look * yawOffset;
                ladders.Add(newLadder);
                Debug.Log($"{startTile.tileID} to {endTile.tileID}");
                break;
            }
        }
    }

    void GenerateSnakes()
    {
        int snakesCount = Random.Range(5, 10);
        Debug.Log(snakesCount);
        int randomTileStart;
        for (int i = 0; i < snakesCount; i++) // use < not <=
        {
            for(int y = 0; y < 10; y++)
            {
                // tileID range: 0 .. tiles.Length-1
                randomTileStart = Random.Range(20, tiles.Length);
                // find start tile by ID

                Tile startTile = FindStartTile(randomTileStart);
                if (startTile == null)
                {
                    Debug.Log($"No valid start tile at {randomTileStart} " + y);
                    //i--;
                    
                    continue;
                }

                // pick an end tile ID some steps ahead; clamp so it stays in range
                int minEnd = randomTileStart - 10;
                int maxEnd = randomTileStart - 35;
                minEnd = Mathf.Clamp(minEnd, 0, tiles.Length - 10);
                maxEnd = Mathf.Clamp(maxEnd, 0, tiles.Length - 10);
                if (minEnd > maxEnd) { int tmp = minEnd; minEnd = maxEnd; maxEnd = tmp; }

                int randomTileEnd = Random.Range(minEnd, maxEnd + 1); // inclusive upper bound

                // find end tile by ID
                Tile endTile = null;
                foreach (Tile ts in tiles)
                {
                    if (ts == null) continue;
                    if (ts.tileID == randomTileEnd)
                    {
                        endTile = ts;
                        break;
                    }
                }

                if (endTile == null) continue; // no matching end tile found — skip

                // mark functions
                startTile.tileFunction = 3;
                endTile.tileFunction = 4;

                // instantiate ladder at the ladder position child of the start tile
                Vector3 pos = startTile.GetComponentInChildren<SnakePos>().transform.position;
                GameObject newSnake = Instantiate(snakePrefab, pos, Quaternion.identity, transform);

                // set ladder IDs on Ladder component
                Snake snakeComp = newSnake.GetComponent<Snake>();
                if (snakeComp != null)
                {
                    snakeComp.startTile = startTile.tileID;
                    snakeComp.endTile = endTile.tileID;
                }

                // compute direct direction from start to end
                Vector3 targetDirection = endTile.transform.position - startTile.transform.position;
                if (targetDirection.sqrMagnitude <= Mathf.Epsilon) continue;

                // use LookRotation to face the target
                Quaternion look = Quaternion.LookRotation(targetDirection.normalized);

                // APPLY A YAW OFFSET if your ladder model's forward axis is rotated.
                // The common fix when things are off by -90° yaw is to multiply by this:
                Quaternion yawOffset = Quaternion.Euler(0f, -90f, 0f);
                // If it's off by +90°, change to Quaternion.Euler(0f, 90f, 0f).

                newSnake.transform.rotation = look * yawOffset;
                snakes.Add(newSnake);
                Debug.Log($"{startTile.tileID} to {endTile.tileID}");
                break;
            }
        }
    }
    Tile FindTileByID(int id)
    {
        foreach (Tile t in tiles)
        {
            if (t != null && t.tileID == id)
                return t;
        }
        return null;
    }

    Tile FindStartTile(int centerID)
    {
        // Find the center tile
        Tile center = FindTileByID(centerID);
        if (center == null) return null;

        // Check +-5 range
        for (int offset = -5; offset <= 5; offset++)
        {
            int checkID = centerID + offset;
            Tile checkTile = FindTileByID(checkID);

            if (checkTile == null) continue;

            // If ANY neighbor is occupied → invalid start tile
            if (checkTile.tileFunction != 0)
                return null;
        }

        // All checks passed
        return center;
    }

    
}
