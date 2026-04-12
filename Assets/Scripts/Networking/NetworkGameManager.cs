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
    private bool isNetworkEnabled = false;

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
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkPlayerToMove.OnValueChanged -= OnPlayerToMoveChanged;
        networkWheelValue.OnValueChanged -= OnWheelValueChanged;
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
    /// </summary>
    public void UpdateActivePlayerOnServer(int playerIndex)
    {
        if (!IsServer)
        {
            return;
        }

        // Server writes to NetworkVariable (this triggers OnValueChanged on all clients)
        int nextPlayer = (networkPlayerToMove.Value + 1) % gameManager.players.Count;
        networkPlayerToMove.Value = nextPlayer;
        Debug.Log(networkPlayerToMove.Value);
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
        //Debug.Log($"[NetworkGameManager] Wheel value changed: {oldValue} -> {newValue}");
    }

    public bool IsNetworkEnabled => isNetworkEnabled;

    /// <summary>
    /// Request turn change from client
    /// </summary>
    [ServerRpc]
    public void RequestTurnChangeServerRpc()
    {
        int nextPlayer = (networkPlayerToMove.Value + 1) % gameManager.players.Count;
        networkPlayerToMove.Value = nextPlayer;
        // OnPlayerToMoveChanged callback will notify all clients
    }
}
