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
        generateSaL();
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
        newTile.name = $"Tile_{idCounter}";

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


    public Tile FindTileByID(int id)
    {
        foreach (Tile t in tiles)
        {
            if (t != null && t.tileID == id)
                return t;
        }
        return null;
    }

    Tile FindTile(int centerID)
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


    void generateSaL()
    {
        List<int> SaLEndTiles = new List<int> {8, 24, 23, 9, 22, 30, 21, 12, 18, 19, 11, 20};
        //List<int> SnakesEndTiles = new List<int> {-8, -24, -23, -9, -22, -30, -21, -12, -18, -19, -11, -20};
        int SalCount = Random.Range(5,15);
        int nextSaLID;
        int randomSaLID;
        int previousSaLID = 0;
        int isLadder;
        GameObject SaLPrefab;
        Vector3 pos = new Vector3(0,0,0);
        for(int i = 0; i < SalCount + 1; i++)
        {
            randomSaLID = Random.Range(5, 10);
            nextSaLID = previousSaLID + randomSaLID;
            if(nextSaLID > tiles.Length - 2)
                break;
            Tile startTile = FindTileByID(nextSaLID);
            Tile endTile = null;
            
            isLadder = Random.Range(0,2);
            if(nextSaLID < 20 && isLadder == 0)
            {
                isLadder = 1;
                Debug.Log("changing snake to ladder");
            }
            if(nextSaLID > tiles.Length - 20 && isLadder == 1)
            {
                isLadder = 0;
                Debug.Log("Changing ladder to snake");
            }
            if(isLadder == 0)
            {
                SaLPrefab = snakePrefab;
                int randomTileEnd = Random.Range(0,12);
                endTile = FindTileByID(Mathf.Clamp(nextSaLID - SaLEndTiles[randomTileEnd], 0 , tiles.Length - 10));

                Debug.Log("snake " + startTile + " " + endTile);
                if(startTile != null)
                {
                    startTile.tileFunction = 3;
                    endTile.tileFunction = 4;
                    pos = startTile.GetComponentInChildren<SnakePos>().transform.position;
                }
                    
                
            }
            else
            {
                SaLPrefab = ladderPrefab;
                int randomTileEnd = Random.Range(0,12);
                endTile = FindTileByID(Mathf.Clamp(nextSaLID + SaLEndTiles[randomTileEnd], 0 , tiles.Length - 10));
                
                Debug.Log("ladder: " + startTile + " " + endTile);
                if(startTile != null)
                {
                    startTile.tileFunction = 1;
                    endTile.tileFunction = 2;
                    pos = startTile.GetComponentInChildren<LadderPos>().transform.position;
                }
                    
            }
            
            previousSaLID = nextSaLID;
            GameObject newSaL = Instantiate(SaLPrefab, pos, Quaternion.identity, transform);
            if(isLadder == 0)
            {
                snakes.Add(newSaL);
                Snake snakeComp = newSaL.GetComponent<Snake>();
                if (snakeComp != null)
                {
                    snakeComp.startTile = startTile.tileID;
                    snakeComp.endTile = endTile.tileID;
                }
            }
            else
            {
                ladders.Add(newSaL);
                Ladder ladderComp = newSaL.GetComponent<Ladder>();
                if (ladderComp != null)
                {
                    ladderComp.startTile = startTile.tileID;
                    ladderComp.endTile = endTile.tileID;
                }
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

            newSaL.transform.rotation = look * yawOffset;
        
        }
    }

    
}
