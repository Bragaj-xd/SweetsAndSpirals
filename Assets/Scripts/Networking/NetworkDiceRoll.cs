using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Network utility for dice rolling - synchronizes across all clients.
/// </summary>
public class NetworkDiceRoll : NetworkBehaviour
{
    private DiceRoll localDiceRoll;
    private NetworkVariable<int> networkWheelValue = new NetworkVariable<int>(0);
    private NetworkVariable<int> networkWheelSpun = new NetworkVariable<int>(0);
    private NetworkVariable<int> networkCardWheelValue = new NetworkVariable<int>(0);

    private void Awake()
    {
        localDiceRoll = GetComponent<DiceRoll>();
    }

    [ServerRpc]
    public void SpinTheWheelServerRpc()
    {
        if (localDiceRoll != null)
        {
            localDiceRoll.SpinTheWheel();
            networkWheelValue.Value = localDiceRoll.wheelValue;
            networkWheelSpun.Value = localDiceRoll.wheelSpun;
            
            BroadcastWheelSpinClientRpc(localDiceRoll.wheelValue, localDiceRoll.wheelSpun);
        }
    }

    [ClientRpc]
    private void BroadcastWheelSpinClientRpc(int wheelValue, int wheelSpun)
    {
        networkWheelValue.Value = wheelValue;
        networkWheelSpun.Value = wheelSpun;
        //Debug.Log($"[NetworkDiceRoll] Wheel spun: {wheelValue} (total spins: {wheelSpun})");
    }

    [ServerRpc]
    public void SpinTheWheelForCardsServerRpc()
    {
        if (localDiceRoll != null)
        {
            localDiceRoll.SpinTheWheelForCards();
            networkCardWheelValue.Value = localDiceRoll.cardWheelValue;
            
            BroadcastCardWheelSpinClientRpc(localDiceRoll.cardWheelValue);
        }
    }

    [ClientRpc]
    private void BroadcastCardWheelSpinClientRpc(int cardWheelValue)
    {
        networkCardWheelValue.Value = cardWheelValue;
        //Debug.Log($"[NetworkDiceRoll] Card wheel spun: {cardWheelValue}");
    }

    public int GetNetworkWheelValue() => networkWheelValue.Value;
    public int GetNetworkWheelSpun() => networkWheelSpun.Value;
    public int GetNetworkCardWheelValue() => networkCardWheelValue.Value;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkWheelValue.OnValueChanged += OnWheelValueChanged;
        networkWheelSpun.OnValueChanged += OnWheelSpunChanged;
        networkCardWheelValue.OnValueChanged += OnCardWheelValueChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        networkWheelValue.OnValueChanged -= OnWheelValueChanged;
        networkWheelSpun.OnValueChanged -= OnWheelSpunChanged;
        networkCardWheelValue.OnValueChanged -= OnCardWheelValueChanged;
    }

    private void OnWheelValueChanged(int oldValue, int newValue)
    {
        //Debug.Log($"[NetworkDiceRoll] Wheel value changed: {oldValue} -> {newValue}");
    }

    private void OnWheelSpunChanged(int oldValue, int newValue)
    {
        //Debug.Log($"[NetworkDiceRoll] Wheel spun count changed: {oldValue} -> {newValue}");
    }

    private void OnCardWheelValueChanged(int oldValue, int newValue)
    {
        //Debug.Log($"[NetworkDiceRoll] Card wheel value changed: {oldValue} -> {newValue}");
    }
}
