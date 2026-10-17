using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused {get; private set;}

    [Header("UI")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button menuButton;
    public Button exitButton;

    [Header("Scenes")]
    public string menuSceneName = "Main Menu";

    private void Start()
    {
        IsPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (menuButton != null) menuButton.onClick.AddListener(ReturnToMenu);
        if (exitButton != null) exitButton.onClick.AddListener(ExitToDesktop);
    }
    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (IsPaused) Resume();
        else Pause();
    }
    public void Pause()
    {
        IsPaused = true;
        pausePanel.SetActive(true);

        Time.timeScale = 0f;
        AudioListener.pause = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void Resume()
    {
        IsPaused = false;
        pausePanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void ReturnToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(menuSceneName);
    }
    public void ExitToDesktop()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else 
            Application.Quit();
        #endif
    }
}
