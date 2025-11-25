using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;

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

    private bool isMoving = false;

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
        int targetID = currentPlayerPos + diceRoll.wheelValue;
        GameObject player = redToMove ? redPlayer : bluePlayer;

        if (!isMoving)
        {
            StartCoroutine(MovePlayerTileByTile(player, targetID));
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
    Transform GetMarkerForPlayer(Tile tile, GameObject player)
    {
        if (tile == null) return null;
        bool movingRed = (player == redPlayer);
        return movingRed ? tile.transform.Find("Red Position") : tile.transform.Find("Blue Position");
    }

    // main coroutine: moves player tile-by-tile from currentPos -> destinationID (inclusive)
    IEnumerator MovePlayerTileByTile(GameObject player, int destinationID)
    {
        isMoving = true;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null)
        {
            Debug.LogError("PlayerStats missing on player!");
            isMoving = false;
            yield break;
        }

        int start = stats.currentPos;
        if (start == destinationID)
        {
            // already there - still check tile function (in case of immediate ladder/snake)
            yield return StartCoroutine(HandleTileEffects(player, destinationID));
            isMoving = false;
            yield break;
        }

        int step = destinationID > start ? 1 : -1;

        for (int id = start + step; ; id += step)
        {
            Tile tile = floorManager.FindTileByID(id);
            if (tile == null)
            {
                Debug.LogWarning($"MovePlayerTileByTile: no tile with ID {id}");
                break;
            }

            Transform marker = GetMarkerForPlayer(tile, player);
            if (marker == null)
            {
                Debug.LogWarning($"Tile {tile.tileID} missing marker for {player.name}");
            }
            else
            {
                player.transform.position = marker.position;
            }

            stats.currentPos = id;
            Debug.Log($"{player.name} moved to {id}");

            // WAIT between steps (tweak delay as needed)
            yield return new WaitForSeconds(0.25f);

            if (id == destinationID) break;
        }

        // After arrival, handle tile effects (ladders/snakes)
        yield return StartCoroutine(HandleTileEffects(player, destinationID));

        isMoving = false;
    }

    // check tileFunction and if snake/ladder move to endpoint (tile-by-tile)
    IEnumerator HandleTileEffects(GameObject player, int tileID)
    {
        Tile tile = floorManager.FindTileByID(tileID);
        if (tile == null) yield break;

        switch (tile.tileFunction)
        {
            case 1: // ladder start
                Debug.Log($"{player.name} stepped on a ladder at {tileID}");
                foreach (GameObject l in floorManager.ladders)
                {
                    Ladder ladder = l.GetComponent<Ladder>();
                    if (ladder != null && ladder.startTile == tileID)
                    {
                        int endID = ladder.endTile;
                        // small pause before climbing
                        yield return new WaitForSeconds(0.2f);
                        yield return StartCoroutine(MoveAlongSegments(player, ladder.segmentPositions));

                        player.GetComponent<PlayerStats>().currentPos = endID;
                        break; // assume only one ladder per start
                    }
                }
                break;

            case 3: // snake start
                Debug.Log($"{player.name} stepped on a snake at {tileID}");
                foreach (GameObject s in floorManager.snakes)
                {
                    Snake snake = s.GetComponent<Snake>();
                    if (snake != null && snake.startTile == tileID)
                    {
                        int endID = snake.endTile;
                        // small pause before sliding
                        yield return new WaitForSeconds(0.2f);
                        yield return StartCoroutine(MoveAlongSegments(player, snake.segmentPositions));

                        player.GetComponent<PlayerStats>().currentPos = endID;
                        break; // assume only one snake per start
                    }
                }
                break;

            // other tileFunctions (0,2,4) can be handled here if needed
            default:
                yield break;
        }
    }
    IEnumerator MoveAlongSegments(GameObject player, List<Transform> segments)
    {
        foreach (Transform seg in segments)
        {
            player.transform.position = seg.position;

            // small delay between segment steps
            yield return new WaitForSeconds(0.15f);
        }
    }
}
