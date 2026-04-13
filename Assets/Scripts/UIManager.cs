using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    //pause menu references
    public string mainMenuSceneName;
    public GameObject pauseMenuUI;
    public TextMeshProUGUI joinCodeText; // Join code display in pause menu

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
        if(pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Guard: check if gameManager is initialized
        if (gameManager == null)
            return;

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
                if (p == null) continue; // Skip null players
                
                PlayerStats stats = p.GetComponent<PlayerStats>();
                if (stats != null && stats.currentPos == floorManager.finishTile.tileID)
                {
                    winScreen.gameObject.SetActive(true);
                    winScreenText.text = p.name + " Won";
                }
            }
        }
        
    }
    
    void UpdatePlayerToMove()
    {
        // Guard: check if we have players and valid index
        if (gameManager.players.Count == 0)
        {
            Debug.LogWarning("[UIManager] No players registered yet");
            playerToMoveText.text = "Waiting for players...";
            return;
        }

        if (gameManager.playerToMove < 0 || gameManager.playerToMove >= gameManager.players.Count)
        {
            //Debug.LogWarning($"[UIManager] playerToMove ({gameManager.playerToMove}) out of bounds for {gameManager.players.Count} players");
            gameManager.playerToMove = 0; // Reset to first player
        }

        GameObject currentPlayer = gameManager.players[gameManager.playerToMove];
        if (currentPlayer != null)
        {
            playerToMoveText.text = currentPlayer.name + "'s Turn";
        }

        // Update playerToMoveBackground with sprites from the list
        if (player != null)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null && stats.playerToMoveBackgrounds.Count > 0)
            {
                // Get the sprite for the current player's turn
                if (gameManager.playerToMove < stats.playerToMoveBackgrounds.Count && stats.playerToMoveBackgrounds[gameManager.playerToMove] != null)
                {
                    if (playerToMoveBackground != null)
                    {
                        Image backgroundImage = playerToMoveBackground.GetComponent<Image>();
                        if (backgroundImage != null)
                        {
                            backgroundImage.sprite = stats.playerToMoveBackgrounds[gameManager.playerToMove];
                        }
                    }
                }
            }
        }
    }


    public void RollThree()
    {
        if (!gameManager.rolledThree)
        {
            rollThree.SetActive(false);
            return;
        }

        // In networked games, only show to the active player (owner of that player object)
        if (gameManager.players.Count > 0)
        {
            GameObject activePlayer = gameManager.players[gameManager.playerToMove];
            
            // Check if this client controls the active player
            if (activePlayer != null)
            {
                NetworkPlayerController npc = activePlayer.GetComponent<NetworkPlayerController>();
                if (npc != null && !npc.IsOwner)
                {
                    // This client doesn't control the active player, hide UI
                    rollThree.SetActive(false);
                }
                else
                {
                    // This client controls the active player, show UI
                    rollThree.SetActive(true);
                }
            }
        }
        else
        {
            // Local game or no players yet
            rollThree.SetActive(true);
        }
    }

    // Called by the Move 3 button
    public void OnMoveThreeButtonClicked()
    {
        if (gameManager == null || gameManager.players.Count == 0)
        {
            Debug.LogWarning("[UIManager] GameManager or players not initialized in OnMoveThreeButtonClicked");
            return;
        }

        // Get the active player
        GameObject activePlayer = gameManager.players[gameManager.playerToMove];
        if (activePlayer == null)
        {
            Debug.LogWarning("[UIManager] Active player is null in OnMoveThreeButtonClicked");
            return;
        }

        // Get the active player's PlayerActions component and call MoveThree
        PlayerActions playerActions = activePlayer.GetComponent<PlayerActions>();
        if (playerActions == null)
        {
            Debug.LogWarning("[UIManager] PlayerActions component not found on active player");
            return;
        }

        Debug.Log($"[UIManager] Calling MoveThree() on {activePlayer.name}");
        playerActions.MoveThree();
    }

    //Pause menu logic
    public void MainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
    public void Continue()
    {
        player.GetComponent<PlayerActions>().inputMenu = false;
        pauseMenuUI.SetActive(false);
    }

    /// <summary>
    /// Display the join code in the pause menu
    /// </summary>
    public void DisplayJoinCode()
    {
        if (joinCodeText == null)
        {
            Debug.LogWarning("[UIManager] joinCodeText is not assigned in inspector!");
            return;
        }

        MultiplayerManager multiplayerManager = MultiplayerManager.Instance;
        if (multiplayerManager != null)
        {
            string code = multiplayerManager.CurrentJoinCode;
            if (!string.IsNullOrEmpty(code))
            {
                joinCodeText.text = $"Join Code: {code}";
            }
            else
            {
                joinCodeText.text = "Join Code: Not Available";
            }
        }
        else
        {
            joinCodeText.text = "Join Code: Not Connected";
        }
    }
}

    
