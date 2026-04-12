using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// NetworkFloorManager handles synchronized board state across all clients.
/// Tracks ladder/snake placements that all players see.
/// </summary>
public class NetworkFloorManager : NetworkBehaviour
{
    private FloorManager localFloorManager;
    
    // Network list to track tile functions
    private NetworkList<int> networkTileFunctions = new NetworkList<int>();
    private bool initialized = false;

    private void Awake()
    {
        localFloorManager = GetComponent<FloorManager>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!initialized && IsServer)
        {
            InitializeTileStates();
            initialized = true;
        }
    }

    private void InitializeTileStates()
    {
        if (localFloorManager != null && localFloorManager.tiles != null)
        {
            networkTileFunctions.Clear();
            foreach (var tile in localFloorManager.tiles)
            {
                if (tile != null)
                {
                    networkTileFunctions.Add(tile.tileFunction);
                }
            }
        }
    }

    /// <summary>
    /// RPC to place a ladder/snake on the board
    /// </summary>
    [ServerRpc]
    public void PlaceSaLServerRpc(int startTileID, int endTileID, int saLType)
    {
        // Validate and place SaL
        ProcessSaLPlacementClientRpc(startTileID, endTileID, saLType);
    }

    [ClientRpc]
    private void ProcessSaLPlacementClientRpc(int startTileID, int endTileID, int saLType)
    {
        if (localFloorManager != null && 
            startTileID < networkTileFunctions.Count && 
            endTileID < networkTileFunctions.Count)
        {
            // Update network state
            if (saLType == 1) // Ladder
            {
                networkTileFunctions[startTileID] = 1;
                networkTileFunctions[endTileID] = 2;
            }
            else if (saLType == 2) // Snake
            {
                networkTileFunctions[startTileID] = 3;
                networkTileFunctions[endTileID] = 4;
            }
            
            //Debug.Log($"[NetworkFloorManager] SaL placed: type={saLType}, start={startTileID}, end={endTileID}");
        }
    }

    /// <summary>
    /// Get tile function from network state
    /// </summary>
    public int GetNetworkTileFunction(int tileID)
    {
        if (tileID >= 0 && tileID < networkTileFunctions.Count)
        {
            return networkTileFunctions[tileID];
        }
        return 0;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkTileFunctions.Dispose();
    }
}
