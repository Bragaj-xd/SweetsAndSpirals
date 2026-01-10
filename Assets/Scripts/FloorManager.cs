using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject ladderPrefab;
    public GameObject ladder2Prefab;
    public GameObject ladder3Prefab;
    public GameObject snakePrefab;
    public int width = 10;
    public int height = 10;
    public int MaxTileID;
    public float tileSize = 2f;

    [HideInInspector] public Tile[,] tiles;
    public Tile finishTile;
    public List<GameObject> ladders = new List<GameObject>();
    public List<GameObject> snakes = new List<GameObject>();
    public List<GameObject> jams = new List<GameObject>();
    public List<GameObject> caramels = new List<GameObject>();

    void Awake()
    {
        MaxTileID = width * height;
        GenerateFloor();
        //generateSaL();
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

    public int GetRow(int tileID)
        {
            return tileID / width;
        }

        public bool IsRowReversed(int tileID)
        {
            return GetRow(tileID) % 2 == 1;
        }
    public int GetSerpentineTileDelta(int startTileID, Vector3 dir, int boardWidth)
    {
        dir = dir.normalized;
        int row = startTileID / boardWidth;
        bool reversed = row % 2 == 1;

        // Vertical movement
        if (Mathf.Abs(dir.z) > Mathf.Abs(dir.x))
        {
            return dir.z > 0
                ? boardWidth      // up
                : -boardWidth;    // down
        }

        // Horizontal movement
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.z))
        {
            if (!reversed)
                return dir.x > 0 ? 1 : -1;
            else
                return dir.x > 0 ? -1 : 1;
        }

        // Diagonal
        int dx;
        if (!reversed)
            dx = dir.x > 0 ? 1 : -1;
        else
            dx = dir.x > 0 ? -1 : 1;

        int dz = dir.z > 0 ? boardWidth : -boardWidth;

        return dx + dz;
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
        if(tile.tileID == (width * height) - 1)
        {
            finishTile = tile;
            Debug.Log(finishTile);
        }
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
        int SalCount = Random.Range(5,15);
        int nextSaLID;
        int randomSaLID;
        int previousSaLID = 0;
        int isLadder;
        GameObject SaLPrefab;
        Vector3 posStart = new Vector3(0,0,0);
        Vector3 posEnd = new Vector3(0,0,0);
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
                    posStart = startTile.GetComponentInChildren<SnakePos>().transform.position;
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
                }
                    
            }
            
            if(isLadder == 0)
            {
                GameObject newSaL = BuildSnake(startTile,endTile);
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
                GameObject newSaL = BuildLadder(startTile,endTile);
                ladders.Add(newSaL);
                Ladder ladderComp = newSaL.GetComponent<Ladder>();
                if (ladderComp != null)
                {
                    ladderComp.startTile = startTile.tileID;
                    ladderComp.endTile = endTile.tileID;
                }
            }
            previousSaLID = nextSaLID;
        }
    }

    GameObject BuildLadder(Tile startTile, Tile endTile)
    {
        Vector3 startPos = startTile.GetComponentInChildren<SaLPos>().transform.position;
        Vector3 endPos = endTile.GetComponentInChildren<SaLPos>().transform.position;

        Vector3 dir = (endPos - startPos).normalized;
        float totalDistance = Vector3.Distance(startPos, endPos);

        float segmentLength = 1f;  // or measure from prefab bounds
        int segmentCount = Mathf.CeilToInt(totalDistance / segmentLength);

        GameObject ladderRoot = new GameObject("Ladder"); 
        ladderRoot.transform.parent = this.transform;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 pos = startPos + dir * (i * segmentLength);
            GameObject seg = Instantiate(ladderPrefab, pos, Quaternion.LookRotation(dir), ladderRoot.transform);
        }

        Ladder ladderComp = ladderRoot.AddComponent<Ladder>();
        ladderComp.startTile = startTile.tileID;
        ladderComp.endTile   = endTile.tileID;

        return ladderRoot;
    }
    GameObject BuildSnake(Tile startTile, Tile endTile)
    {
        Vector3 startPos = startTile.GetComponentInChildren<SnakePos>().transform.position;
        Vector3 endPos = endTile.GetComponentInChildren<SnakePos>().transform.position;

        // Direction the snake segments will follow
        Vector3 dir = (endPos - startPos).normalized;
        float totalDistance = Vector3.Distance(startPos, endPos);

        // Length of a snake segment (adjust to match your model)
        float segmentLength = 1.0f;

        int segmentCount = Mathf.CeilToInt(totalDistance / segmentLength);

        // Container object for the whole snake
        GameObject snakeRoot = new GameObject("Snake");
        snakeRoot.transform.parent = this.transform;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 pos = startPos + dir * (i * segmentLength);

            // Spawn one snake piece
            GameObject segment = Instantiate(
                snakePrefab, 
                pos, 
                Quaternion.LookRotation(dir), 
                snakeRoot.transform
            );
        }

        // Add script to root
        Snake snakeComp = snakeRoot.AddComponent<Snake>();
        snakeComp.startTile = startTile.tileID;
        snakeComp.endTile   = endTile.tileID;

        return snakeRoot;
    }
    
}
