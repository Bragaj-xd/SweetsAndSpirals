using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI winScreenText;
    public TextMeshProUGUI playerToMoveText;
    public GameObject rollThree;
    public GameManager gameManager;
    public FloorManager floorManager;
    public GameObject player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(winScreenText != null)
        {
            winScreenText.gameObject.SetActive(false);
        }
        if(rollThree != null)
        {
            rollThree.SetActive(false);
        }
        
        
    }

    // Update is called once per frame
    void Update()
    {
        player = gameManager.activePlayer;
        UpdateWinScreen();
        UpdatePlayerToMove();
        RollThree();

    }

    void UpdateWinScreen()
    {
        if(floorManager.finishTile != null)
        {
            foreach(GameObject p in gameManager.players)
            {
                if(p.GetComponent<PlayerStats>().currentPos == floorManager.finishTile.tileID)
                {
                    winScreenText.gameObject.SetActive(true);
                    winScreenText.text = p.name + " Won";
                }
            }
        }
        
    }
    
    void UpdatePlayerToMove()
    {
        playerToMoveText.text = gameManager.players[gameManager.playerToMove].name + "'s Turn";
    }

    public void RollThree()
    {
        if(gameManager.rolledThree)
        {
            rollThree.SetActive(true);
        }
        
    }

    //roll three logic
    public void MoveThree()
    {
        gameManager.UpdatePlayerPosition(player);
        gameManager.rolledThree = false;
        rollThree.SetActive(false);
    }
    public void PickCard()
    {
        gameManager.AddChanceCard(player);
        gameManager.rolledThree = false;
        rollThree.SetActive(false);
    }
}

    
