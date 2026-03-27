using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName;
    public void MainMenu()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Continue()
    {
        PauseMenu.IsActive(false);
    }
}
