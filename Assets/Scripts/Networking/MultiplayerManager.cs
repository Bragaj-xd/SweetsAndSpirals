using Unity.Netcode;
using UnityEngine;
using System.Collections;
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
    private bool networkingInitialized = false;
    private List<GameObject> pendingPlayerRegistrations = new List<GameObject>(); // Players waiting for GameManager
    
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
        // Skip if already initialized to prevent duplicate callback registration
        if (networkingInitialized)
        {
            Debug.Log("[MultiplayerManager] Networking already initialized, skipping re-initialization");
            return;
        }

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
        
        // Unregister any existing callbacks first (in case of re-initialization)
        networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        networkManager.OnServerStarted -= OnServerStarted;
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        
        Debug.Log("[MultiplayerManager] ConnectionApproval enabled");

        // Register callbacks only once
        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnServerStarted;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;

        networkingInitialized = true;
        isNetworkActive = true;
        Debug.Log("[MultiplayerManager] Networking initialized");
    }

    public void StartAsHost()
    {
        StartCoroutine(StartAsHostAsync());
    }
    
    private System.Collections.IEnumerator StartAsHostAsync()
    {
        // Shut down any existing network instance first
        bool wasShutdown = ShutdownExistingNetwork();
        
        // Only wait if we actually shut down something
        if (wasShutdown)
        {
            // Wait for async shutdown to complete
            yield return new WaitForSeconds(2.0f);
            
            // Extra check to ensure NetworkManager is fully shutdown
            int checkCount = 0;
            int maxChecks = 20;
            while (networkManager != null && (networkManager.IsListening || networkManager.IsServer || networkManager.IsClient) && checkCount < maxChecks)
            {
                Debug.Log($"[MultiplayerManager] NetworkManager still active, waiting... (attempt {checkCount + 1}/{maxChecks})");
                yield return new WaitForSeconds(0.2f);
                checkCount++;
            }
            
            // If we hit max attempts, wait one more time to be sure
            if (checkCount >= maxChecks)
            {
                Debug.LogWarning($"[MultiplayerManager] Max shutdown attempts reached, waiting additional time...");
                yield return new WaitForSeconds(1.0f);
            }
        }
        else
        {
            Debug.Log("[MultiplayerManager] No existing network to shutdown, proceeding immediately");
        }
        
        currentGameMode = GameMode.OnlineMultiplayer;
        
        // Initialize Relay services for internet play
        yield return StartCoroutine(InitializeRelayForHost());
    }
    
    private System.Collections.IEnumerator InitializeRelayForHost()
    {
        // Check if NetworkManager is already running
        if (networkManager != null && (networkManager.IsListening || networkManager.IsServer || networkManager.IsClient))
        {
            Debug.Log("[MultiplayerManager] NetworkManager is already running, using existing instance");
            SetGameModeIfAvailable(true);
            yield break;
        }
        
        // First, shut down any zombie network instance
        bool wasShutdown = ShutdownExistingNetwork();
        
        // Only wait if we actually shut down something
        if (wasShutdown)
        {
            // Wait longer for async shutdown to complete
            yield return new WaitForSeconds(3.0f);
            
            // Extra check to ensure NetworkManager is fully shutdown
            int checkCount = 0;
            int maxChecks = 30;
            while (networkManager != null && (networkManager.IsListening || networkManager.IsServer || networkManager.IsClient) && checkCount < maxChecks)
            {
                Debug.Log($"[MultiplayerManager] NetworkManager still active, waiting... (attempt {checkCount + 1}/{maxChecks})");
                yield return new WaitForSeconds(0.2f);
                checkCount++;
            }
            
            if (checkCount >= maxChecks)
            {
                Debug.LogWarning($"[MultiplayerManager] Max shutdown attempts reached, giving up on clean shutdown");
                // Don't try to start if we can't properly shutdown
                Debug.LogError("[MultiplayerManager] Could not properly shutdown existing NetworkManager, aborting Host start");
                yield break;
            }
        }
        
        // Use existing singleton or create new one
        RelayManager relayManager = RelayManager.Instance;
        if (relayManager == null)
        {
            GameObject relayObj = new GameObject("RelayManager");
            relayManager = relayObj.AddComponent<RelayManager>();
            // Awake will initialize singleton
            yield return null;
            relayManager = RelayManager.Instance;
        }
        
        // Initialize Unity Services
        yield return relayManager.StartCoroutine(relayManager.InitializeServices());
        
        // Allocate relay session
        bool allocationComplete = false;
        yield return relayManager.StartCoroutine(relayManager.AllocateRelay(code => 
        {
            currentJoinCode = code;
            allocationComplete = true;
        }));
        
        if (!allocationComplete || string.IsNullOrEmpty(currentJoinCode))
        {
            Debug.LogError("[MultiplayerManager] Failed to allocate Relay session");
            yield break;
        }
        
        // Now initialize networking with Relay configured
        InitializeNetworking();
        
        // Verify NetworkManager is ready
        if (networkManager == null)
        {
            Debug.LogError("[MultiplayerManager] NetworkManager is null when trying to start Host!");
            yield break;
        }
        
        if (networkManager.StartHost())
        {
            Debug.Log($"[MultiplayerManager] Started as Host with Relay join code: {currentJoinCode}");
            SetGameModeIfAvailable(true);
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
        StartCoroutine(JoinWithCodeAsync(joinCode));
    }

    private IEnumerator JoinWithCodeAsync(string joinCode)
    {
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("[MultiplayerManager] Join code cannot be empty!");
            yield break;
        }
        
        // Shut down any existing network instance first
        bool wasShutdown = ShutdownExistingNetwork();
        
        // Only wait if we actually shut down something
        if (wasShutdown)
        {
            // Wait for async shutdown to complete
            yield return new WaitForSeconds(2.0f);
            
            // Extra check to ensure NetworkManager is fully shutdown
            int checkCount = 0;
            int maxChecks = 20;
            while (networkManager != null && (networkManager.IsListening || networkManager.IsServer || networkManager.IsClient) && checkCount < maxChecks)
            {
                Debug.Log($"[MultiplayerManager] NetworkManager still active, waiting... (attempt {checkCount + 1}/{maxChecks})");
                yield return new WaitForSeconds(0.2f);
                checkCount++;
            }
            
            // If we hit max attempts, wait one more time to be sure
            if (checkCount >= maxChecks)
            {
                Debug.LogWarning($"[MultiplayerManager] Max shutdown attempts reached, waiting additional time...");
                yield return new WaitForSeconds(1.0f);
            }
        }
        else
        {
            Debug.Log("[MultiplayerManager] No existing network to shutdown, proceeding immediately");
        }
        
        currentGameMode = GameMode.OnlineMultiplayer;
        
        // Initialize Relay services for internet play
        yield return StartCoroutine(InitializeRelayForClient(joinCode));
    }
    
    private System.Collections.IEnumerator InitializeRelayForClient(string joinCode)
    {
        // Check if NetworkManager is already running
        if (networkManager != null && (networkManager.IsListening || networkManager.IsServer || networkManager.IsClient))
        {
            Debug.Log("[MultiplayerManager] NetworkManager is already running, using existing instance");
            SetGameModeIfAvailable(true);
            yield break;
        }
        
        // First, shut down any zombie network instance
        bool wasShutdown = ShutdownExistingNetwork();
        
        // Only wait if we actually shut down something
        if (wasShutdown)
        {
            // Wait longer for async shutdown to complete
            yield return new WaitForSeconds(3.0f);
            
            // Extra check to ensure NetworkManager is fully shutdown
            int checkCount = 0;
            int maxChecks = 30;
            while (networkManager != null && (networkManager.IsListening || networkManager.IsServer || networkManager.IsClient) && checkCount < maxChecks)
            {
                Debug.Log($"[MultiplayerManager] NetworkManager still active, waiting... (attempt {checkCount + 1}/{maxChecks})");
                yield return new WaitForSeconds(0.2f);
                checkCount++;
            }
            
            if (checkCount >= maxChecks)
            {
                Debug.LogWarning($"[MultiplayerManager] Max shutdown attempts reached, giving up on clean shutdown");
                // Don't try to start if we can't properly shutdown
                Debug.LogError("[MultiplayerManager] Could not properly shutdown existing NetworkManager, aborting Client start");
                yield break;
            }
        }
        
        // Use existing singleton or create new one
        RelayManager relayManager = RelayManager.Instance;
        if (relayManager == null)
        {
            GameObject relayObj = new GameObject("RelayManager");
            relayManager = relayObj.AddComponent<RelayManager>();
            // Awake will initialize singleton
            yield return null;
            relayManager = RelayManager.Instance;
        }
        
        // Initialize Unity Services
        yield return relayManager.StartCoroutine(relayManager.InitializeServices());
        
        // Join relay session with code
        bool joinComplete = false;
        yield return relayManager.StartCoroutine(relayManager.JoinRelay(joinCode, () => 
        {
            joinComplete = true;
        }));
        
        if (!joinComplete)
        {
            Debug.LogError($"[MultiplayerManager] Failed to join Relay session with code: {joinCode}");
            yield break;
        }
        
        // Now initialize networking with Relay configured
        InitializeNetworking();
        
        // Verify NetworkManager is ready
        if (networkManager == null)
        {
            Debug.LogError("[MultiplayerManager] NetworkManager is null when trying to join as Client!");
            yield break;
        }
        
        if (networkManager.StartClient())
        {
            Debug.Log("[MultiplayerManager] Started as Client via Relay with join code: " + joinCode);
            SetGameModeIfAvailable(true);
        }
        else
        {
            Debug.LogError($"[MultiplayerManager] Failed to start as Client. NetworkManager state - IsListening: {networkManager.IsListening}, IsServer: {networkManager.IsServer}, IsClient: {networkManager.IsClient}");
        }
    }
    
    /// <summary>
    /// Shutdown any existing network instance before starting a new one.
    /// Returns true if a shutdown was performed, false if nothing was running.
    /// </summary>
    private bool ShutdownExistingNetwork()
    {
        if (networkManager != null)
        {
            if (networkManager.IsListening)
            {
                Debug.Log("[MultiplayerManager] Shutting down existing network instance");
                networkManager.Shutdown();
                networkingInitialized = false; // Allow re-initialization on next session
                return true;
            }
            else if (networkManager.IsServer || networkManager.IsClient)
            {
                Debug.Log("[MultiplayerManager] NetworkManager not listening but is server/client, forcing shutdown");
                networkManager.Shutdown();
                networkingInitialized = false;
                return true;
            }
        }
        return false; // Nothing was running
    }

    public void StartLocalGame()
    {
        // Shut down any existing network instance
        ShutdownExistingNetwork();
        
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
            if (gameManager != null)
            {
                Debug.Log("[MultiplayerManager] GameManager found after null check");
            }
        }
        
        // Set game mode if found
        if (gameManager != null)
        {
            gameManager.SetGameMode(isNetworked);
            // Register any players that were spawned while waiting for GameManager
            RegisterPendingPlayers();
            Debug.Log($"[MultiplayerManager] SetGameModeIfAvailable complete - Pending players registered");
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
            Debug.Log($"[MultiplayerManager] Registered networked player {playerObject.name} (ClientId: {clientId})");
            
            // Try to find and register with GameManager if available
            if (gameManager == null)
            {
                gameManager = FindAnyObjectByType<GameManager>();
                if (gameManager != null)
                {
                    Debug.Log("[MultiplayerManager] GameManager found during player registration");
                }
            }
            
            if (gameManager != null)
            {
                gameManager.RegisterPlayer(playerObject);
                Debug.Log($"[MultiplayerManager] {playerObject.name} registered with GameManager immediately");
            }
            else
            {
                // GameManager not available yet (still in menu scene), queue for later
                if (!pendingPlayerRegistrations.Contains(playerObject))
                {
                    pendingPlayerRegistrations.Add(playerObject);
                    Debug.Log($"[MultiplayerManager] Player {playerObject.name} queued for registration (GameManager not found yet, pending count: {pendingPlayerRegistrations.Count})");
                }
            }
        }
    }
    
    /// <summary>
    /// Register all pending players once GameManager becomes available
    /// </summary>
    private void RegisterPendingPlayers()
    {
        if (gameManager == null || pendingPlayerRegistrations.Count == 0)
        {
            if (gameManager == null)
                Debug.LogWarning("[MultiplayerManager] Cannot register pending players - GameManager is null!");
            return;
        }
        
        Debug.Log($"[MultiplayerManager] Registering {pendingPlayerRegistrations.Count} pending players with GameManager");
        foreach (GameObject playerObject in pendingPlayerRegistrations)
        {
            if (playerObject != null)
            {
                gameManager.RegisterPlayer(playerObject);
                Debug.Log($"[MultiplayerManager] Registered pending player: {playerObject.name}");
            }
        }
        pendingPlayerRegistrations.Clear();
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
