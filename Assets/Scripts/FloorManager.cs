using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject ladderPrefab;
    public int width = 10;
    public int height = 10;
    public float tileSize = 2f;

    [HideInInspector] public Tile[,] tiles;
    public List<GameObject> ladders = new List<GameObject>();

    void Awake()
    {
        GenerateFloor();
        GenerateLadders();
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
        int ladderCount = Random.Range(5, 15);

        for (int i = 0; i < ladderCount; i++) // use < not <=
        {
            int randomTileStart = Random.Range(0, tiles.Length); // tileID range: 0 .. tiles.Length-1

            // find start tile by ID
            Tile startTile = null;
            foreach (Tile t in tiles)
            {
                if (t == null) continue;
                if (t.tileID == randomTileStart)
                {
                    startTile = t;
                    break;
                }
            }

            // if not found or already used for ladder start/end, pick another
            if (startTile == null || startTile.tileFunction == 1 || startTile.tileFunction == 2)
            {
                // try a few more random picks before skipping
                bool found = false;
                for (int tries = 0; tries < 8 && !found; tries++)
                {
                    randomTileStart = Random.Range(0, tiles.Length);
                    foreach (Tile t in tiles)
                    {
                        if (t == null) continue;
                        if (t.tileID == randomTileStart && t.tileFunction != 1 && t.tileFunction != 2)
                        {
                            startTile = t;
                            found = true;
                            break;
                        }
                    }
                }
                if (!found) continue; // couldn't find a good start this iteration
            }

            // pick an end tile ID some steps ahead; clamp so it stays in range
            int minEnd = randomTileStart + 10;
            int maxEnd = randomTileStart + 25;
            minEnd = Mathf.Clamp(minEnd, 0, tiles.Length - 1);
            maxEnd = Mathf.Clamp(maxEnd, 0, tiles.Length - 1);
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
        }
    }
    
}
