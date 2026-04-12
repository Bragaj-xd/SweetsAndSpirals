# NetworkManager Configuration Guide

## Scene Setup for Multiplayer

This guide shows how to properly configure NetworkManager in your multiplayer scene.

## Creating the NetworkManager GameObject

### Step 1: Create GameObject
1. In Scene Hierarchy, right-click → **Create Empty**
2. Rename to `NetworkManager`
3. Reset Transform (right-click Transform → Reset)

### Step 2: Add Components
Select the NetworkManager GameObject and add:
- **Unity.Netcode.NetworkManager** (script)
- **Unity.Transport.UnityTransport** (script)

### Step 3: Configure NetworkManager

In Inspector, set these values:

```
NetworkManager
├─ Connection Data:
│  └─ Protocol Version: 1
├─ Network Config:
│  ├─ Tick Rate: 60
│  ├─ Send Tick Rate: 60
│  └─ Max Clients: 4
├─ Spawning Config:
│  ├─ Player Prefab Assignment: Default Player Prefab
│  │  └─ [Assign your player prefab here]
│  └─ Instance Owner Write Permission: Owner
├─ Logging:
│  └─ Log Level: Normal (or Verbose for debugging)
└─ Transport:
   └─ [Assign UnityTransport component]
```

## Transport Configuration

Select the **UnityTransport** component and configure:

```
UnityTransport
├─ Protocol Type: UDP
├─ Debug: ON (during development)
├─ Max Payload Size: 6144 bytes
├─ Send Queue Batch Size: 64
├─ Fragmented Message Size: 2000 bytes
├─ Server Configuration:
│  ├─ Listen Address: 0.0.0.0
│  ├─ Listen Port: 7777
│  └─ Max Connection Attempts: 60
└─ Client Configuration:
   ├─ Connect Address: localhost (or server IP)
   ├─ Connect Port: 7777
   └─ Connection Data Timeout: 60
```

## Player Prefab Setup

Your player prefab needs these components:

```
Player Prefab (Must be in Resources/Prefabs/)
├─ Transform
├─ [Visual Model/Renderer]
├─ PlayerStats (your script)
├─ PlayerActions (your script)
├─ NetworkObject (→ check "Owner Spawned")
├─ NetworkPlayerController (new networking script)
└─ [Any other player components]
```

### Critical Settings for NetworkObject:
- ✅ **Ownable**: Checked
- ✅ **Owner Spawned**: Checked (if spawning from player)
- ✅ **Is Spawned**: Checked
- ✅ **Network Hidden**: Unchecked

## Scene Hierarchy - Complete View

```
SampleMultiplayerScene
├─ NetworkManager (GameObject)
│  ├─ NetworkManager (script)
│  └─ UnityTransport (script)
│
├─ MultiplayerManager (GameObject)
│  ├─ MultiplayerManager (script)
│  └─ MultiplayerMenuUI (script) [optional]
│
├─ GameManager (GameObject)
│  ├─ GameManager (script)
│  ├─ NetworkGameManager (script)
│  ├─ DiceRoll (script)
│  ├─ NetworkDiceRoll (script)
│  └─ FloorManager (script)
│
├─ FloorManager (GameObject)
│  ├─ FloorManager (script)
│  └─ NetworkFloorManager (script)
│
├─ Players
│  ├─ Player1 (Spawned at runtime from prefab)
│  │  ├─ NetworkObject (script)
│  │  ├─ NetworkPlayerController (script)
│  │  ├─ PlayerActions (script)
│  │  ├─ PlayerStats (script)
│  │  └─ [Other components]
│  │
│  ├─ Player2 [Same structure]
│  ├─ Player3 [Same structure]
│  └─ Player4 [Same structure]
│
└─ UI
   ├─ Canvas
   │  ├─ MainMenuPanel
   │  ├─ HostClientPanel
   │  └─ JoinPanel
   └─ [Other UI elements]
```

## Startup Initialization Order

```
1. Scene loads
   ↓
2. NetworkManager component starts
   ↓
3. MultiplayerManager.Start() called
   ↓
4. User selects game mode from menu
   ↓
5a. LOCAL GAME:
    - MultiplayerManager.StartLocalGame()
    - GameManager.SetGameMode(false)
    - Existing players instantiate (not networked)
    ↓
5b. HOST:
    - MultiplayerManager.StartAsHost()
    - NetworkManager.StartHost()
    - Players spawned as NetworkObjects
    ↓
5c. CLIENT:
    - MultiplayerManager.StartAsClient(ip)
    - NetworkManager.StartClient()
    - Players spawned by server, ownership assigned to owner client
```

## Code Example: Initializing Scene

```csharp
using Unity.Netcode;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private void Start()
    {
        // This will initialize hosting or client mode
        var manager = MultiplayerManager.Instance;
        
        if (manager != null)
        {
            // Menu will let user choose: Local, Host, or Client
            // OR you can start automatically:
            #if UNITY_EDITOR
            manager.StartLocalGame(); // Default for testing
            #endif
        }
    }
}
```

## Important: Player Prefab Path

NetworkManager requires the player prefab at a specific path:

```
Assets/Resources/Prefabs/Player.prefab
              ↑─────────────↑
         Must be in Resources folder!
```

Or assign directly in Inspector:
1. Create your player prefab
2. Drag into Inspector on NetworkManager
3. Assign to "Player Prefab" field

## Connection Flow

### Host Startup
```
Start Host
  ↓
Listen on 0.0.0.0:7777
  ↓
NetworkManager.OnServerStarted event fires
  ↓
Accept client connections
```

### Client Connection
```
Request connection to host IP:Port
  ↓
Send connection data
  ↓
Host approves (or rejects)
  ↓
NetworkManager.OnClientConnected event fires
  ↓
Receive spawned GameObjects with NetworkObjects
```

## Debug Configuration

For development troubleshooting, enable debug logging:

```csharp
// In any NetworkBehaviour:
if (IsServer) Debug.Log("[SERVER] Action processed");
if (IsClient) Debug.Log("[CLIENT] Update received");
if (IsHost) Debug.Log("[HOST] Both client and server");
if (IsOwner) Debug.Log("[OWNER] This player is mine");
```

## Performance Tuning

For different network conditions:

### LAN (Same Network)
```
Tick Rate: 60
Network Tick Rate: 60
Max Connection Attempts: 60
Connection Data Timeout: 60
```

### Internet (Cloud)
```
Tick Rate: 30 (reduced)
Network Tick Rate: 30
Max Connection Attempts: 180 (increased)
Connection Data Timeout: 180 (increased)
```

### Mobile (Unstable)
```
Tick Rate: 20
Network Tick Rate: 20
Max Connection Attempts: 300
Connection Data Timeout: 300
```

## Common Configuration Issues

### Issue: "Port already in use"
**Solution**: Change Listen Port (7777 → 7778)

### Issue: "Client can't find host"
**Solution**: Check firewall allows port 7777
```
Windows: Control Panel → Windows Defender Firewall
  → Allow app through firewall
  → Add your Unity executable
```

### Issue: "Players not spawning"
**Solution**: Verify player prefab path and NetworkObject settings
```
✓ Prefab in Resources/Prefabs/
✓ NetworkObject present on prefab
✓ NetworkObject set to "Owner Spawned"
✓ Player Prefab field assigned in NetworkManager
```

### Issue: "Lag or stuttering"
**Solution**: Reduce Network Tick Rate
```
From: 60 → To: 30 or 20
Reduces network traffic significantly
```

## Network Manager Inspector Checklist

Before testing, verify:

```
✅ NetworkManager component present
✅ UnityTransport component present
✅ Player Prefab assigned
✅ Max Clients set to 4
✅ Listen Port set to 7777
✅ Protocol set to UDP
✅ Connection Approval enabled
✅ Scene is saved
```

## Testing Checklist

### Local Test
```
1. Play scene in Editor
2. Select "Local Game" from menu
3. Verify: Game works (no network involved)
```

### Host Test
```
1. Play scene in Editor
2. Select "Host"
3. Verify: Host starts listening
4. Check Console: "Server started" message
```

### Client Test
```
1. Have 2 instances (Editor + Build, or 2 Builds)
2. Start Host first in one instance
3. Start Client in other instance (IP: 127.0.0.1 or LAN IP)
4. Verify: Client connects, shows "Connected"
5. Verify: Players see same board state
```

---

**Next**: After configuring NetworkManager, see README.md for integration steps.
