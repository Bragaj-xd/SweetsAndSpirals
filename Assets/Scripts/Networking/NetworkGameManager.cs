using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// NetworkGameManager handles game state synchronization for networked games.
/// Works alongside GameManager to provide network functionality.
/// </summary>
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    private GameManager gameManager;
    private NetworkVariable<int> networkPlayerToMove = new NetworkVariable<int>(0);
    private NetworkVariable<int> networkWheelValue = new NetworkVariable<int>(0);
    private NetworkVariable<bool> networkRolledThree = new NetworkVariable<bool>(false);
    private bool isNetworkEnabled = false;
    
    // Track player effects - stores jam duration and skip state per player
    private Dictionary<int, int> playerJamInUse = new Dictionary<int, int>();
    private Dictionary<int, bool> playerSkipNextTurn = new Dictionary<int, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        gameManager = GetComponent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[NetworkGameManager] GameManager not found!");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isNetworkEnabled = true;
        //Debug.Log("[NetworkGameManager] Network spawned");

        // Subscribe to network variable changes
        networkPlayerToMove.OnValueChanged += OnPlayerToMoveChanged;
        networkWheelValue.OnValueChanged += OnWheelValueChanged;
        networkRolledThree.OnValueChanged += OnRolledThreeChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkPlayerToMove.OnValueChanged -= OnPlayerToMoveChanged;
        networkWheelValue.OnValueChanged -= OnWheelValueChanged;
        networkRolledThree.OnValueChanged -= OnRolledThreeChanged;
    }

    /*
    /// <summary>
    /// RPC: Update active player on all clients
    /// Note: Server should set networkPlayerToMove directly; this ClientRpc just notifies
    /// </summary>
    [ClientRpc]
    public void UpdateActivePlayerClientRpc(int playerIndex)
    {
        // Clients update GameManager directly (without touching NetworkVariable)
        if (gameManager != null)
        {
            gameManager.playerToMove = playerIndex;
        }
    }
    */
    /// <summary>
    /// Server method: Update active player and sync to all clients
    /// <summary>
    /// Server method: Update active player and sync to all clients
    /// </summary>
    public void UpdateActivePlayerOnServer(int currentPlayerIndex)
    {
        if (!IsServer)
        {
            return;
        }

        if (gameManager == null || gameManager.players.Count == 0)
        {
            Debug.LogError("[NetworkGameManager] Cannot update active player - GameManager or players list is invalid!");
            return;
        }

        // Calculate next player from current player index
        int nextPlayer = (currentPlayerIndex + 1) % gameManager.players.Count;
        
        // Server writes to NetworkVariable (this triggers OnValueChanged on all clients)
        networkPlayerToMove.Value = nextPlayer;
        Debug.Log($"[NetworkGameManager] Turn advanced: {currentPlayerIndex} -> {nextPlayer}");
    }
    /// <summary>
    /// RPC: Move player tile by tile (networked version)
    /// Called on all clients - executes movement without re-broadcasting to prevent infinite loops.
    /// </summary>
    [ClientRpc]
    public void MovePlayerTileByTileClientRpc(int playerIndex, int destinationTileID)
    {
        if (gameManager != null)
        {
            // Call ExecuteNetworkMovePlayerTileByTile to avoid re-broadcasting the RPC
            StartCoroutine(gameManager.ExecuteNetworkMovePlayerTileByTile(playerIndex, destinationTileID));
        }
        else
        {
            Debug.LogError("[NetworkGameManager] GameManager is null in MovePlayerTileByTileClientRpc!");
        }
    }

    /// <summary>
    /// RPC: Move multiple players for card effects (e.g., Switch Places)
    /// Sets ignoreTileEffects on specified players and moves them
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void MovePlayersForCardEffectServerRpc(int[] playerIndices, int[] destinations, int[] ignoreTileEffectsIndices)
    {
        // Server broadcasts to all clients
        MovePlayersForCardEffectClientRpc(playerIndices, destinations, ignoreTileEffectsIndices);
    }

    [ClientRpc]
    public void MovePlayersForCardEffectClientRpc(int[] playerIndices, int[] destinations, int[] ignoreTileEffectsIndices)
    {
        if (gameManager == null)
        {
            Debug.LogError("[NetworkGameManager] GameManager is null in MovePlayersForCardEffectClientRpc!");
            return;
        }

        // Set ignoreTileEffects flags for the specified players
        if (ignoreTileEffectsIndices != null)
        {
            foreach (int idx in ignoreTileEffectsIndices)
            {
                if (idx >= 0 && idx < gameManager.players.Count)
                {
                    PlayerStats stats = gameManager.players[idx].GetComponent<PlayerStats>();
                    if (stats != null)
                        stats.ignoreTileEffects = true;
                }
            }
        }

        // Move all specified players
        if (playerIndices != null && destinations != null)
        {
            for (int i = 0; i < playerIndices.Length && i < destinations.Length; i++)
            {
                int playerIdx = playerIndices[i];
                int dest = destinations[i];
                
                if (playerIdx >= 0 && playerIdx < gameManager.players.Count)
                {
                    StartCoroutine(gameManager.ExecuteNetworkMovePlayerTileByTile(playerIdx, dest));
                }
            }
        }
    }

    /// <summary>
    /// RPC: Move a single player tile by tile (any client can call)
    /// Server broadcasts to all clients for synchronized movement
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void MovePlayerTileByTileServerRpc(int playerIndex, int destinationTileID)
    {
        // Server broadcasts to all clients
        MovePlayerTileByTileClientRpc(playerIndex, destinationTileID);
    }

    /// <summary>
    /// RPC: Spin wheel - synchronized across all clients
    /// </summary>
    [ClientRpc]
    public void SpinWheelClientRpc(int wheelResult)
    {
        // Clients just receive the wheel result, no need to write to NetworkVariable
        //Debug.Log($"[NetworkGameManager] Wheel spun: {wheelResult}");
    }

    /// <summary>
    /// Server method: Spin wheel and sync to all clients
    /// </summary>
    public void SpinWheelOnServer(int wheelResult)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[NetworkGameManager] SpinWheelOnServer called from non-server!");
            return;
        }

        // Server writes to NetworkVariable (syncs to all clients)
        networkWheelValue.Value = wheelResult;
    }

    /// <summary>
    /// RPC: Add chance card to player
    /// </summary>
    [ClientRpc]
    public void AddChanceCardClientRpc(int playerIndex, int cardId)
    {
        if (gameManager != null && playerIndex < gameManager.players.Count)
        {
            GameObject player = gameManager.players[playerIndex];
            gameManager.AddChanceCard(player);
            //Debug.Log($"[NetworkGameManager] Card {cardId} added to player {playerIndex}");
        }
    }

    private void OnPlayerToMoveChanged(int oldValue, int newValue)
    {
        if (gameManager != null && newValue < gameManager.players.Count)
        {
            gameManager.playerToMove = newValue;
            gameManager.activePlayer = gameManager.players[newValue];
        }
    }

    private void OnWheelValueChanged(int oldValue, int newValue)
    {
        if (gameManager != null && gameManager.diceRoll != null)
        {
            // Sync the network wheel value to the local DiceRoll component
            gameManager.diceRoll.wheelValue = newValue;
            Debug.Log($"[NetworkGameManager] Wheel value synced: {oldValue} -> {newValue}");
        }
    }

    private void OnRolledThreeChanged(bool oldValue, bool newValue)
    {
        if (gameManager != null)
        {
            gameManager.rolledThree = newValue;
        }
    }

    /// <summary>
    /// Server method: Sets rolled three state and syncs to all clients
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetRolledThreeOnServerRpc(bool value)
    {
        networkRolledThree.Value = value;
    }

    /// <summary>
    /// Server method: Apply jam effect to a player and sync to all clients
    /// </summary>
    public void ApplyJamEffectOnServer(int playerIndex)
    {
        if (!IsServer)
        {
            return;
        }
        
        ApplyJamEffectClientRpc(playerIndex);
    }
    
    [ClientRpc]
    private void ApplyJamEffectClientRpc(int playerIndex)
    {
        if (gameManager != null && playerIndex < gameManager.players.Count)
        {
            GameObject player = gameManager.players[playerIndex];
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.jamInUse = 2;
                Debug.Log($"[NetworkGameManager] Jam effect applied to player {playerIndex}");
            }
        }
    }
    
    /// <summary>
    /// Server method: Apply skip next turn effect and sync to all clients
    /// </summary>
    public void ApplySkipNextTurnOnServer(int playerIndex)
    {
        if (!IsServer)
        {
            return;
        }
        
        ApplySkipNextTurnClientRpc(playerIndex);
    }
    
    [ClientRpc]
    private void ApplySkipNextTurnClientRpc(int playerIndex)
    {
        if (gameManager != null && playerIndex < gameManager.players.Count)
        {
            GameObject player = gameManager.players[playerIndex];
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.skipNextTurn = true;
                Debug.Log($"[NetworkGameManager] Skip effect applied to player {playerIndex}");
            }
        }
    }
    
    /// <summary>
    /// Server method: Update jam counter and sync to all clients
    /// </summary>
    public void UpdateJamCounterOnServer(int playerIndex, int newValue)
    {
        if (!IsServer)
        {
            return;
        }
        
        UpdateJamCounterClientRpc(playerIndex, newValue);
    }
    
    [ClientRpc]
    private void UpdateJamCounterClientRpc(int playerIndex, int newValue)
    {
        if (gameManager != null && playerIndex < gameManager.players.Count)
        {
            GameObject player = gameManager.players[playerIndex];
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.jamInUse = newValue;
            }
        }
    }
    
    /// <summary>
    /// Server method: Reset skip next turn effect and sync to all clients
    /// </summary>
    public void ResetSkipNextTurnOnServer(int playerIndex)
    {
        if (!IsServer)
        {
            return;
        }
        
        ResetSkipNextTurnClientRpc(playerIndex);
    }
    
    [ClientRpc]
    private void ResetSkipNextTurnClientRpc(int playerIndex)
    {
        if (gameManager != null && playerIndex < gameManager.players.Count)
        {
            GameObject player = gameManager.players[playerIndex];
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.skipNextTurn = false;
            }
        }
    }

    public bool IsNetworkEnabled => isNetworkEnabled;

    /// <summary>
    /// Sync SaL (Ladder/Snake/Jam/Caramel) placement across all clients
    /// salType: 0=Ladder, 1=Snake, 2=Jam, 3=Caramel
    /// salLength: 2, 3, or 4 (for ladder/snake); ignored for jam/caramel
    /// placementPos: world position where the SaL was placed
    /// placementRot: rotation of the SaL (important for snakes/ladders)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void PlaceSaLOnServerRpc(int startTileID, int endTileID, int salType, int salLength, Vector3 placementPos, Quaternion placementRot)
    {
        PlaceSaLClientRpc(startTileID, endTileID, salType, salLength, placementPos, placementRot);
    }
    
    [ClientRpc]
    private void PlaceSaLClientRpc(int startTileID, int endTileID, int salType, int salLength, Vector3 placementPos, Quaternion placementRot)
    {
        if (gameManager == null || gameManager.floorManager == null)
        {
            Debug.LogError("[NetworkGameManager] GameManager or FloorManager is null in PlaceSaLClientRpc!");
            return;
        }
        
        // Update tile functions
        switch(salType)
        {
            case 0: // Ladder
                gameManager.floorManager.FindTileByID(startTileID).tileFunction = 1;
                gameManager.floorManager.FindTileByID(endTileID).tileFunction = 2;
                break;
            case 1: // Snake
                gameManager.floorManager.FindTileByID(startTileID).tileFunction = 3;
                gameManager.floorManager.FindTileByID(endTileID).tileFunction = 4;
                break;
            case 2: // Jam
                gameManager.floorManager.FindTileByID(startTileID).tileFunction = 5;
                break;
            case 3: // Caramel
                gameManager.floorManager.FindTileByID(startTileID).tileFunction = 6;
                break;
        }
        
        // Instantiate the SaL GameObject on this client
        // Get the active player's PlayerActions to access prefabs
        if (gameManager.activePlayer == null)
            return;
            
        PlayerActions playerActions = gameManager.activePlayer.GetComponent<PlayerActions>();
        if (playerActions == null)
            return;
        
        GameObject salPrefab = null;
        
        switch (salType)
        {
            case 0: // Ladder
                salPrefab = salLength switch
                {
                    2 => playerActions.ladder2Prefab,
                    3 => playerActions.ladder3Prefab,
                    4 => playerActions.ladder4Prefab,
                    _ => playerActions.ladder2Prefab
                };
                break;
            case 1: // Snake
                salPrefab = salLength switch
                {
                    2 => playerActions.snake2Prefab,
                    3 => playerActions.snake3Prefab,
                    4 => playerActions.snake4Prefab,
                    _ => playerActions.snake2Prefab
                };
                break;
            case 2: // Jam
                salPrefab = playerActions.jamPrefab;
                break;
            case 3: // Caramel
                salPrefab = playerActions.caramelPrefab;
                break;
        }
        
        if (salPrefab != null)
        {
            GameObject salRoot = Instantiate(salPrefab, placementPos, placementRot);
            salRoot.transform.SetParent(gameManager.floorManager.transform);
            
            // Add to FloorManager's lists
            switch (salType)
            {
                case 0: // Ladder
                    salRoot.name = "Ladder";
                    gameManager.floorManager.ladders.Add(salRoot);
                    break;
                case 1: // Snake
                    salRoot.name = "Snake";
                    gameManager.floorManager.snakes.Add(salRoot);
                    break;
                case 2: // Jam
                    salRoot.name = "Jam";
                    gameManager.floorManager.jams.Add(salRoot);
                    break;
                case 3: // Caramel
                    salRoot.name = "Caramel";
                    gameManager.floorManager.caramels.Add(salRoot);
                    break;
            }
        }
    }

    /// <summary>
    /// Request turn change from client
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestTurnChangeServerRpc()
    {
        int nextPlayer = (networkPlayerToMove.Value + 1) % gameManager.players.Count;
        networkPlayerToMove.Value = nextPlayer;
        // OnPlayerToMoveChanged callback will notify all clients
    }
}
