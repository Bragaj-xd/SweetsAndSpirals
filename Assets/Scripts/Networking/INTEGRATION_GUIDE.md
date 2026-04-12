# Integration Guide: Adding Multiplayer to Existing Code

## Overview
This guide shows how to integrate the new multiplayer system into your existing `PlayerActions.cs`, `GameManager.cs`, and other game scripts **without breaking local-player functionality**.

## Architecture

```
Your Game Logic (Local/Single-Player)
           ↓
    Wrapper Layer (Network-Aware)
           ↓
    Unity Netcode / Relay Service
```

## Step 1: Update PlayerActions.cs

### Add Network Support
At the top of `PlayerActions.cs`:

```csharp
using Unity.Netcode;

public class PlayerActions : MonoBehaviour
{
    // ... existing code ...
    
    private NetworkPlayerController networkController;
    private bool isNetworkGame = false;

    void Start()
    {
        // ... existing start code ...
        
        networkController = GetComponent<NetworkPlayerController>();
        isNetworkGame = MultiplayerManager.Instance != null && 
                       MultiplayerManager.Instance.IsNetworkActive;
    }

    void HandleLeftClick()
    {
        if (isNetworkGame && networkController != null)
        {
            // Network version - send RPC to server
            HandleNetworkClick();
        }
        else
        {
            // Existing local code continues as-is
            // ... all your existing click handling ...
        }
    }

    private void HandleNetworkClick()
    {
        // Use networkController to send RPCs
        // Example for dice roll:
        networkController.RollDiceServerRpc(mousePos);
    }
}
```

## Step 2: Update GameManager.cs

### Add Network Awareness
```csharp
using Unity.Netcode;

public class GameManager : MonoBehaviour
{
    // ... existing code ...
    
    private NetworkGameManager networkGameManager;
    private bool isNetworkGame = false;

    void Start()
    {
        // ... existing initialization ...
        
        // Set network mode
        SetGameMode(MultiplayerManager.Instance != null && 
                   MultiplayerManager.Instance.IsNetworkActive);
    }

    public void SetGameMode(bool enableNetworking)
    {
        isNetworkGame = enableNetworking;
        if (isNetworkGame)
        {
            networkGameManager = GetComponent<NetworkGameManager>();
        }
    }

    // In your turn-changing logic:
    void EndPlayerTurn()
    {
        if (isNetworkGame && networkGameManager != null)
        {
            // Network version
            networkGameManager.RequestTurnChangeServerRpc();
        }
        else
        {
            // Local version - existing code
            playerToMove = (playerToMove + 1) % players.Count;
        }
    }

    // Existing MovePlayerTileByTile - add network call at end
    public IEnumerator MovePlayerTileByTile(GameObject player, int destinationID)
    {
        // ... existing movement code ...
        
        // At the end, after movement completes:
        if (isNetworkGame && networkGameManager != null)
        {
            // Sync movement to all clients
            int playerIndex = players.IndexOf(player);
            networkGameManager.MovePlayerTileByTileClientRpc(playerIndex, destinationID);
        }
        
        yield return null;
    }
}
```

## Step 3: Update DiceRoll.cs

### Add Network Observation
```csharp
using Unity.Netcode;

public class DiceRoll : MonoBehaviour
{
    // ... existing code ...
    
    private NetworkDiceRoll networkDiceRoll;
    private bool isNetworkGame = false;

    void Start()
    {
        isNetworkGame = MultiplayerManager.Instance != null && 
                       MultiplayerManager.Instance.IsNetworkActive;
        
        if (isNetworkGame)
        {
            networkDiceRoll = GetComponent<NetworkDiceRoll>();
        }
    }

    public void SpinTheWheel()
    {
        if (isNetworkGame && networkDiceRoll != null)
        {
            networkDiceRoll.SpinTheWheelServerRpc();
        }
        else
        {
            // Existing local spin logic
            wheelValue = Random.Range(1, 7);
            wheelSpun++;
        }
    }
}
```

## Step 4: Scene Setup for Multiplayer

### Create a "Multiplayer Game" Scene
1. Duplicate your existing game scene
2. Add these GameObjects with components:

```
NetworkManager
├─ NetworkManager (script)
└─ UnityTransport (script)

MultiplayerManagerGO
├─ MultiplayerManager (script)
└─ MultiplayerMenuUI (script)

[Keep all existing GameObjects]
├─ GameManager
│  ├─ NetworkGameManager (new)
│  ├─ GameManager (existing)
│  ├─ DiceRoll
│  │  └─ NetworkDiceRoll (new)
│  └─ FloorManager
│     └─ NetworkFloorManager (new)
│
└─ Players
   ├─ Player 1
   │  ├─ NetworkObject (new)
   │  ├─ NetworkPlayerController (new)
   │  ├─ PlayerActions (existing)
   │  └─ PlayerStats (existing)
   │
   ├─ Player 2 [same setup]
   ├─ Player 3 [same setup]
   └─ Player 4 [same setup]
```

## Step 5: Handle Input Synchronization

### In PlayerActions.cs - Input Filtering
```csharp
public void LeftMouseButton(InputAction.CallbackContext context)
{
    // Only process input if this is the active player
    if (isNetworkGame)
    {
        // In network mode, check if we own this player
        NetworkPlayerController npc = GetComponent<NetworkPlayerController>();
        if (npc != null && !npc.IsOwner)
        {
            return; // Ignore input if not owner
        }
    }
    else
    {
        // Local mode - check active player
        if (gameManager.activePlayer != player)
            return;
    }

    // Continue with existing input logic
    if (!context.performed) return;
    HandleLeftClick();
}
```

## Step 6: RPC Pattern Examples

### Example 1: Moving a Player
```csharp
// In PlayerActions.cs
StartCoroutine(gameManager.MovePlayerTileByTile(player, destinationTile));

// This calls:
// - Local: Direct coroutine execution
// - Network: MovePlayerTileByTile() RPC is sent to server
//           Server broadcasts to all clients via ClientRpc
```

### Example 2: Playing a Card
```csharp
// In PlayerActions.cs - when card is clicked
if (isNetworkGame && networkController != null)
{
    networkController.PlaceCardServerRpc(cardId, startTile, endTile);
}
else
{
    // Local logic
    HandleCardPlacement(cardId, startTile, endTile);
}
```

## Step 7: Testing Strategy

### Test Checklist
- [ ] **Local Mode**: Existing game works without MultiplayerManager
- [ ] **Local + Network Manager**: Game works with MM but set to LocalCoOp
- [ ] **Host Mode**: Player 1 starts as host, game initializes
- [ ] **Client Mode**: Player 2 connects to host
- [ ] **Input Blocking**: Only active player can input
- [ ] **State Sync**: All players see same board state
- [ ] **Reconnection**: Handle player disconnect/reconnect

### Test Execution
```csharp
// In a test script:

// Test 1: Local game
MultiplayerManager.Instance.StartLocalGame();
// Verify: gameManager.isNetworkGame == false

// Test 2: Host game
MultiplayerManager.Instance.StartAsHost();
// Verify: Players can roll dice, move pieces

// Test 3: Client connection
MultiplayerManager.Instance.StartAsClient("127.0.0.1");
// Verify: Client receives server state
```

## Step 8: Conditional Code Pattern

For clean code that supports both modes:

```csharp
public void DoSomething()
{
    if (isNetworkGame)
    {
        // Network version
        networkComponent.DoSomethingServerRpc();
    }
    else
    {
        // Local version (your existing code)
        ExecuteLocalLogic();
    }
}
```

**OR** use a more elegant approach with delegates:

```csharp
public delegate void GameActionHandler();
public GameActionHandler OnPlayerAction;

void Start()
{
    if (isNetworkGame)
    {
        OnPlayerAction = NetworkAction;
    }
    else
    {
        OnPlayerAction = LocalAction;
    }
}

public void TriggerAction()
{
    OnPlayerAction?.Invoke();
}

private void NetworkAction() { /* ... */ }
private void LocalAction() { /* ... */ }
```

## Common Issues & Solutions

### Issue: "NetworkPlayerController not syncing input"
**Solution**: Check that `IsOwner` is true for the local player
```csharp
Debug.Log($"IsOwner: {GetComponent<NetworkPlayerController>().IsOwner}");
```

### Issue: "Only host can see changes"
**Solution**: Make sure all RPCs use `SendTo.All` for client updates
```csharp
[Rpc(SendTo.All)]  // Not just SendTo.Server
public void UpdateGameStateClientRpc() { }
```

### Issue: "Player movement not synchronized"
**Solution**: Call network version from GameManager
```csharp
// In GameManager.MovePlayerTileByTile:
if (!isNetworkGame) yield break;
networkGameManager.BroadcastMovement(playerIndex, destination);
```

## Performance Tips

1. **Only sync changed state**: Use NetworkVariables, not full state every frame
2. **Batch RPCs**: Group multiple actions in one RPC call
3. **Use NetworkLists wisely**: They're cheaper than individual NetworkVariables
4. **Validate on server**: Prevent cheating by validating all actions server-side

## Next: Advanced Topics

- [ ] Implement client-side prediction for smooth movement
- [ ] Add lag compensation for dice rolls
- [ ] Implement player reconnection logic
- [ ] Add persistent lobby system
- [ ] Implement matchmaking

