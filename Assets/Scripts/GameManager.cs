using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.UI;

/*
    <----------------------------------------------------------------------------------------------------------------------->
        TO DO LIST:
    ---------------------
            Fixes
        -------------------
                - fix player position after moving from SaL (asi?)
        -------------------
            Player Actions
        -------------------
                - if step on player, move that player back one tile
        -------------------
            Cards
        -------------------
                - add cards
                - add card bundle
                - add card shuffling

                - cards to keep

        -------------------
            SaL spawning
        -------------------
                - pre game SaL placement phase
                
        -------------------
            UI
        -------------------
                - updated UI (graphics shit)
                - Dynamic Camera
        -------------------
            Multiplayer
        -------------------
                - redo this shit to multiplayer version (gg we are cooked)
                - keep both versions local/online multiplayer


    <----------------------------------------------------------------------------------------------------------------------->
*/

public class GameManager : MonoBehaviour
{
    public Button rollTheDice;
    public FloorManager floorManager;
    public int playerToMove = 0;
    public GameObject activePlayer;
    public List<GameObject> players;
    public DiceRoll diceRoll;
    public GameObject cardPrefab;
    public bool rolledThree;
    public GameObject rollThree;
    public GameObject cardPos;
    public GameObject cardPosDiscard;
    public TextMeshProUGUI cardName;
    public TextMeshProUGUI cardText;
    
    public List<string> playerPositionNames = new List<string>()
            {
                "Red Position",
                "Blue Position",
                "Green Position",
                "Yellow Position"
            };
    public bool isMoving = false;


    private int lastWheelNum = 0;

    void Start()
    {
        // Safety checks
        if (floorManager == null)
        {
            Debug.LogError("GameManager: FloorManager is not assigned!");
            return;
        }

        if (players[0] == null || players[1] == null)
        {
            Debug.LogError("GameManager: Red or Blue player not assigned!");
            return;
        }
        if(cardText != null)
        {
            cardText.gameObject.SetActive(false);
        }
        if(cardName != null)
        {
            cardName.gameObject.SetActive(false);
        }

        // Find starting tile (ID = 0)
        foreach (Tile t in floorManager.tiles)
        {
            if (t.tileID == 0)
            {
                // Find markers
                Transform redMarker = t.transform.Find("Red Position");
                Transform blueMarker = t.transform.Find("Blue Position");
                Transform greenMarker = t.transform.Find("Green Position");
                Transform yellowMarker = t.transform.Find("Yellow Position");

                

                if (redMarker != null)
                    players[0].transform.position = redMarker.position;
                else
                    Debug.LogWarning($"Tile {t.tileID} missing RedPos marker!");

                if (blueMarker != null)
                    players[1].transform.position = blueMarker.position;
                else
                    Debug.LogWarning($"Tile {t.tileID} missing BluePos marker!");
                if (greenMarker != null)
                    players[2].transform.position = greenMarker.position;
                else
                    Debug.LogWarning($"Tile {t.tileID} missing BluePos marker!");
                if (yellowMarker != null)
                    players[3].transform.position = yellowMarker.position;
                else
                    Debug.LogWarning($"Tile {t.tileID} missing BluePos marker!");
                players[0].GetComponent<PlayerStats>().currentPos = 0;
                players[1].GetComponent<PlayerStats>().currentPos = 0;
                players[2].GetComponent<PlayerStats>().currentPos = 0;
                players[3].GetComponent<PlayerStats>().currentPos = 0;

                break;
            }
        }
        activePlayer = players[playerToMove];
    }

    void Update()
    {
        // Check if dice was just spun
        activePlayer = players[playerToMove];

        PlayerStats stats = activePlayer.GetComponent<PlayerStats>();
        if (stats.skipNextTurn)
        {
            Debug.Log($"{activePlayer.name} skips this turn due to caramel");

            stats.skipNextTurn = false; // consume skip
            playerToMove = (playerToMove + 1) % players.Count;
            return;
        }
        if (diceRoll.wheelSpun > lastWheelNum)
        {
            rollTheDice.interactable = false;

            // Increment move counter
            
            lastWheelNum = diceRoll.wheelSpun;

            if (diceRoll.wheelValue != 3)
            {           
                    UpdatePlayerPosition(activePlayer);
            }
            if(diceRoll.wheelValue == 3)
            {
                rolledThree = true;   
            }      
        }
    }
    void FindTile(int currentPlayerPos)
    {
        int targetID;
        
        if(activePlayer.GetComponent<PlayerStats>().jamInUse > 0)
            {
                targetID = currentPlayerPos + diceRoll.wheelValue -1;
                activePlayer.GetComponent<PlayerStats>().jamInUse -=1;
                Debug.Log(activePlayer.GetComponent<PlayerStats>().jamInUse);
            }
        else
            targetID = currentPlayerPos + diceRoll.wheelValue;

            
        
        GameObject player = players[playerToMove];

        if (!isMoving)
        {
            StartCoroutine(MovePlayerTileByTile(player, targetID));
        }
    }

    public void UpdatePlayerPosition(GameObject player)
    {
        FindTile(player.GetComponent<PlayerStats>().currentPos);
    }
    public void AddChanceCard(GameObject player)
    {
        GameObject newCard = SpawnCard();

        player.GetComponent<PlayerStats>().cards.Add(newCard);
        //rollTheDice.interactable = true;
        Debug.Log($"Added card {newCard.name} to {player.name}'s cards!");
    }
    
    int PickRandomCard()
    {
        return Random.Range(0,6);
    }
    GameObject SpawnCard()
    {
        GameObject newCard = Instantiate(cardPrefab, cardPos.transform.position, Quaternion.identity, transform);
        newCard.name = "Card";
        newCard.GetComponent<CardStats>().cardId = PickRandomCard();
        switch(newCard.GetComponent<CardStats>().cardId)
        {
            case 0:
                cardName.text = "Poki Ladder";
                cardText.text = "Place a Ladder on board to create a shortcut. Move it with mouse and rotate with Scroll Wheel.";
                break;
            case 1:
                cardName.text = "Sour Snake";
                cardText.text = "Place a Snake on board to create an obstacle. Move it with mouse and rotate with Scroll Wheel.";
                break;
            case 2:
                cardName.text = "Slowing Jam";
                cardText.text = "Place a Jam on board. Slows movement of players by one for two rounds.";
                break;
            case 3:
                cardName.text = "Sticky Caramel";
                cardText.text = "Place a Caramel on board. Stops player for one round.";
                break;
            case 4:
                cardName.text = "Forgetful";
                cardText.text = "You forgot some of your sweets somewhere on the road. Move two places black.";
                break;
            case 5:
                cardName.text = "Sweet Rush";
                cardText.text = "After eating candies, you are full of energy. Move two places forward.";
                break;
        }         

        cardName.gameObject.SetActive(true);
        cardText.gameObject.SetActive(true);

        StartCoroutine(MoveCardToDiscard(newCard, 5f));
        

        return newCard;
    }
    Transform GetMarkerForPlayer(Tile tile, GameObject player)
    {
        if (tile == null || player == null)
        {
            Debug.Log("tile or player is null");
            return null;
        }
            

        int index = players.IndexOf(player);
        Debug.Log("index: " + index);
        if (index < 0 || index >= playerPositionNames.Count)
        {
            Debug.Log(playerPositionNames.Count);
            return null;
        }
        

        string posName = playerPositionNames[index];
        Debug.Log("posname " + posName);
        return tile.transform.Find(posName);
        
    }

    // main coroutine: moves player tile-by-tile from currentPos -> destinationID (inclusive)
    public IEnumerator MovePlayerTileByTile(GameObject player, int destinationID)
    {
        isMoving = true;
        destinationID = Mathf.Clamp(
            destinationID,
            0,
            floorManager.MaxTileID
        );
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
            Debug.Log(player);
            Transform marker = GetMarkerForPlayer(tile, player);
            Debug.Log(marker);
            if (marker == null)
            {
                Debug.LogWarning($"Tile {tile.tileID} missing marker for {player.name}");
            }
            else
            {
                player.transform.position = marker.position;
            }

            stats.currentPos = id;
            //Debug.Log($"{player.name} moved to {id}");

            // WAIT between steps (tweak delay as needed)
            yield return new WaitForSeconds(0.25f);

            if (id == destinationID)
            {
                if(tile.tileFunction != 7)
                    playerToMove = (playerToMove + 1) % players.Count;
                
                break;
            }
            
        }

        // After arrival, handle tile effects (ladders/snakes)
        yield return StartCoroutine(HandleTileEffects(player, destinationID));
        isMoving = false;
    }

    // check tileFunction and if snake/ladder move to endpoint (tile-by-tile)
    IEnumerator HandleTileEffects(GameObject player, int tileID)
    {
        Tile tile = floorManager.FindTileByID(tileID);
        Debug.Log(tile.tileFunction);
        if (tile == null) yield break;

        switch (tile.tileFunction)
        {
            case 0: // nothing xd
                rollTheDice.interactable = true;
                break;
            case 1: // ladder start
                Debug.Log($"{player.name} stepped on a ladder at {tileID}");
                foreach (GameObject l in floorManager.ladders)
                {
                    Ladder ladder = l.GetComponent<Ladder>();
                    Debug.Log(ladder);
                    if (ladder != null && ladder.startTile == tileID)
                    {
                        int endID = ladder.endTile;
                        // small pause before climbing
                        yield return new WaitForSeconds(0.2f);
                        yield return StartCoroutine(MoveAlongSegments(player, ladder.segmentPositions));

                        player.GetComponent<PlayerStats>().currentPos = endID;
                        SnapPlayerToTile(player, endID);
                        rollTheDice.interactable = true;
                        break; // assume only one ladder per start
                    }
                }
                break;
            case 2: // ladder end
                rollTheDice.interactable = true;
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
                        Debug.Log("move player");
                        yield return StartCoroutine(MoveAlongSegments(player, snake.segmentPositions));

                        player.GetComponent<PlayerStats>().currentPos = endID;
                        SnapPlayerToTile(player, endID);
                        rollTheDice.interactable = true;
                        break; // assume only one snake per start
                    }
                }
                break;
            case 4: // snake end
                rollTheDice.interactable = true;
                break;
            case 5: //jam
                Debug.Log($"{player.name} stepped on a jam at {tileID}");
                activePlayer.GetComponent<PlayerStats>().jamInUse = 2;
                rollTheDice.interactable = true;
                break;
            case 6: //caramel
                Debug.Log($"{player.name} stepped on a caramel at {tileID}");
                activePlayer.GetComponent<PlayerStats>().skipNextTurn = true;
                rollTheDice.interactable = true;
                break;
            case 7: //chance
                Debug.Log($"{player.name} stepped on a chance tile at {tileID}");
                activePlayer.GetComponent<PlayerActions>().PickCard();
                rollTheDice.interactable = false;
                break;
            // other tileFunctions (0,2,4) can be handled here if needed
            default:
                yield break;
        }
    }

    public void SnapPlayerToTile(GameObject player, int tileID)
    {
        Tile tile = floorManager.FindTileByID(tileID);
        if (tile == null) return;

        Transform marker = GetMarkerForPlayer(tile, player);
        if (marker != null)
            player.transform.position = marker.position;
            player.GetComponent<PlayerStats>().currentPos = tileID;
            playerToMove = (playerToMove + 1) % players.Count;

    }
    IEnumerator MoveAlongSegments(GameObject player, List<Transform> segments)
    {
        foreach (Transform seg in segments)
        {
            player.transform.position = seg.position;
            Debug.Log("moving player");

            // small delay between segment steps
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator MoveCardToDiscard(GameObject card, float delay)
    {
        if (card == null)
            yield break;

        yield return new WaitForSeconds(delay);
        cardName.gameObject.SetActive(false);
        cardText.gameObject.SetActive(false);

        if (card == null)
            yield break;

        card.transform.position = cardPosDiscard.transform.position;
    }

}

