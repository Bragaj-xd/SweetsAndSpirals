using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenu;

    [SerializeField] private string gameSceneName;
    public void MainMenu()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Pokracovat()
    {
        pauseMenu.SetActive(false);
    }
}
