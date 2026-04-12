using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central manager for multiplayer functionality.
/// Handles mode selection (local/online), player setup, and network initialization.
/// </summary>
public class MultiplayerManager : MonoBehaviour
{
    public static MultiplayerManager Instance { get; private set; }

    public enum GameMode
    {
        SinglePlayer,
        LocalCoOp,
        OnlineMultiplayer
    }

    [SerializeField] private GameMode currentGameMode = GameMode.LocalCoOp;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private int maxPlayers = 4;

    private Dictionary<ulong, GameObject> playerDictionary = new Dictionary<ulong, GameObject>();
    private bool isNetworkActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeGameMode();
    }

    private void InitializeGameMode()
    {
        switch (currentGameMode)
        {
            case GameMode.SinglePlayer:
                Debug.Log("[MultiplayerManager] Initializing Single Player");
                gameManager.SetGameMode(false);
                break;

            case GameMode.LocalCoOp:
                Debug.Log("[MultiplayerManager] Initializing Local Co-Op");
                gameManager.SetGameMode(false);
                break;

            case GameMode.OnlineMultiplayer:
                Debug.Log("[MultiplayerManager] Initializing Online Multiplayer");
                InitializeNetworking();
                break;
        }
    }

    private void InitializeNetworking()
    {
        if (networkManager == null)
        {
            networkManager = FindAnyObjectByType<NetworkManager>();
        }

        if (networkManager == null)
        {
            Debug.LogError("[MultiplayerManager] NetworkManager not found in scene!");
            return;
        }

        // Enable ConnectionApproval before setting callback
        networkManager.NetworkConfig.ConnectionApproval = true;
        Debug.Log("[MultiplayerManager] ConnectionApproval enabled");

        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnServerStarted;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;

        isNetworkActive = true;
    }

    public void StartAsHost()
    {
        currentGameMode = GameMode.OnlineMultiplayer;
        InitializeNetworking();
        if (networkManager.StartHost())
        {
            Debug.Log("[MultiplayerManager] Started as Host");
            gameManager.SetGameMode(true);
        }
        else
        {
            Debug.LogError("[MultiplayerManager] Failed to start as Host");
        }
    }

    public void StartAsClient(string ipAddress = "127.0.0.1")
    {
        currentGameMode = GameMode.OnlineMultiplayer;
        InitializeNetworking();
        if (networkManager.StartClient())
        {
            Debug.Log("[MultiplayerManager] Started as Client");
        }
        else
        {
            Debug.LogError("[MultiplayerManager] Failed to start as Client");
        }
    }

    public void StartLocalGame()
    {
        currentGameMode = GameMode.LocalCoOp;
        gameManager.SetGameMode(false);
        Debug.Log("[MultiplayerManager] Started Local Co-Op Game");
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        bool approve = networkManager.ConnectedClients.Count < maxPlayers;
        response.Approved = approve;
        response.CreatePlayerObject = true;
    }

    private void OnServerStarted()
    {
        Debug.Log("[MultiplayerManager] Server started");
    }

    private void OnClientConnected(ulong clientId)
    {
        //Debug.Log($"[MultiplayerManager] Client {clientId} connected");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        //Debug.Log($"[MultiplayerManager] Client {clientId} disconnected");
        if (playerDictionary.ContainsKey(clientId))
        {
            playerDictionary.Remove(clientId);
        }
    }

    public void RegisterPlayer(ulong clientId, GameObject playerObject)
    {
        if (!playerDictionary.ContainsKey(clientId))
        {
            playerDictionary.Add(clientId, playerObject);
            //Debug.Log($"[MultiplayerManager] Registered networked player {playerObject.name}");
            
            // Also register with GameManager for dynamic player list
            if (gameManager != null)
            {
                gameManager.RegisterPlayer(playerObject);
            }
            else
            {
                Debug.LogWarning("[MultiplayerManager] GameManager not found, cannot register player with game!");
            }
        }
    }

    public GameObject GetPlayer(ulong clientId)
    {
        return playerDictionary.ContainsKey(clientId) ? playerDictionary[clientId] : null;
    }

    public bool IsNetworkActive => isNetworkActive;
    public GameMode CurrentGameMode => currentGameMode;

    private void OnDestroy()
    {
        if (networkManager != null && isNetworkActive)
        {
            networkManager.ConnectionApprovalCallback -= ApprovalCheck;
            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
