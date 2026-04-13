using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Manages Relay setup for internet multiplayer (Unity 6.3+ compatible).
/// Note: The unified Multiplayer Services package has a significantly different API
/// than the legacy Relay package. This implementation aims for compatibility with
/// what's available in the current version.
/// </summary>
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance { get; private set; }

    private NetworkManager networkManager;
    private bool isInitialized = false;
    private string currentJoinCode = "";
    
    // Error tracking for async operations
    private Exception lastException = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[RelayManager] Duplicate instance detected, destroying this one");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[RelayManager] Singleton instance established");
    }

    /// <summary>
    /// Initialize Unity Services and authentication
    /// </summary>
    public IEnumerator InitializeServices()
    {
        if (isInitialized)
        {
            Debug.Log("[RelayManager] Services already initialized");
            yield break;
        }

        Debug.Log("[RelayManager] Initializing services...");
        
        lastException = null;
        
        // Initialize Unity Services (safe to call multiple times)
        var initTask = UnityServices.InitializeAsync();
        yield return new WaitUntil(() => initTask.IsCompleted);

        if (initTask.IsFaulted)
        {
            lastException = initTask.Exception;
            Debug.LogError($"[RelayManager] Failed to initialize: {initTask.Exception}");
            yield break;
        }

        // Sign in anonymously - handle "already signing in" state
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            var signInTask = AuthenticationService.Instance.SignInAnonymouslyAsync();
            
            // Wait for the task to complete
            yield return new WaitUntil(() => signInTask.IsCompleted);

            // Check for errors after task is complete (outside try-catch)
            if (signInTask.IsFaulted)
            {
                string exceptionMessage = signInTask.Exception?.InnerException?.Message ?? 
                                        signInTask.Exception?.Message ?? "Unknown error";
                
                if (exceptionMessage.Contains("already signing in"))
                {
                    Debug.LogWarning("[RelayManager] Sign-in was already in progress, waiting for completion...");
                    // Wait and check again
                    yield return new WaitForSeconds(1f);
                    
                    if (AuthenticationService.Instance.IsSignedIn)
                    {
                        Debug.Log("[RelayManager] Player is now signed in");
                        isInitialized = true;
                        Debug.Log("[RelayManager] Services initialized and authenticated");
                        yield break;
                    }
                }
                
                lastException = signInTask.Exception;
                Debug.LogError($"[RelayManager] Failed to sign in: {signInTask.Exception}");
                yield break;
            }
        }

        isInitialized = true;
        Debug.Log("[RelayManager] Services initialized and authenticated");
    }

    /// <summary>
    /// Allocate a relay session as host.
    /// Note: This requires the Relay API to be available in the Multiplayer Services package.
    /// If not available, this will fail gracefully.
    /// </summary>
    public IEnumerator AllocateRelay(Action<string> onAllocationComplete = null)
    {
        if (!isInitialized)
        {
            Debug.LogError("[RelayManager] Services not initialized!");
            yield break;
        }

        Debug.Log("[RelayManager] Attempting to allocate relay session...");
        
        lastException = null;

        // Attempt to find and use RelayService
        // This is version-dependent - the API may not exist in some Multiplayer Services versions
        var relayType = Type.GetType("Unity.Services.Relay.RelayService, Unity.Services.Relay");
        if (relayType == null)
        {
            Debug.LogWarning("[RelayManager] Relay API not available in this version. Falling back to local network only.");
            // For now, generate a local join code instead
            GenerateLocalJoinCode();
            onAllocationComplete?.Invoke(currentJoinCode);
            yield break;
        }

        // Use reflection to call RelayService methods dynamically
        var instanceProperty = relayType.GetProperty("Instance");
        var allocationMethod = relayType.GetMethod("CreateAllocationAsync");
        var getCodeMethod = relayType.GetMethod("GetJoinCodeAsync");
        
        if (instanceProperty == null || allocationMethod == null || getCodeMethod == null)
        {
            Debug.LogWarning("[RelayManager] Relay methods not accessible. Falling back to local network only.");
            GenerateLocalJoinCode();
            onAllocationComplete?.Invoke(currentJoinCode);
            yield break;
        }

        var relayInstance = instanceProperty.GetValue(null);
        
        // Call CreateAllocationAsync(4) for max 4 players
        var taskResult = allocationMethod.Invoke(relayInstance, new object[] { 4 });
        if (taskResult is Task allocationTask)
        {
            yield return new WaitUntil(() => allocationTask.IsCompleted);

            if (allocationTask.IsFaulted)
            {
                Debug.LogError($"[RelayManager] Allocation failed: {allocationTask.Exception}");
                GenerateLocalJoinCode();
                onAllocationComplete?.Invoke(currentJoinCode);
                yield break;
            }

            // Get the Result property (it's a generic Task)
            var resultProperty = allocationTask.GetType().GetProperty("Result");
            if (resultProperty == null)
            {
                Debug.LogError("[RelayManager] Could not get allocation result");
                GenerateLocalJoinCode();
                onAllocationComplete?.Invoke(currentJoinCode);
                yield break;
            }

            var allocation = resultProperty.GetValue(allocationTask);
            var allocationIdProperty = allocation.GetType().GetProperty("AllocationId");
            var allocationId = allocationIdProperty?.GetValue(allocation);
            
            Debug.Log($"[RelayManager] Allocation created: {allocationId}");

            // Get join code
            var getCodeTask = (Task)getCodeMethod.Invoke(relayInstance, new object[] { allocationId });
            yield return new WaitUntil(() => getCodeTask.IsCompleted);

            if (getCodeTask.IsFaulted)
            {
                Debug.LogError($"[RelayManager] Get join code failed: {getCodeTask.Exception}");
                GenerateLocalJoinCode();
                onAllocationComplete?.Invoke(currentJoinCode);
                yield break;
            }

            var codeProperty = getCodeTask.GetType().GetProperty("Result");
            currentJoinCode = (string)codeProperty?.GetValue(getCodeTask);
            
            Debug.Log($"[RelayManager] Relay allocated! Join code: {currentJoinCode}");
            SetupHostTransport(allocation);
            onAllocationComplete?.Invoke(currentJoinCode);
        }
        else
        {
            Debug.LogWarning("[RelayManager] Could not allocate relay. Falling back to local network.");
            GenerateLocalJoinCode();
            onAllocationComplete?.Invoke(currentJoinCode);
        }
    }

    /// <summary>
    /// Join a relay session as client
    /// </summary>
    public IEnumerator JoinRelay(string joinCode, Action onJoinComplete = null)
    {
        if (!isInitialized)
        {
            Debug.LogError("[RelayManager] Services not initialized!");
            yield break;
        }

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("[RelayManager] Join code is empty!");
            yield break;
        }

        Debug.Log($"[RelayManager] Attempting to join relay with code: {joinCode}");
        
        lastException = null;

        var relayType = Type.GetType("Unity.Services.Relay.RelayService, Unity.Services.Relay");
        if (relayType == null)
        {
            Debug.LogWarning("[RelayManager] Relay API not available.");
            currentJoinCode = joinCode;
            onJoinComplete?.Invoke();
            yield break;
        }

        var instanceProperty = relayType.GetProperty("Instance");
        var joinMethod = relayType.GetMethod("JoinAllocationAsync");
        
        if (instanceProperty == null || joinMethod == null)
        {
            Debug.LogWarning("[RelayManager] Relay join method not accessible.");
            currentJoinCode = joinCode;
            onJoinComplete?.Invoke();
            yield break;
        }

        var relayInstance = instanceProperty.GetValue(null);
        var joinTask = (Task)joinMethod.Invoke(relayInstance, new object[] { joinCode });
        
        yield return new WaitUntil(() => joinTask.IsCompleted);

        if (joinTask.IsFaulted)
        {
            Debug.LogError($"[RelayManager] Join failed: {joinTask.Exception}");
            onJoinComplete?.Invoke();
            yield break;
        }

        var resultProperty = joinTask.GetType().GetProperty("Result");
        var joinAllocation = resultProperty?.GetValue(joinTask);
        
        currentJoinCode = joinCode;
        Debug.Log("[RelayManager] Joined relay session successfully");
        SetupClientTransport(joinAllocation);
        onJoinComplete?.Invoke();
    }

    /// <summary>
    /// Generate a local join code for fallback when Relay is unavailable
    /// </summary>
    private void GenerateLocalJoinCode()
    {
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        currentJoinCode = "";
        for (int i = 0; i < 6; i++)
        {
            currentJoinCode += chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        Debug.Log($"[RelayManager] Generated local fallback join code: {currentJoinCode}");
    }

    /// <summary>
    /// Configure transport for host
    /// </summary>
    private void SetupHostTransport(object allocation)
    {
        if (allocation == null)
        {
            Debug.Log("[RelayManager] Allocation is null - skipping transport setup (using local network)");
            return;
        }

        networkManager = FindAnyObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("[RelayManager] NetworkManager not found!");
            return;
        }

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[RelayManager] UnityTransport not found!");
            return;
        }

        // Try to extract relay server data
        var allocType = allocation.GetType();
        var relayServerProperty = allocType.GetProperty("RelayServer");
        var allocationIdBytesProperty = allocType.GetProperty("AllocationIdBytes");
        var keyProperty = allocType.GetProperty("Key");
        
        if (relayServerProperty == null)
        {
            Debug.Log("[RelayManager] Could not find RelayServer property - relay API may be different");
            return;
        }

        var relayServer = relayServerProperty.GetValue(allocation);
        var relayServerType = relayServer.GetType();
        var ipProperty = relayServerType.GetProperty("IpV4");
        var portProperty = relayServerType.GetProperty("Port");
        
        if (ipProperty != null && portProperty != null)
        {
            var ip = (string)ipProperty.GetValue(relayServer);
            var port = (ushort)portProperty.GetValue(relayServer);
            var allocationIdBytes = (byte[])allocationIdBytesProperty?.GetValue(allocation);
            var key = (byte[])keyProperty?.GetValue(allocation);
            
            transport.SetRelayServerData(ip, port, allocationIdBytes, key, null, null);
            Debug.Log("[RelayManager] Host transport configured for Relay");
        }
    }

    /// <summary>
    /// Configure transport for client
    /// </summary>
    private void SetupClientTransport(object joinAllocation)
    {
        if (joinAllocation == null)
        {
            Debug.Log("[RelayManager] Join allocation is null - skipping transport setup (using local network)");
            return;
        }

        networkManager = FindAnyObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            Debug.LogError("[RelayManager] NetworkManager not found!");
            return;
        }

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[RelayManager] UnityTransport not found!");
            return;
        }

        var allocType = joinAllocation.GetType();
        var relayServerProperty = allocType.GetProperty("RelayServer");
        var allocationIdBytesProperty = allocType.GetProperty("AllocationIdBytes");
        var keyProperty = allocType.GetProperty("Key");
        
        if (relayServerProperty == null)
        {
            Debug.Log("[RelayManager] Could not find RelayServer property - relay API may be different");
            return;
        }

        var relayServer = relayServerProperty.GetValue(joinAllocation);
        var relayServerType = relayServer.GetType();
        var ipProperty = relayServerType.GetProperty("IpV4");
        var portProperty = relayServerType.GetProperty("Port");
        
        if (ipProperty != null && portProperty != null)
        {
            var ip = (string)ipProperty.GetValue(relayServer);
            var port = (ushort)portProperty.GetValue(relayServer);
            var allocationIdBytes = (byte[])allocationIdBytesProperty?.GetValue(joinAllocation);
            var key = (byte[])keyProperty?.GetValue(joinAllocation);
            
            transport.SetRelayServerData(ip, port, allocationIdBytes, key, null, null);
            Debug.Log("[RelayManager] Client transport configured for Relay");
        }
    }

    public string GetCurrentJoinCode() => currentJoinCode;
}
