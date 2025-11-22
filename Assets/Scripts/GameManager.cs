using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public TextMeshProUGUI winScreenText;
    public FloorManager floorManager;
    public int playerToMove = 0;
    public bool redToMove = true;
    public GameObject redPlayer;
    public GameObject bluePlayer;
    public DiceRoll diceRoll;
    public GameObject cardPrefab;

    public bool playerInMovement;

    public bool inputMenu;

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

    public void OnMenu(InputAction.CallbackContext context) => inputMenu = context.ReadValueAsButton();
    void Update()
    {
        if(inputMenu)
        {
            Application.Quit();
        }
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
                if (diceRoll.wheelValue <= 6)
                    UpdatePlayerPosition(redPlayer); // Move red player once
                else
                    AddChanceCard(redPlayer);
            }
            else
            {
                redToMove = false;
                if (diceRoll.wheelValue <= 6)
                    UpdatePlayerPosition(bluePlayer); // Move blue player once
                else
                    AddChanceCard(bluePlayer);
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
        Transform redMarker = null;
        Transform blueMarker = null;
        foreach (Tile t in floorManager.tiles)
        {
            if (t.tileID == targetTileID)
            {
                switch (t.tileFunction)
                {
                    case 0:
                        if (redToMove)
                        {
                            redMarker = t.transform.Find("Red Position");
                        }
                        else
                        {
                            blueMarker = t.transform.Find("Blue Position");
                        }
                        break;
                    case 1:
                        Debug.Log("stepped on a ladder");
                        foreach (GameObject l in floorManager.ladders)
                        {
                            if (l.GetComponent<Ladder>().startTile == targetTileID)
                            {
                                int newTargetID = l.GetComponent<Ladder>().endTile;
                                foreach (Tile ts in floorManager.tiles)
                                {
                                    if (ts.tileID == newTargetID)
                                    {
                                        if (redToMove)
                                        {
                                            redMarker = ts.transform.Find("Red Position");
                                        }
                                            
                                        else
                                        {
                                            blueMarker = ts.transform.Find("Blue Position");
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        if (redToMove)
                        {
                            redMarker = t.transform.Find("Red Position");
                        }
                        else
                        {
                            blueMarker = t.transform.Find("Blue Position");
                        }
                        break;
                    case 3:
                        Debug.Log("stepped on a snake");
                        foreach (GameObject s in floorManager.snakes)
                        {
                            if (s.GetComponent<Snake>().startTile == targetTileID)
                            {
                                int newTargetID = s.GetComponent<Snake>().endTile;
                                foreach (Tile ts in floorManager.tiles)
                                {
                                    if (ts.tileID == newTargetID)
                                    {
                                        if (redToMove)
                                        {
                                            redMarker = ts.transform.Find("Red Position");
                                        }
                                            
                                        else
                                        {
                                            blueMarker = ts.transform.Find("Blue Position");
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 4:
                        if (redToMove)
                        {
                            redMarker = t.transform.Find("Red Position");
                        }
                        else
                        {
                            blueMarker = t.transform.Find("Blue Position");
                        }
                        break;
                }
                if (redToMove)
                {
                    if (redMarker != null)
                    {
                        StartCoroutine(MovePlayerTileByTile(
                            redPlayer,
                            redPlayer.GetComponent<PlayerStats>().currentPos + 1,
                            redMarker.GetComponentInParent<Tile>().tileID
                        ));

                        //redPlayer.transform.position = redMarker.position;
                        //redPlayer.GetComponent<PlayerStats>().currentPos = redMarker.GetComponentInParent<Tile>().tileID;
                    }
                    else
                    {
                        Debug.LogWarning($"Tile {t.tileID} missing RedPos marker!");
                    }
                }
                else
                {
                    if (blueMarker != null)
                    {
                        StartCoroutine(MovePlayerTileByTile(
                            bluePlayer,
                            bluePlayer.GetComponent<PlayerStats>().currentPos + 1,
                            blueMarker.GetComponentInParent<Tile>().tileID
                        ));
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

    void UpdatePlayerPosition(GameObject player)
    {
        FindTile(player.GetComponent<PlayerStats>().currentPos);
    }
    void AddChanceCard(GameObject player)
    {
        GameObject newCard = SpawnCard();

        player.GetComponent<PlayerStats>().cards.Add(newCard);

        Debug.Log($"Added card {newCard.name} to {player.name}'s cards!");
    }
    
    int PickRandomCard()
    {
        return Random.Range(0, 8);
    }
    GameObject SpawnCard()
    {
        Vector3 pos = new Vector3(0f, 0f);
        GameObject newCard = Instantiate(cardPrefab, pos, Quaternion.identity, transform);
        newCard.name = "Card";
        newCard.GetComponent<CardStats>().cardId = PickRandomCard();

        return newCard;
    }

    IEnumerator MovePlayerTileByTile(GameObject player, int start, int end)
    {
        playerInMovement = true;
        for (int i = start; i <= end; i++)
        {
            Tile tile = floorManager.FindTileByID(i);
            if (tile == null) yield break;

            // Move player to this tile's marker
            string markerName = redToMove ? "Red Position" : "Blue Position";
            Transform marker = tile.transform.Find(markerName);

            player.transform.position = marker.position;
            player.GetComponent<PlayerStats>().currentPos = i;

            Debug.Log($"Step on tile {i}");

            // Wait before moving to the next tile
            yield return new WaitForSeconds(0.5f); // adjust speed here
            Debug.Log(playerInMovement);
        }
        playerInMovement = false;
        Debug.Log(playerInMovement);
    }
}
