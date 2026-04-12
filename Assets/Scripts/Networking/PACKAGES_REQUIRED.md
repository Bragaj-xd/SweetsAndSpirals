# Required Unity Packages for Multiplayer

Install these packages in your project via the Package Manager.

## Installation Steps

1. In Unity Editor: **Window → TextureImporter → Package Manager**
2. Click **+** button → **Add package by name**
3. Enter package name from list below
4. Wait for installation to complete
5. Repeat for each package

## Required Packages

### Multiplayer & Networking

| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.netcode.gameobjects` | 1.8.1+ | Core networking RPCs & NetworkVariables |
| `com.unity.transport` | 2.0.0+ | Network protocol layer |
| `com.unity.collections` | 1.4.0+ | High-performance collections |

### Optional - Convenience

| Package | Version | Purpose |
|---------|---------|---------|
| `com.unity.services.core` | 1.12.0+ | Unity services authentication |
| `com.unity.services.relay` | 1.1.0+ | Cloud player relay (for internet play) |
| `com.unity.services.lobby` | 1.2.0+ | Player lobby system |
| `com.unity.netcode.components` | 0.1.0+ | Pre-built network components |

## Minimum Setup

These 3 packages are **required**:

```
com.unity.netcode.gameobjects
com.unity.transport
com.unity.collections
```

## Installation via git URL (Alternative)

Paste in Package Manager "Add package from git URL":

```
https://github.com/Unity-Technologies/netcode.git?path=/com.unity.netcode.gameobjects
https://github.com/Unity-Technologies/transport.git?path=/com.unity.transport
https://github.com/Unity-Technologies/Collections.git?path=/com.unity.collections
```

## Verification

After installation, verify in Package Manager window - you should see:

```
✓ Netcode for GameObjects (v1.8.1+)
✓ Transport (v2.0.0+)
✓ Collections (v1.4.0+)
```

## Project Settings

After installing packages, configure:

1. **Edit → Project Settings → Transport**
   - Protocol: UDP ✓
   - Enable: ✓

2. **Edit → Project Settings → Netcode**
   - Default Transport: UnityTransport ✓

## Troubleshooting

### "Package not found"
- Check package name spelling exactly
- Ensure you have internet connection
- Try "Add by git URL" method

### "Compilation errors after install"
- Window → TextureImporter → General → Update all
- Reimport Assets (Ctrl+Shift+R)
- Close and reopen Unity

### "NetworkManager script missing"
- Verify `com.unity.netcode.gameobjects` is installed
- Reimport the package
- Restart Unity

## Version Recommendations

For compatibility with target Unity versions:

| Unity Version | Netcode | Transport | Collections |
|---------------|---------|-----------|-------------|
| 2022.3 LTS | 1.8.1+ | 2.0.0+ | 1.4.0+ |
| 2023.1+ | 1.8.1+ | 2.0.0+ | 1.4.0+ |
| 2023.2+ | 1.9.0+ | 2.1.0+ | 1.5.0+ |

**Recommended**: Use LTS version (2022.3) for stability

## Quick Package Install Script

If you're setting up multiple projects, you can create a file called `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unity.netcode.gameobjects": "1.8.1",
    "com.unity.transport": "2.0.0",
    "com.unity.collections": "1.4.0",
    "com.unity.services.relay": "1.1.0"
  }
}
```

Then Unity will automatically install on next load.

## After Installation - Next Steps

1. ✅ Packages installed
2. ➡️ Create multiplayer scene with NetworkManager
3. ➡️ Add MultiplayerManager to scene
4. ➡️ Configure NetworkManager transport
5. ➡️ Test host/client connection

See **README.md** for full setup instructions.
