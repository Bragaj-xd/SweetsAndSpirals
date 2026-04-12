# Sweets and Spirals - Multiplayer Setup Guide

## Overview
This guide explains how to set up and use the multiplayer system for Sweets and Spirals using Unity Multiplayer Services (UMS) and Netcode for GameObjects.

## Project Structure

```
Assets/Scripts/
├── Networking/
│   ├── MultiplayerManager.cs          # Central multiplayer coordinator
│   ├── NetworkGameManager.cs           # Game state synchronization
│   ├── NetworkPlayerController.cs      # Per-player network logic
│   └── NetworkGameEvents.cs            # Event broadcasting system
├── GameManager.cs                     # Updated with SetGameMode()
├── PlayerActions.cs                   # Existing single-player logic
└── [other game scripts]
```

## Installation Steps

### 1. Install Required Packages
In the Package Manager, install:
- **Netcode for GameObjects** (com.unity.netcode.gameobjects)
- **Transport** (com.unity.transport)
- **Collections** (com.unity.collections)

```bash
# Via Package Manager > Add by name:
com.unity.netcode.gameobjects
com.unity.transport
```

### 2. Scene Setup

#### Single-Player / Local Co-Op Scene
1. Keep existing scene structure as-is
2. Add `MultiplayerManager` prefab to scene (or Script)
3. Ensure `GameManager` has `SetGameMode(false)` called

#### Online/Multiplayer Scene
1. Create a new scene or duplicate existing
2. Add **NetworkManager** GameObject:
   - Component: `Unity.Netcode.NetworkManager`
   - Set Transport: `UnityTransport`
3. Add **MultiplayerManager** GameObject:
   - Component: `MultiplayerManager.cs`
   - Assign NetworkManager reference
   - Set `currentGameMode = OnlineMultiplayer`
4. Add **NetworkGameManager** to GameManager GameObject:
   - Component: `NetworkGameManager.cs`
5. Add **NetworkPlayerController** to each Player:
   - Component: `NetworkPlayerController.cs`
   - Requires `NetworkObject` component on player
6. Add **NetworkObject** to each player:
   - Component: `Unity.Netcode.NetworkObject`

### 3. Set Up Player Prefabs (for networked players)

For each player that will be spawned in online mode:
1. Create a prefab with:
   - `PlayerStats.cs`
   - `PlayerActions.cs`
   - `NetworkObject.cs` (set as owner spawned)
   - `NetworkPlayerController.cs`
2. Assign to `NetworkManager.PlayerPrefab`

## Usage

### Starting a Game

#### Local/Single-Player
```csharp
MultiplayerManager.Instance.StartLocalGame();
```

#### As Host (Server)
```csharp
MultiplayerManager.Instance.StartAsHost();
```

#### As Client
```csharp
MultiplayerManager.Instance.StartAsClient("192.168.1.100");
```

### Game Flow - Networked Version

1. **Player Input**: Only the player who owns the NetworkObject can input
2. **RPC Calls**: Actions are sent to server via ServerRpc
3. **Server Processing**: Server validates and processes actions
4. **Broadcasting**: ServerRpc results broadcast to clients via ClientRpc
5. **State Sync**: NetworkVariables automatically sync changes

### Example: Rolling Dice

**Local Mode** (existing code):
```csharp
// PlayerActions.cs - works as-is
diceRoll.SpinTheWheel();
```

**Network Mode** (wrapper handles it):
```csharp
// NetworkPlayerController.cs intercepts input
RollDiceServerRpc(mousePos);  // Sent to server
// Server processes, broadcasts via ExecuteDiceRollServerRpc()
```

## Key Design Pattern: Wrapper Approach

The existing `PlayerActions` and `GameManager` code is **unchanged** when playing local.

When networking is enabled:
1. `MultiplayerManager` initializes network
2. `NetworkGameManager` wraps `GameManager`
3. `NetworkPlayerController` wraps `PlayerActions`
4. Network communication happens via RPCs and NetworkVariables
5. Local logic still executes normally

## Important NetworkVariables & RPCs

### Key NetworkVariables (sync automatically)
- `networkPlayerToMove` - Current player's turn index
- `networkWheelValue` - Last dice roll value

### Key RPCs (called from clients to server)
- `RollDiceServerRpc()` - Request dice roll
- `PlaceCardServerRpc()` - Place a card
- `MovePlayerServerRpc()` - Move player
- `RequestTurnChangeServerRpc()` - End turn

### Key RPCs (called from server to all clients)
- `ExecuteDiceRollServerRpc()` - Process dice roll
- `ExecuteCardPlacementServerRpc()` - Process card placement
- `MovePlayerTileByTileClientRpc()` - Sync player movement
- `NotifyTurnChangeClientRpc()` - Notify turn change
- `SyncPlayerStateClientRpc()` - Update player stats

## Network Manager Configuration

### UnityTransport Settings
```
Protocol: UDP
Max Send Queue Size: 64
Max Payload Size: 6144
Connection Data: 2048 bytes
Disconnect Timeout: 60 seconds
```

### Connection Settings
- **Host**: Connect via Relay (recommended for internet)
- **Client**: Initialize with guest/host join code

## Troubleshooting

### "NetworkManager not found"
- Ensure NetworkManager is in scene before MultiplayerManager
- Check that NetworkManager has UnityTransport component

### "Player doesn't have input"
- Verify `NetworkObject.IsOwner` is true
- Check `NetworkPlayerController` is enabled on owned players only

### "State not syncing"
- Verify IPCs are called on Server (use `if (!IsServer) return;`)
- Check NetworkVariables are public or use property accessors

### "Players disconnecting"
- Check firewall/network settings
- Increase disconnect timeout in UnityTransport
- Verify relay service is running (if using Relay)

## Next Steps

1. **Test Local Co-Op First**: Ensure existing game works with `SetGameMode(false)`
2. **Implement Server Logic**: Update `GameManager` to validate moves on server
3. **Add Security**: Implement anti-cheat checks in server-side RPCs
4. **Performance**: Use Netcode best practices (only sync changed values)
5. **Deploy**: Use Relay Service for production multiplayer

## References
- [Netcode for GameObjects Docs](https://docs-multiplayer.unity3d.com/netcode/current/about/)
- [NetworkVariable & RPC Guide](https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariables/)
- [Unity Relay Service](https://docs-multiplayer.unity3d.com/relay/current/)

