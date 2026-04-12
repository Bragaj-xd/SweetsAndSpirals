using UnityEngine;

/// <summary>
/// Base class for networked game events.
/// Used for synchronizing game state changes across the network.
/// </summary>
public class NetworkGameEvents : MonoBehaviour
{
    public static NetworkGameEvents Instance { get; private set; }

    // Game events
    public delegate void PlayerMovedHandler(GameObject player, int newPosition);
    public delegate void DiceRolledHandler(int value);
    public delegate void TurnChangedHandler(int newPlayerIndex);
    public delegate void CardPlayedHandler(int playerId, int cardId);

    public event PlayerMovedHandler OnPlayerMoved;
    public event DiceRolledHandler OnDiceRolled;
    public event TurnChangedHandler OnTurnChanged;
    public event CardPlayedHandler OnCardPlayed;

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

    public void InvokePlayerMoved(GameObject player, int newPosition)
    {
        OnPlayerMoved?.Invoke(player, newPosition);
    }

    public void InvokeDiceRolled(int value)
    {
        OnDiceRolled?.Invoke(value);
    }

    public void InvokeTurnChanged(int newPlayerIndex)
    {
        OnTurnChanged?.Invoke(newPlayerIndex);
    }

    public void InvokeCardPlayed(int playerId, int cardId)
    {
        OnCardPlayed?.Invoke(playerId, cardId);
    }
}
