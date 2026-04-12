using Unity.VisualScripting;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.Netcode;

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
    public TextMeshProUGUI cardName;
    public TextMeshProUGUI cardText;
    public GameObject wheel;
    public GameObject cardPosDiscard;
    public List<GameObject> cardPrefabs;
    
    public List<string> playerPositionNames = new List<string>()
            {
                "Red Position",
                "Blue Position",
                "Green Position",
                "Yellow Position"
            };
    public bool isMoving = false;

    private int lastWheelNum = 0;
    private bool isNetworkEnabled = false;
    private NetworkGameManager networkGameManager;
    private int maxPlayers = 4;
    
    // Coroutine tracking for memory leak prevention
    private Coroutine activeMovementCoroutine = null;
    private Dictionary<GameObject, Coroutine> playerMovementCoroutines = new Dictionary<GameObject, Coroutine>();

    private void Awake()
    {
        // Initialize early so PlayerActions can find us
        if (floorManager == null)
        {
            floorManager = GetComponent<FloorManager>();
            if (floorManager == null)
            {
                GameObject floorManagerGO = GameObject.FindGameObjectWithTag("FloorManager");
                if (floorManagerGO != null)
                    floorManager = floorManagerGO.GetComponent<FloorManager>();
            }
        }

        // Initialize DiceRoll
        if (diceRoll == null)
        {
            diceRoll = GetComponent<DiceRoll>();
            if (diceRoll != null)
            {
                //Debug.Log($"[GameManager] DiceRoll found on self");
            }
        }

        // Ensure players list exists (even if empty for networked games)
        if (players == null)
        {
            players = new List<GameObject>();
        }

        //Debug.Log($"[GameManager] Awake - FloorManager: {(floorManager != null ? "found" : "NOT found")}, DiceRoll: {(diceRoll != null ? "found" : "NOT found")}, Players: {players.Count}");
    }

    private void OnDestroy()
    {
        // Clean up all coroutines to prevent memory leaks
        CleanupCoroutines();
    }

    /// <summary>
    /// Stops all active player movement coroutines to prevent memory leaks
    /// </summary>
    private void CleanupCoroutines()
    {
        foreach (var kvp in playerMovementCoroutines)
        {
            if (kvp.Value != null)
            {
                StopCoroutine(kvp.Value);
            }
        }
        playerMovementCoroutines.Clear();
        
        if (activeMovementCoroutine != null)
        {
            StopCoroutine(activeMovementCoroutine);
            activeMovementCoroutine = null;
        }
    }

    public void SetGameMode(bool enableNetworking)
    {
        isNetworkEnabled = enableNetworking;
        if (enableNetworking)
        {
            networkGameManager = GetComponent<NetworkGameManager>();
            if (networkGameManager == null)
            {
                Debug.LogWarning("[GameManager] NetworkGameManager not found - networking disabled");
                isNetworkEnabled = false;
            }
        }
        //Debug.Log($"[GameManager] Game mode set - Networking: {isNetworkEnabled}");
    }

    void Start()
    {
        SetGameMode(true); // default to networked mode, will disable if NetworkGameManager missing
        
        // Ensure DiceRoll is initialized
        if (diceRoll == null)
        {
            diceRoll = GetComponent<DiceRoll>();
            if (diceRoll == null)
            {
                GameObject gmGO = GameObject.FindGameObjectWithTag("GameManager");
                if (gmGO != null)
                    diceRoll = gmGO.GetComponent<DiceRoll>();
            }
            if (diceRoll == null)
            {
                diceRoll = FindAnyObjectByType<DiceRoll>();
            }
            
            if (diceRoll != null)
            {
                //Debug.Log("[GameManager] DiceRoll initialized in Start()");
            }
                
            else
                Debug.LogError("[GameManager] DiceRoll NOT FOUND anywhere in scene!");
        }

        // Safety checks
        if (floorManager == null)
        {
            Debug.LogError("GameManager: FloorManager is not assigned!");
            return;
        }

        // For local mode, check if players are pre-assigned
        // For networked mode, players will be added via RegisterPlayer() as they join
        if (!isNetworkEnabled && players.Count < 2)
        {
            Debug.LogError("GameManager: At least 2 players must be assigned for local mode!");
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

        // Initialize all registered players or wait for networked players
        InitializePlayerPositions();
        
        if (players.Count > 0)
        {
            activePlayer = players[playerToMove];
            //Debug.Log($"[GameManager] Game started with {players.Count} players. Active player: {activePlayer.name}");
        }
        else
        {
            Debug.LogWarning("[GameManager] No players registered yet - waiting for networked players to join");
        }
    }

    public void InitializePlayerPositions()
    {
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

                // Initialize each registered player
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i] == null) continue;
                    
                    Transform marker = null;
                    switch (i)
                    {
                        case 0: marker = redMarker; break;
                        case 1: marker = blueMarker; break;
                        case 2: marker = greenMarker; break;
                        case 3: marker = yellowMarker; break;
                    }

                    if (marker != null)
                    {
                        players[i].transform.position = marker.position;
                        PlayerStats stats = players[i].GetComponent<PlayerStats>();
                        if (stats != null)
                            stats.currentPos = 0;
                    }
                    else
                    {
                        //Debug.LogWarning($"[GameManager] Player {i} marker not found on starting tile");
                    }
                }

                break;
            }
        }
    }

    /// <summary>
    /// Register a player when they join (called by MultiplayerManager for networked games)
    /// </summary>
    public void RegisterPlayer(GameObject playerObject)
    {
        if (playerObject == null)
        {
            Debug.LogError("[GameManager] Attempted to register null player!");
            return;
        }

        if (players.Contains(playerObject))
        {
            //Debug.LogWarning($"[GameManager] Player {playerObject.name} already registered");
            return;
        }

        if (players.Count >= maxPlayers)
        {
            //Debug.LogError($"[GameManager] Maximum players ({maxPlayers}) reached, cannot register {playerObject.name}");
            return;
        }

        players.Add(playerObject);
        
        // Rename player to player1, player2, etc.
        playerObject.name = $"player{players.Count}";
        
        //Debug.Log($"[GameManager] Player registered: {playerObject.name} (Total: {players.Count}/{maxPlayers})");

        // Initialize this player's position
        if (players.Count == 1)
        {
            // First player - also become active player
            activePlayer = playerObject;
        }

        // Update player position on starting tile
        foreach (Tile t in floorManager.tiles)
        {
            if (t.tileID == 0)
            {
                string[] markerNames = { "Red Position", "Blue Position", "Green Position", "Yellow Position" };
                Transform marker = t.transform.Find(markerNames[players.Count - 1]);
                
                if (marker != null)
                {
                    playerObject.transform.position = marker.position;
                    PlayerStats stats = playerObject.GetComponent<PlayerStats>();
                    if (stats != null)
                        stats.currentPos = 0;
                }
                break;
            }
        }
    }

    public void OnMoveThreeButton()
    {
        if (activePlayer == null) return;

        PlayerActions actions = activePlayer.GetComponent<PlayerActions>();
        if (actions != null)
        {
            actions.MoveThree(); // your existing logic
        }
    }

    public void OnPickCardButton()
    {
        if (activePlayer == null) return;

        PlayerActions actions = activePlayer.GetComponent<PlayerActions>();
        if (actions != null)
        {
            actions.PickCard();
        }
    }

    void Update()
    {
        // Guard: check if we have players
        if (players.Count == 0)
            return;

        // Guard: ensure playerToMove is valid
        if (playerToMove >= players.Count)
        {
            playerToMove = 0;
            Debug.LogWarning("[GameManager] playerToMove out of range, reset to 0");
        }

        // Check if dice was just spun
        activePlayer = players[playerToMove];

        if (activePlayer == null)
            return;

        PlayerStats stats = activePlayer.GetComponent<PlayerStats>();
        if (stats == null)
            return;

        if (stats.skipNextTurn)
        {
            //Debug.Log($"{activePlayer.name} skips this turn due to caramel");

            stats.skipNextTurn = false; // consume skip
            EndPlayerTurn(); // Network-aware turn change
            return;
        }
        
        // Guard: ensure diceRoll is available
        if (diceRoll == null)
        {
            Debug.LogWarning("[GameManager] DiceRoll not initialized yet");
            return;
        }

        if (diceRoll.wheelSpun > lastWheelNum)
        {
            //Debug.Log($"[GameManager] Dice rolled! wheelSpun={diceRoll.wheelSpun}, wheelValue={diceRoll.wheelValue}");
            rollTheDice.interactable = false;

            // Increment move counter
            lastWheelNum = diceRoll.wheelSpun;

            if (diceRoll.wheelValue != 3)
            {
                //Debug.Log($"[GameManager] Moving player (not a 3)");
                UpdatePlayerPosition(activePlayer);
            }
            if(diceRoll.wheelValue == 3)
            {
                //Debug.Log("[GameManager] Rolled a 3!");
                rolledThree = true;   
            }      
        }
    }
    void FindTile(int currentPlayerPos)
    {
        //Debug.Log($"[GameManager] FindTile called: currentPlayerPos={currentPlayerPos}");

        // Guard: check if we have players
        if (players.Count == 0)
        {
            Debug.LogError("[GameManager] FindTile called but no players registered!");
            return;
        }

        // Guard: ensure playerToMove is valid
        if (playerToMove >= players.Count)
        {
            //Debug.LogError($"[GameManager] playerToMove ({playerToMove}) out of range for {players.Count} players");
            return;
        }

        // Guard: ensure diceRoll is available
        if (diceRoll == null)
        {
            Debug.LogError("[GameManager] DiceRoll is null in FindTile!");
            return;
        }

        int targetID;
        
        //Debug.Log($"[GameManager] diceRoll.wheelValue = {diceRoll.wheelValue}");

        PlayerStats activeStats = activePlayer.GetComponent<PlayerStats>();
        if(activeStats.jamInUse > 0)
        {
            targetID = currentPlayerPos + diceRoll.wheelValue - 1;
            activeStats.jamInUse -= 1;
            //Debug.Log($"[GameManager] Jam in use! targetID = {targetID}");
        }
        else if(activeStats.moveBackwards)
        {
            targetID = currentPlayerPos - diceRoll.wheelValue;
            activeStats.moveBackwards = false;
            //Debug.Log($"[GameManager] Moving backwards! targetID = {targetID}");
        }
        else
        {
            targetID = currentPlayerPos + diceRoll.wheelValue;
            //Debug.Log($"[GameManager] Normal move! currentPos={currentPlayerPos} + wheelValue={diceRoll.wheelValue} = targetID={targetID}");
        }

        GameObject player = players[playerToMove];

        if (!isMoving)
        {
            //Debug.Log($"[GameManager] Starting movement coroutine: {player.name} -> {targetID}");
            // Stop any existing coroutine for this player
            if (playerMovementCoroutines.ContainsKey(player))
            {
                StopCoroutine(playerMovementCoroutines[player]);
            }
            // Start new movement coroutine and track it
            Coroutine newCoro = StartCoroutine(MovePlayerTileByTile(player, targetID));
            playerMovementCoroutines[player] = newCoro;
        }
        else
        {
            Debug.LogWarning("[GameManager] Player is already moving, cannot start new movement");
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
        //Debug.Log($"Added card {newCard.name} to {player.name}'s cards!");
    }
    
    int PickRandomCard()
    {
        return Random.Range(0, cardPrefabs.Count);
        //return  12; // for testing, always pick move player card
    }
    GameObject SpawnCard()
    {
        cardPrefab = cardPrefabs[PickRandomCard()];
        GameObject newCard = Instantiate(cardPrefab, cardPos.transform.position, cardPos.transform.rotation, transform);

        // Cache component lookup
        CardStats cardStats = cardPrefab.GetComponent<CardStats>();
        if (cardStats != null)
        {
            // Cache transform lookups and only call GetComponent once per transform
            Transform cardNameTransform = newCard.transform.Find("Card Name");
            Transform cardTextTransform = newCard.transform.Find("Card Text");
            Transform cardImageTransform = newCard.transform.Find("Card Image");

            if (cardNameTransform != null)
            {
                TextMeshPro nameText = cardNameTransform.GetComponent<TextMeshPro>();
                if (nameText != null)
                    nameText.text = cardStats.cardName;
            }

            if (cardTextTransform != null)
            {
                TextMeshPro descText = cardTextTransform.GetComponent<TextMeshPro>();
                if (descText != null)
                    descText.text = cardStats.cardText;
            }

            if (cardImageTransform != null)
            {
                Renderer imageRenderer = cardImageTransform.GetComponent<Renderer>();
                if (imageRenderer != null)
                    imageRenderer.material = cardStats.cardImage;
            }
        }
        
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
    // NOTE: This is the public API - it broadcasts the RPC for network games
    public IEnumerator MovePlayerTileByTile(GameObject player, int destinationID)
    {
        // Network broadcast: notify all clients about this movement (only on the server/owner)
        if (isNetworkEnabled && networkGameManager != null)
        {
            int playerIndex = players.IndexOf(player);
            if (playerIndex >= 0)
            {
                //Debug.Log($"[GameManager] Network: Broadcasting movement of player {playerIndex} to tile {destinationID}");
                networkGameManager.MovePlayerTileByTileClientRpc(playerIndex, destinationID);
                // IMPORTANT: Don't execute the coroutine here - let the RPC handler do it
                yield break;
            }
        }

        // For local (non-network) mode, execute the movement directly
        yield return StartCoroutine(ExecuteMovePlayerTileByTile(player, destinationID));
    }

    /// <summary>
    /// Internal coroutine that executes the actual movement logic without broadcasting.
    /// Called by MovePlayerTileByTile (local mode) or by the RPC handler (network mode).
    /// </summary>
    private IEnumerator ExecuteMovePlayerTileByTile(GameObject player, int destinationID)
    {
        try
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
                    //Debug.LogWarning($"MovePlayerTileByTile: no tile with ID {id}");
                    break;
                }
                Transform marker = GetMarkerForPlayer(tile, player);
                if (marker == null)
                {
                    //Debug.LogWarning($"Tile {tile.tileID} missing marker for {player.name}");
                }
                else
                {
                    player.transform.position = marker.position;
                }

                stats.currentPos = id;

                // WAIT between steps (tweak delay as needed)
                yield return new WaitForSeconds(0.25f);

                if (id == destinationID)
                {
                    if(tile.tileFunction != 7)
                    {
                        // Check if player has reroll card active
                        if(!stats.reroll)
                        {
                            // Turn change - update both local and network
                            EndPlayerTurn();
                        }
                        else
                        {
                            // Player has reroll - will be consumed after tile effects
                            //Debug.Log($"{player.name} gets another roll from reroll card!");
                        }
                    }
                    
                    break;
                }
                
            }

            // After arrival, handle tile effects (ladders/snakes)
            if(!stats.ignoreTileEffects)
                yield return StartCoroutine(HandleTileEffects(player, destinationID));
            if(stats.ignoreTileEffects)
                stats.ignoreTileEffects = false;
            
            // Re-enable button if player has reroll
            if(stats.reroll)
            {
                rollTheDice.interactable = true;
                stats.reroll = false;
            }
        }
        finally
        {
            isMoving = false;
            // Remove coroutine from tracking dict
            if (playerMovementCoroutines.ContainsKey(player))
            {
                playerMovementCoroutines.Remove(player);
            }
        }
    }

    /// <summary>
    /// Public method for RPC handlers to execute movement without re-broadcasting.
    /// Called by NetworkGameManager.MovePlayerTileByTileClientRpc to avoid infinite RPC loops.
    /// </summary>
    public IEnumerator ExecuteNetworkMovePlayerTileByTile(int playerIndex, int destinationTileID)
    {
        if (playerIndex >= 0 && playerIndex < players.Count)
        {
            yield return StartCoroutine(ExecuteMovePlayerTileByTile(players[playerIndex], destinationTileID));
        }
        else
        {
            //Debug.LogWarning($"[GameManager] Invalid player index for network movement: {playerIndex}");
        }
    }

    /// <summary>
    /// Network-aware turn management.
    /// Updates local player turn and syncs via NetworkVariable if in network mode.
    /// </summary>
    /// <summary>
    /// Advance to the next player's turn. This is the centralized way to change turns.
    /// Handles both playerToMove index and activePlayer reference atomically.
    /// Network-aware for synchronized gameplay.
    /// </summary>
    public void AdvanceTurn()
    {
        // Guard: check if players exist
        if (players.Count == 0)
        {
            Debug.LogError("[GameManager] AdvanceTurn called but no players are registered!");
            return;
        }

        // Calculate next player index
        int nextPlayerIndex = (playerToMove + 1) % players.Count;
        
        // Update both index and reference atomically
        playerToMove = nextPlayerIndex;
        activePlayer = players[nextPlayerIndex];

        // Network sync: broadcast turn change to all clients
        if (isNetworkEnabled && networkGameManager != null)
        {
            Debug.Log(nextPlayerIndex);
            networkGameManager.UpdateActivePlayerOnServer(nextPlayerIndex);
            
        }
        else
        {
            // local mode
            playerToMove = nextPlayerIndex;
            activePlayer = players[nextPlayerIndex];
        }
        }

    private void EndPlayerTurn()
    {
        // Guard: check if players exist
        if (players.Count == 0)
        {
            Debug.LogError("[GameManager] EndPlayerTurn called but no players are registered!");
            return;
        }

        PlayerStats stats = activePlayer.GetComponent<PlayerStats>();
        if (stats == null)
            return;

        // Use centralized turn advancement
        if (isNetworkEnabled && networkGameManager != null)
        {
            if (networkGameManager.IsServer)
            {
                // Host/server changes turn directly
                AdvanceTurn();
            }
            else
            {
                // Client requests server to change turn
                //networkGameManager.RequestTurnChangeServerRpc();
            }
        }
        else
        {
            // Local (offline) mode
            AdvanceTurn();
        }
    }

    // check tileFunction and if snake/ladder move to endpoint (tile-by-tile)
    IEnumerator HandleTileEffects(GameObject player, int tileID)
    {
        Tile tile = floorManager.FindTileByID(tileID);
        if (tile == null) yield break;

        // Cache component lookups to avoid repeated allocations
        PlayerStats playerStats = player.GetComponent<PlayerStats>();
        PlayerStats activePlayerStats = activePlayer.GetComponent<PlayerStats>();
        PlayerActions activePlayerActions = activePlayer.GetComponent<PlayerActions>();

        switch (tile.tileFunction)
        {
            case 0: // nothing xd
                rollTheDice.interactable = true;
                break;
            case 1: // ladder start
                //Debug.Log($"{player.name} stepped on a ladder at {tileID}");
                for (int i = 0; i < floorManager.ladders.Count; i++)
                {
                    Ladder ladder = floorManager.ladders[i].GetComponent<Ladder>();
                    if (ladder != null && ladder.startTile == tileID)
                    {
                        int endID = ladder.endTile;
                        // small pause before climbing
                        yield return new WaitForSeconds(0.2f);
                        yield return StartCoroutine(MoveAlongSegments(player, ladder.segmentPositions));

                        if (playerStats != null)
                            playerStats.currentPos = endID;
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
                //Debug.Log($"{player.name} stepped on a snake at {tileID}");
                for (int i = 0; i < floorManager.snakes.Count; i++)
                {
                    Snake snake = floorManager.snakes[i].GetComponent<Snake>();
                    if (snake != null && snake.startTile == tileID)
                    {
                        int endID = snake.endTile;
                        // small pause before sliding
                        yield return new WaitForSeconds(0.2f);
                        yield return StartCoroutine(MoveAlongSegments(player, snake.segmentPositions));

                        if (playerStats != null)
                            playerStats.currentPos = endID;
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
                //Debug.Log($"{player.name} stepped on a jam at {tileID}");
                if (activePlayerStats != null)
                {
                    activePlayerStats.jamInUse = 2;
                }
                rollTheDice.interactable = true;
                break;
            case 6: //caramel
                //Debug.Log($"{player.name} stepped on a caramel at {tileID}");
                if (activePlayerStats != null)
                {
                    activePlayerStats.skipNextTurn = true;
                }
                rollTheDice.interactable = true;
                break;
            case 7: //chance
                //Debug.Log($"{player.name} stepped on a chance tile at {tileID}");
                if (activePlayerActions != null)
                {
                    activePlayerActions.PickCard();
                }
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
        {
            player.transform.position = marker.position;
            player.GetComponent<PlayerStats>().currentPos = tileID;
            EndPlayerTurn(); // Network-aware turn change
        }
        else
        {
            //Debug.LogWarning($"[GameManager] Marker not found for player {player.name} on tile {tileID}");
        }
    }
    IEnumerator MoveAlongSegments(GameObject player, List<Transform> segments)
    {
        for (int i = 0; i < segments.Count; i++)
        {
            player.transform.position = segments[i].position;

            // small delay between segment steps
            yield return new WaitForSeconds(0.15f);
        }
    }

    

}

