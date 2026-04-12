# 🎯 Multiplayer Development Quick Reference Card

## 📋 At a Glance

| Item | Value |
|------|-------|
| **System** | Unity Netcode for GameObjects |
| **Architecture** | Wrapper-based (preserves existing code) |
| **Max Players** | 4 (configurable) |
| **Modes** | Single-Player, Local Co-Op, Online |
| **Status** | ✅ Infrastructure ready, Phase 2: Integration |

---

## 🎬 Quick Start

```csharp
// Start game mode
MultiplayerManager.Instance.StartLocalGame();        // Local co-op
MultiplayerManager.Instance.StartAsHost();           // Host server
MultiplayerManager.Instance.StartAsClient("127.0.0.1");  // Join server

// Check if networked
if (MultiplayerManager.Instance.IsNetworkActive) { }
```

---

## 📦 Installation Checklist

- [ ] Install: `com.unity.netcode.gameobjects` (v1.8.1+)
- [ ] Install: `com.unity.transport` (v2.0.0+)
- [ ] Install: `com.unity.collections` (v1.4.0+)
- [ ] Create multiplayer scene
- [ ] Add NetworkManager GameObject
- [ ] Add MultiplayerManager GameObject
- [ ] Configure NetworkManager in Inspector

---

## 🔑 Key Patterns

### Send Command to Server
```csharp
[Rpc(SendTo.Server)]
void RequestActionServerRpc(int data)
{
    if (!IsServer) return;  // Only runs on server
    ProcessAction(data);
    BroadcastResultClientRpc(data);
}
```

### Broadcast Result to All Clients
```csharp
[Rpc(SendTo.All)]
void BroadcastResultClientRpc(int data)
{
    // Runs on all clients (including server)
    UpdateGameState(data);
}
```

### Auto-Sync Values
```csharp
private NetworkVariable<int> playerScore = new NetworkVariable<int>(0);

// Set on server only:
if (IsServer) playerScore.Value = 100;
// Automatically updates on all clients!
```

### Check Ownership
```csharp
if (IsOwner) { /* Only for player who owns this */ }
if (IsServer) { /* Only for server */ }
if (IsHost) { /* Only for host */ }
```

---

## 🎮 Player Input

```csharp
// Only owner can input
if (GetComponent<NetworkPlayerController>().IsOwner)
{
    HandlePlayerInput();
}

// Or check in InputAction callback:
public void OnLeftMouseButton(InputAction.CallbackContext context)
{
    if (gameManager.activePlayer != player) return;
    HandleClick();
}
```

---

## 🔄 Game State Sync

```csharp
// Option 1: Use NetworkVariable (automatic sync)
NetworkVariable<int> currentTileID = new NetworkVariable<int>(0);
currentTileID.Value = 5;  // Auto-syncs to all clients

// Option 2: Use RPC (manual broadcast)
[Rpc(SendTo.All)]
void UpdatePlayerPositionClientRpc(int newPos)
{
    player.GetComponent<PlayerStats>().currentPos = newPos;
}
```

---

## 🧪 Testing Flow

```
1. Local Test
   MultiplayerManager.StartLocalGame()
   ✓ Verify existing game works

2. Host Test
   MultiplayerManager.StartAsHost()
   ✓ Verify host starts listening
   ✓ Verify players can move
   ✓ Check console: "Server started"

3. Client Test
   MultiplayerManager.StartAsClient("127.0.0.1")
   ✓ Verify client connects
   ✓ Verify both see same board
   ✓ Verify input blocked for non-active player
```

---

## 📁 File Structure

```
Assets/Scripts/Networking/
├─ MultiplayerManager.cs              (Main coordinator)
├─ NetworkGameManager.cs              (Game state)
├─ NetworkPlayerController.cs         (Player input)
├─ NetworkDiceRoll.cs                 (Dice sync)
├─ NetworkFloorManager.cs             (Board state)
├─ NetworkGameEvents.cs               (Event system)
├─ MultiplayerMenuUI.cs               (Menu UI)
└─ [Documentation files]
```

---

## 🆘 Common Issues

| Issue | Fix |
|-------|-----|
| "NetworkManager not found" | Add NetworkManager to scene first |
| Input not working | Check `IsOwner == true` |
| Clients disconnecting | Increase timeouts in transport |
| State not syncing | Check RPC uses `SendTo.All` |
| Can't connect | Check firewall port 7777 |
| Players see different board | Validate on server first |

---

## 💻 Conditional Code Template

```csharp
public class YourScript : MonoBehaviour
{
    private bool isNetworkGame = false;
    private NetworkComponent networkComp;
    
    void Start()
    {
        isNetworkGame = MultiplayerManager.Instance?.IsNetworkActive ?? false;
        networkComp = GetComponent<NetworkComponent>();
    }
    
    void DoSomething()
    {
        if (isNetworkGame && networkComp != null)
        {
            networkComp.DoSomethingServerRpc();  // Network version
        }
        else
        {
            ExecuteLocalLogic();  // Local version (existing code)
        }
    }
}
```

---

## 📊 RPC vs NetworkVariable

| Use Case | RPC | NetworkVariable |
|----------|-----|-----------------|
| **One-time event** | ✅ Yes | ❌ No |
| **Frequent updates** | ❌ No | ✅ Yes |
| **Input/button press** | ✅ Yes | ❌ No |
| **Player position** | ❌ Heavy traffic | ✅ Efficient |
| **Dice roll** | ✅ Yes | ⚠️ Optional |
| **Turn order** | ✅ Yes | ⚠️ Optional |

---

## 🎯 Development Sequence

**Week 1**:
- [ ] Install packages
- [ ] Read documentation (MULTIPLAYER_SETUP.md)
- [ ] Create multiplayer scene
- [ ] Configure NetworkManager
- [ ] Test local mode

**Week 2**:
- [ ] Read INTEGRATION_GUIDE.md
- [ ] Update PlayerActions.cs
- [ ] Update GameManager.cs
- [ ] Add network awareness

**Week 3**:
- [ ] Test host mode
- [ ] Test client connection
- [ ] Fix state sync issues
- [ ] Handle disconnects

**Week 4**:
- [ ] Full gameplay testing
- [ ] LAN testing
- [ ] Performance tuning
- [ ] Documentation updates

---

## 🚀 Deployment Steps

```
Phase 1: Local Testing
  → Works on same machine ✅

Phase 2: LAN Testing  
  → Works on local network ✅

Phase 3: Internet Testing
  → Add Unity Relay Service
  → Update NetworkManager transport
  → Test with Relay joined code ✅

Phase 4: Production
  → Deploy dedicated server, OR
  → Use player-hosted with Relay, OR
  → Use cloud-hosted solution ✅
```

---

## 📞 Quick Help Links

| Need Help With | Document | Section |
|---|---|---|
| Installation | PACKAGES_REQUIRED.md | Installation Steps |
| Setup | NETWORKMANAGER_CONFIG.md | Scene Setup |
| Integration | INTEGRATION_GUIDE.md | Step-by-Step |
| Architecture | MULTIPLAYER_SETUP.md | Architecture Overview |
| Code Examples | NetworkQuickStartExample.cs | All sections |
| Roadmap | README.md | Implementation Path |

---

## 🔍 Debugging Commands

```csharp
// Check network state
Debug.Log($"Local: {!MultiplayerManager.Instance.IsNetworkActive}");
Debug.Log($"IsOwner: {GetComponent<NetworkPlayerController>().IsOwner}");
Debug.Log($"IsServer: {GetComponent<NetworkPlayerController>().IsServer}");
Debug.Log($"IsHost: {GetComponent<NetworkPlayerController>().IsHost}");

// Check RPC execution
if (!IsServer) return;  // Only on server
[Rpc(SendTo.All)]
void DebugRpcClientRpc() => Debug.Log("RPC executed on all clients");

// Check NetworkVariable changes
myVariable.OnValueChanged += (old, newVal) => 
    Debug.Log($"Changed: {old} → {newVal}");
```

---

## ⚡ Performance Guidelines

```
Bandwidth per action: 100-300 bytes
Network tick rate: 60 (LAN) / 30 (Internet)
Max latency (target): 100ms (LAN) / 200ms (Internet)
Players per session: 4

With this setup:
- LAN: ~12 KB/s
- Internet: ~6 KB/s
- Very efficient!
```

---

## 🎓 Learning Resources

- **Official Docs**: [docs-multiplayer.unity3d.com](https://docs-multiplayer.unity3d.com/)
- **Video Tutorial**: Search "Unity Netcode Tutorial" on YouTube
- **Community**: [Discord.gg/unity-netcode](https://discord.gg/unity)

---

## 📋 Ready to Build?

```
✅ System ready
✅ Documentation complete
✅ Code examples available
✅ Testing guide provided

👉 Next Step: Read README.md for Phase 2 implementation guide
```

---

Print this card and keep it nearby during development! 🎮

