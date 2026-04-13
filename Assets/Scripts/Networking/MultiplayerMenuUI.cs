using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Multiplayer menu UI for selecting game mode and managing connections.
/// </summary>
public class MultiplayerMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject hostClientPanel;
    [SerializeField] private GameObject joinPanel;
    
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    
    [SerializeField] private Button joinHostButton;
    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    
    private MultiplayerManager multiplayerManager;

    private void Start()
    {
        multiplayerManager = MultiplayerManager.Instance;
        
        if (multiplayerManager == null)
        {
            Debug.LogError("[MultiplayerMenuUI] MultiplayerManager not found!");
            return;
        }

        SetupButtons();
        ShowMainMenu();
    }

    private void SetupButtons()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        clientButton.onClick.AddListener(OnClientClicked);
        joinHostButton.onClick.AddListener(OnJoinHostClicked);
    }

    private void OnLocalGameClicked()
    {
        Debug.Log("[MultiplayerMenuUI] Starting local game");
        multiplayerManager.StartLocalGame();
        gameObject.SetActive(false);
    }

    private void OnHostClicked()
    {
        ShowHostClientPanel();
        Debug.Log("[MultiplayerMenuUI] Host selected");
        multiplayerManager.StartAsHost();
        UpdateConnectionStatus("Starting as Host...");
    }

    private void OnClientClicked()
    {
        ShowJoinPanel();
    }

    private void OnJoinHostClicked()
    {
        string joinCode = ipAddressInput.text;
        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("[MultiplayerMenuUI] Join code cannot be empty!");
            UpdateConnectionStatus("Please enter a join code");
            return;
        }
        
        joinCode = joinCode.ToUpper().Trim();
        Debug.Log($"[MultiplayerMenuUI] Joining with code: {joinCode}");
        multiplayerManager.JoinWithCode(joinCode);
        UpdateConnectionStatus($"Joining with code {joinCode}...");
    }

    private void OnBackClicked()
    {
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        hostClientPanel.SetActive(false);
        joinPanel.SetActive(false);
    }

    private void ShowHostClientPanel()
    {
        mainMenuPanel.SetActive(false);
        hostClientPanel.SetActive(true);
        joinPanel.SetActive(false);
    }

    private void ShowJoinPanel()
    {
        mainMenuPanel.SetActive(false);
        hostClientPanel.SetActive(false);
        joinPanel.SetActive(true);
    }

    private void UpdateConnectionStatus(string status)
    {
        if (connectionStatusText != null)
        {
            connectionStatusText.text = status;
        }
    }
}
