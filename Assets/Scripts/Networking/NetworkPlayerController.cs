using Unity.Netcode;
using UnityEngine;
using System.Collections;

/// <summary>
/// NetworkPlayerController manages a single player in networked games.
/// Handles input ownership and networked RPCs for player actions.
/// </summary>
public class NetworkPlayerController : NetworkBehaviour
{
    private PlayerActions playerActions;
    private PlayerStats playerStats;
    private NetworkObject networkObject;
    private ulong createdByClientId;

    private void Awake()
    {
        playerActions = GetComponent<PlayerActions>();
        playerStats = GetComponent<PlayerStats>();
        networkObject = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Register this player with the game when spawned
        if (MultiplayerManager.Instance != null)
        {
            MultiplayerManager.Instance.RegisterPlayer(OwnerClientId, gameObject);
            //Debug.Log($"[NetworkPlayerController] Player {gameObject.name} registered with MultiplayerManager");
        }
        else
        {
            Debug.LogWarning("[NetworkPlayerController] MultiplayerManager not found!");
        }
    }

    private void Start()
    {
        createdByClientId = networkObject.OwnerClientId;
        
        // Only enable input for the owner
        if (playerActions != null)
        {
            playerActions.enabled = IsOwner;
        }

        //Debug.Log($"[NetworkPlayerController] Player {gameObject.name} - IsOwner: {IsOwner}, ClientId: {OwnerClientId}");
    }

    /// <summary>
    /// RPC: Handle dice roll - called on server, executed on all clients
    /// </summary>
    [ServerRpc]
    public void RollDiceServerRpc(Vector2 mousePos)
    {
        // Validate that the caller is the owner of the active player
        GameManager gameManager = playerActions.gameManager;
        if (gameManager != null && gameManager.activePlayer != null)
        {
            NetworkPlayerController activeNPC = gameManager.activePlayer.GetComponent<NetworkPlayerController>();
            if (activeNPC == null || activeNPC.OwnerClientId != this.OwnerClientId)
            {
                Debug.LogWarning($"[NetworkPlayerController] RollDiceServerRpc rejected: Caller is not the active player");
                return;
            }
        }

        // Server generates the wheel value and syncs to all clients
        int wheelResult = UnityEngine.Random.Range(1, 7);
        
        if (NetworkManager.Singleton != null && NetworkGameManager.Instance != null)
        {
            // Sync wheel value through NetworkGameManager
            NetworkGameManager.Instance.SpinWheelOnServer(wheelResult);
        }
        
        // Execute dice roll on all clients with the server-generated value
        ExecuteDiceRollClientRpc(wheelResult, mousePos);
    }

    [ClientRpc]
    private void ExecuteDiceRollClientRpc(int wheelResult, Vector2 mousePos)
    {
        if (playerActions != null)
        {
            // Get the DiceRoll component and set the wheel value
            DiceRoll diceRoll = playerActions.diceRoll;
            if (diceRoll != null)
            {
                diceRoll.SetWheelValue(wheelResult);
                //Debug.Log($"[NetworkPlayerController] Dice rolled: {wheelResult}");
            }
            else
            {
                //Debug.LogWarning($"[NetworkPlayerController] DiceRoll component not found on {gameObject.name}");
            }
        }
    }

    /// <summary>
    /// RPC: Handle card placement
    /// </summary>
    [ServerRpc]
    public void PlaceCardServerRpc(int cardId, int startTileID, int endTileID)
    {
        ExecuteCardPlacementClientRpc(cardId, startTileID, endTileID);
    }

    [ClientRpc]
    private void ExecuteCardPlacementClientRpc(int cardId, int startTileID, int endTileID)
    {
        if (playerStats != null && playerActions != null)
        {
            FloorManager floorManager = playerActions.floorManager;
            if (floorManager != null)
            {
                // Set tile functions based on card type
                switch(cardId)
                {
                    case 0: // Ladder
                        floorManager.FindTileByID(startTileID).tileFunction = 1;
                        floorManager.FindTileByID(endTileID).tileFunction = 2;
                        break;
                    case 1: // Snake
                        floorManager.FindTileByID(startTileID).tileFunction = 3;
                        floorManager.FindTileByID(endTileID).tileFunction = 4;
                        break;
                    case 2: // Jam
                        floorManager.FindTileByID(startTileID).tileFunction = 5;
                        break;
                    case 3: // Caramel
                        floorManager.FindTileByID(startTileID).tileFunction = 6;
                        break;
                }
                //Debug.Log($"[NetworkPlayerController] Card {cardId} placed on tiles {startTileID} to {endTileID}");
            }
            else
            {
                //Debug.LogWarning($"[NetworkPlayerController] FloorManager not found for card placement");
            }
        }
    }

    /// <summary>
    /// RPC: Handle player movement
    /// </summary>
    [ServerRpc]
    public void MovePlayerServerRpc(int destinationTileID)
    {
        ExecuteMovePlayerClientRpc(destinationTileID);
    }

    [ClientRpc]
    private void ExecuteMovePlayerClientRpc(int destinationTileID)
    {
        if (playerActions != null)
        {
            GameManager gameManager = playerActions.gameManager;
            if (gameManager != null)
            {
                StartCoroutine(gameManager.MovePlayerTileByTile(gameObject, destinationTileID));
                //Debug.Log($"[NetworkPlayerController] {gameObject.name} moving to tile {destinationTileID}");
            }
            else
            {
                //Debug.LogWarning($"[NetworkPlayerController] GameManager not found for player movement");
            }
        }
    }

    /// <summary>
    /// RPC: Notify all players of turn change
    /// </summary>
    [ClientRpc]
    public void NotifyTurnChangeClientRpc(int newActivePlayerIndex)
    {
        if (playerActions != null)
        {
            GameManager gameManager = playerActions.gameManager;
            if (gameManager != null && gameManager.players != null && newActivePlayerIndex < gameManager.players.Count)
            {
                gameManager.activePlayer = gameManager.players[newActivePlayerIndex];
                //Debug.Log($"[NetworkPlayerController] Turn changed to {gameManager.activePlayer.name} (index {newActivePlayerIndex})");
            }
            else
            {
                //Debug.LogWarning($"[NetworkPlayerController] GameManager or players list not properly initialized for turn change");
            }
        }
    }

    /// <summary>
    /// Sync player state to all clients
    /// </summary>
    [ClientRpc]
    public void SyncPlayerStateClientRpc(int currentPos, int jamInUse, bool moveBackwards, bool skipNextTurn)
    {
        if (playerStats != null)
        {
            playerStats.currentPos = currentPos;
            playerStats.jamInUse = jamInUse;
            playerStats.moveBackwards = moveBackwards;
            playerStats.skipNextTurn = skipNextTurn;
            //Debug.Log($"[NetworkPlayerController] {gameObject.name} state synced - Pos: {currentPos}, Jam: {jamInUse}, MoveBack: {moveBackwards}, SkipTurn: {skipNextTurn}");
        }
        else
        {
            //Debug.LogWarning($"[NetworkPlayerController] PlayerStats not found on {gameObject.name} for state sync");
        }
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();
        if (playerActions != null)
        {
            playerActions.enabled = true;
        }
        //Debug.Log($"[NetworkPlayerController] {gameObject.name} gained ownership");
    }

    public override void OnLostOwnership()
    {
        base.OnLostOwnership();
        if (playerActions != null)
        {
            playerActions.enabled = false;
        }
        //Debug.Log($"[NetworkPlayerController] {gameObject.name} lost ownership");
    }
}
