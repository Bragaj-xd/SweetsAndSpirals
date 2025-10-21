using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI winScreenText;
    public FloorManager floorManager;
    public int playerToMove = 0;
    public bool redToMove = true;
    public GameObject redPlayer;
    public GameObject bluePlayer;
    public DiceRoll diceRoll;

    private int lastWheelNum = 0;

    void Start()
    {
        // Safety checks
        if (floorManager == null)
        {
            Debug.LogError("GameManager: FloorManager is not assigned!");
            return;
        }

        if (redPlayer == null || bluePlayer == null)
        {
            Debug.LogError("GameManager: Red or Blue player not assigned!");
            return;
        }

        // Find starting tile (ID = 0)
        foreach (Tile t in floorManager.tiles)
        {
            if (t.tileID == 0)
            {
                // Find markers
                Transform redMarker = t.transform.Find("Red Position");
                Transform blueMarker = t.transform.Find("Blue Position");

                if (redMarker != null)
                    redPlayer.transform.position = redMarker.position;
                else
                    Debug.LogWarning($"Tile {t.tileID} missing RedPos marker!");

                if (blueMarker != null)
                    bluePlayer.transform.position = blueMarker.position;
                else
                    Debug.LogWarning($"Tile {t.tileID} missing BluePos marker!");

                redPlayer.GetComponent<PlayerStats>().currentPos = 0;
                bluePlayer.GetComponent<PlayerStats>().currentPos = 0;

                break;
            }
        }
        if(winScreenText != null)
        {
            winScreenText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Check if dice was just spun
        if (diceRoll.wheelSpun > lastWheelNum)
        {
            // Increment move counter
            playerToMove++;
            lastWheelNum = diceRoll.wheelSpun;

            // Decide who moves this turn
            if (playerToMove % 2 == 0)
            {
                redToMove = true;
                UpdateRedPosition(); // Move red player once
            }
            else
            {
                redToMove = false;
                UpdateBluePosition(); // Move blue player once
            }
        }
        if (redPlayer.GetComponent<PlayerStats>().currentPos >= 99)
        {
            Debug.Log("red player won");
            winScreenText.gameObject.SetActive(true);
            winScreenText.text = "Red Player Won";
        }
        if(bluePlayer.GetComponent<PlayerStats>().currentPos >= 99)
        {
            Debug.Log("blue player won");
            winScreenText.gameObject.SetActive(true);   
            winScreenText.text = "Blue Player Won";
        }
    }

    void FindTile(int currentPlayerPos)
    {
        int targetTileID = currentPlayerPos + diceRoll.wheelValue;

        foreach (Tile t in floorManager.tiles)
        {
            if (t.tileID == targetTileID)
            {
                if (redToMove)
                {
                    Transform redMarker = t.transform.Find("Red Position");
                    if (redMarker != null)
                    {
                        redPlayer.transform.position = redMarker.position;
                        redPlayer.GetComponent<PlayerStats>().currentPos = t.tileID;
                    }
                    else
                    {
                        Debug.LogWarning($"Tile {t.tileID} missing RedPos marker!");
                    }
                }
                else
                {
                    Transform blueMarker = t.transform.Find("Blue Position");
                    if (blueMarker != null)
                    {
                        bluePlayer.transform.position = blueMarker.position;
                        bluePlayer.GetComponent<PlayerStats>().currentPos = t.tileID;
                    }
                    else
                    {
                        Debug.LogWarning($"Tile {t.tileID} missing BluePos marker!");
                    }
                }

                break;
            }
        }
    }

    void UpdateRedPosition()
    {
        FindTile(redPlayer.GetComponent<PlayerStats>().currentPos);
    }

    void UpdateBluePosition()
    {
        FindTile(bluePlayer.GetComponent<PlayerStats>().currentPos);
    }
}
