using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName;
    public GameObject PlayButtons;
    public GameObject MainMenuButtons;
    public GameObject JoinButtons;
    public TMP_InputField JoinCodeInputField;
    public TextMeshProUGUI JoinCodeDisplay; // To display the host's join code

    public void Start()
    {
        // Ensure only main menu buttons are active at start
        MainMenuButtons.SetActive(true);
        PlayButtons.SetActive(false);
        JoinButtons.SetActive(false);
    }

    public void HostGame()
    {
        // Load the game scene first
        SceneManager.LoadScene(gameSceneName);
        
        // After scene is loaded, start as host
        SceneManager.sceneLoaded += OnGameSceneLoaded;
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName)
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
            
            // Start as host and register the owner player
            if (MultiplayerManager.Instance != null)
            {
                MultiplayerManager.Instance.StartAsHost();
                Debug.Log("[MainMenu] Host game started and owner registered");
                
                // Display join code to host
                if (JoinCodeDisplay != null)
                {
                    JoinCodeDisplay.text = $"Join Code: {MultiplayerManager.Instance.CurrentJoinCode}";
                    JoinCodeDisplay.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("[MainMenu] MultiplayerManager.Instance not found!");
            }
        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void StartGame()
    {
        MainMenuButtons.SetActive(false);
        PlayButtons.SetActive(true);
    }

    public void ShowJoinMenu()
    {
        PlayButtons.SetActive(false);
        JoinButtons.SetActive(true);
    }

    public void BackToMainMenu()
    {
        PlayButtons.SetActive(false);
        JoinButtons.SetActive(false);
        MainMenuButtons.SetActive(true);
    }

    public void JoinGame()
    {
        if (JoinCodeInputField == null || string.IsNullOrEmpty(JoinCodeInputField.text))
        {
            Debug.LogWarning("[MainMenu] Join code input field is empty!");
            return;
        }
        
        string joinCode = JoinCodeInputField.text.ToUpper().Trim();
        Debug.Log($"[MainMenu] Joining with code: {joinCode}");
        
        // Load the game scene first
        SceneManager.LoadScene(gameSceneName);
        
        // After scene is loaded, join with the code
        SceneManager.sceneLoaded += (scene, mode) => OnGameSceneLoadedForJoin(scene, mode, joinCode);
    }
    
    private void OnGameSceneLoadedForJoin(Scene scene, LoadSceneMode mode, string joinCode)
    {
        if (scene.name == gameSceneName)
        {
            if (MultiplayerManager.Instance != null)
            {
                MultiplayerManager.Instance.JoinWithCode(joinCode);
                Debug.Log("[MainMenu] Client game started with join code");
            }
            else
            {
                Debug.LogError("[MainMenu] MultiplayerManager.Instance not found!");
            }
        }
    }

}
