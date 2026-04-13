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
    
    // Join code system
    private string currentJoinCode = "";
    private static Dictionary<string, string> activeGameCodes = new Dictionary<string, string>(); // code -> host IP
    public string CurrentJoinCode => currentJoinCode;

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
        // Don't initialize game mode in menu scene - wait for game scene to load
        // Only find GameManager if it exists, don't require it
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
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
            GenerateAndSetJoinCode();
            Debug.Log($"[MultiplayerManager] Started as Host with join code: {currentJoinCode}");
            SetGameModeIfAvailable(true);
        }
        else
        {
            Debug.LogError("[MultiplayerManager] Failed to start as Host");
        }
    }
    
    private void GenerateAndSetJoinCode()
    {
        // Generate a random 6-character alphanumeric code
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        currentJoinCode = "";
        for (int i = 0; i < 6; i++)
        {
            currentJoinCode += chars[Random.Range(0, chars.Length)];
        }
        Debug.Log($"[MultiplayerManager] Generated join code: {currentJoinCode}");
    }
    
    public void JoinWithCode(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("[MultiplayerManager] Join code cannot be empty!");
            return;
        }
        
        currentGameMode = GameMode.OnlineMultiplayer;
        InitializeNetworking();
        
        Debug.Log($"[MultiplayerManager] Attempting to join with code: {joinCode}");
        
        // For now, assume joining localhost (same machine) - in production, use lobby matching service
        if (networkManager.StartClient())
        {
            Debug.Log("[MultiplayerManager] Started as Client with join code: " + joinCode);
            SetGameModeIfAvailable(true);
        }
        else
        {
            Debug.LogError("[MultiplayerManager] Failed to start as Client");
        }
    }

    public void StartLocalGame()
    {
        currentGameMode = GameMode.LocalCoOp;
        SetGameModeIfAvailable(false);
        Debug.Log("[MultiplayerManager] Started Local Co-Op Game");
    }
    
    /// <summary>
    /// Safely set game mode on GameManager if it's available.
    /// If GameManager doesn't exist yet (menu scene), it will be found when game scene loads.
    /// </summary>
    private void SetGameModeIfAvailable(bool isNetworked)
    {
        // Try to find GameManager if not already set
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }
        
        // Set game mode if found
        if (gameManager != null)
        {
            gameManager.SetGameMode(isNetworked);
        }
        else
        {
            Debug.LogWarning("[MultiplayerManager] GameManager not found yet (okay if still in menu scene)");
        }
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
