using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public int width = 10;
    public int height = 10;
    public float tileSize = 2f;

    [HideInInspector] public Tile[,] tiles;

    void Awake()
    {
        GenerateFloor();
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
}
