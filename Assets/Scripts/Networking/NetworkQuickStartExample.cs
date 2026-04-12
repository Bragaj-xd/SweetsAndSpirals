using UnityEngine;

/// <summary>
/// QuickStart reference guide - This file contains examples and patterns only.
/// Do NOT instantiate this as a component. Use as code reference.
/// Copy patterns from this file into your actual game scripts.
/// </summary>
public static class NetworkQuickStartReference
{
    /*
    =========================================================
    STEP 1: Scene Setup
    =========================================================
    
    1. Create empty scene
    2. Add NetworkManager + UnityTransport
    3. Add MultiplayerManager
    4. Add Players with:
       - NetworkObject
       - NetworkPlayerController
       - PlayerActions
       - PlayerStats
    
    
    =========================================================
    STEP 2: Modify GameManager.cs
    =========================================================
    
    // Add at top:
    using Unity.Netcode;
    
    public class GameManager : MonoBehaviour
    {
        private NetworkGameManager networkGameManager;
        
        public void SetGameMode(bool enableNetworking)
        {
            if (enableNetworking)
            {
                networkGameManager = GetComponent<NetworkGameManager>();
            }
        }
    }
    
    
    =========================================================
    STEP 3: Modify PlayerActions.cs
    =========================================================
    
    // Add at top:
    using Unity.Netcode;
    
    public class PlayerActions : MonoBehaviour
    {
        private NetworkPlayerController networkController;
        
        void Start()
        {
            networkController = GetComponent<NetworkPlayerController>();
        }
        
        void HandleLeftClick()
        {
            if (networkController != null)
            {
                networkController.RollDiceServerRpc(mousePos);
            }
            else
            {
                // Existing local logic
                diceRoll.SpinTheWheel();
            }
        }
    }
    
    
    =========================================================
    EXAMPLE: Basic RPC Communication
    =========================================================
    
    // CLIENT sends action to SERVER:
    [Rpc(SendTo.Server)]
    private void RequestActionServerRpc(int actionId)
    {
        // Only runs on server
        //Debug.Log($"Server processing action {actionId}");
        
        // Process and validate
        bool valid = ValidateAction(actionId);
        
        // Send result to ALL clients:
        if (valid)
            BroadcastActionResultClientRpc(actionId, true);
    }
    
    // SERVER broadcasts to ALL CLIENTS:
    [Rpc(SendTo.All)]
    private void BroadcastActionResultClientRpc(int actionId, bool success)
    {
        // Runs on all clients (including server)
        //Debug.Log($"Action {actionId} result: {success}");
        
        // Update game state on all clients
        if (success)
            ApplyGameStateChange(actionId);
    }
    
    
    =========================================================
    EXAMPLE: Network Variables (Auto-Sync)
    =========================================================
    
    public class ExampleNetworkBehaviour
    {
        // These sync automatically!
        // private NetworkVariable<int> playerScore = new NetworkVariable<int>(0);
        // private NetworkVariable<bool> isGameActive = new NetworkVariable<bool>(false);
        
        // Subscribe to changes:
        // playerScore.OnValueChanged += (old, newVal) => 
        //     Debug.Log($"Score changed: {old} -> {newVal}");
        
        // Only server can modify:
        // public void SetScore(int newScore)
        // {
        //     if (IsServer)
        //         playerScore.Value = newScore;
        // }
    }
    
    
    =========================================================
    QUICK REFERENCE: Common Patterns
    =========================================================
    
    // 1. Only host/owner can do something:
    // if (IsOwner) { }
    // if (IsServer) { }
    // if (IsHost) { }
    
    // 2. Send data from client to server:
    // [Rpc(SendTo.Server)]
    // void RequestServerRpc(int data) { }
    
    // 3. Server broadcasts to all clients:
    // [Rpc(SendTo.All)]
    // void UpdateAllClientsClientRpc(int data) { }
    
    // 4. Sync state automatically:
    // NetworkVariable<int> myValue = new NetworkVariable<int>(0);
    // Assign on server: myValue.Value = 5;
    // Changes automatically on all clients!
    
    // 5. Handle player disconnect:
    // public override void OnNetworkDespawn()
    // {
    //     base.OnNetworkDespawn();
    //     Debug.Log("Player disconnected");
    // }
    
    
    =========================================================
    UI SETUP (MultiplayerMenuUI)
    =========================================================
    
    Canvas Setup:
    
    Main Menu Panel
    ├─ Local Game Button → MultiplayerManager.StartLocalGame()
    ├─ Host Button       → MultiplayerManager.StartAsHost()
    └─ Join Button       → Show Join Panel
    
    Join Panel
    ├─ IP Address Input Field
    └─ Join Button       → MultiplayerManager.StartAsClient(ipInput.text)
    */
}

