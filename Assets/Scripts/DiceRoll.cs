using UnityEngine;
using TMPro;
using System;
using Unity.Netcode;

public class DiceRoll : MonoBehaviour
{
    public TextMeshProUGUI wheelSpinText;
    public int wheelValue;
    public int cardWheelValue;
    public int wheelSpun = 0;
    public Camera mainCamera;
    private GameManager gameManager;

    void Start()
    {
        //wheelSpinAnimation = GetComponent<Animation>();
        // Find GameManager for ownership checks
        if (gameManager == null)
        {
            gameManager = gameObject.GetComponent<GameManager>();
            if (gameManager == null)
            {
                GameObject gmGO = GameObject.FindGameObjectWithTag("GameManager");
                if (gmGO != null)
                    gameManager = gmGO.GetComponent<GameManager>();
            }
        }
    }
    
    /// <summary>
    /// Legacy method - use SetWheelValue for networked games
    /// </summary>
    public void SpinTheWheel()
    {
        // Only allow the active player to roll
        if (gameManager != null && gameManager.activePlayer != null)
        {
            // Network game: check if this client owns the active player
            NetworkPlayerController npc = gameManager.activePlayer.GetComponent<NetworkPlayerController>();
            if (npc != null && !npc.IsOwner)
            {
                Debug.LogWarning("[DiceRoll] Only the active player can roll the dice");
                return;
            }
        }
        
        Debug.Log(wheelSpun);
        //wheelSpinAnimation.Play();
        wheelValue = UnityEngine.Random.Range(1, 7);
        //wheelValue = ;     //debug
        Debug.Log("Rolled: " + wheelValue);
        wheelSpinText.text = wheelValue.ToString();
        wheelSpun++;
    }
    
    /// <summary>
    /// Set wheel value from network (synced across all clients)
    /// This is called via ExecuteDiceRollClientRpc, which is only broadcast after
    /// server-side validation in RollDiceServerRpc, so no need to validate again here.
    /// </summary>
    public void SetWheelValue(int value)
    {
        wheelValue = value;
        Debug.Log("Rolled: " + wheelValue);
        wheelSpinText.text = wheelValue.ToString();
        wheelSpun++;
    }

    public void SpinTheWheelForCards()
    {
        Debug.Log(wheelSpun);
        //wheelSpinAnimation.Play();
        cardWheelValue = UnityEngine.Random.Range(1, 7);
        //cardWheelValue = 3;     //debug
        Debug.Log("Rolled for cards: " + cardWheelValue);
        wheelSpinText.text = cardWheelValue.ToString();
        
    }

}
