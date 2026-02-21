using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI winScreenText;

    public GameObject winScreen;
    public TextMeshProUGUI playerToMoveText;
    public GameObject rollThree;
    public GameManager gameManager;
    public FloorManager floorManager;
    public GameObject player;
    public GameObject playerToMoveBackground;

    public Camera mainCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(winScreenText != null)
        {
            winScreen.gameObject.SetActive(false);
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
                    winScreen.gameObject.SetActive(true);
                    winScreenText.text = p.name + " Won";
                }
            }
        }
        
    }
    
    void UpdatePlayerToMove()
    {
        playerToMoveText.text = gameManager.players[gameManager.playerToMove].name + "'s Turn";
        playerToMoveBackground.GetComponent<UnityEngine.UI.Image>().sprite = player.GetComponent<PlayerStats>().characterSprite;   }

    void UpdateCardTexts()
    {

    }

    public void RollThree()
    {
        if(gameManager.rolledThree)
        {
            rollThree.SetActive(true);
        }
        
    }


}

    
