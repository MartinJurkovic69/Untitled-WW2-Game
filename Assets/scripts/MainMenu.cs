using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [System.Serializable]
    public class Story
    {
        public string title;
        public Button button;
        public TextMeshProUGUI label;
        public GameObject wiplabel;
        public string sceneName;
        public bool avalible = true;

    }
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject storyPanel;

    [Header("Main Buttons")]
    public Button playButton;
    public Button exitButton;
    public Button backButton;

    [Header("Stories")]
    public Story[] stories;

    private void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (playButton != null) playButton.onClick.AddListener(ShowStories);
        if (exitButton != null) exitButton.onClick.AddListener(QuitGame);
        if (backButton != null) backButton.onClick.AddListener(ShowMain);

        foreach (Story story in stories)
        {
            Story s = story;
            if (s.button.interactable = s.avalible);

            if (s.wiplabel != null) s.wiplabel.SetActive(!s.avalible);
            if (!s.avalible && s.label != null ) s.label.color = new Color(0.6f,0.6f,0.6f,0.5f);
            if (s.avalible) s.button.onClick.AddListener(() => LoadStory(s.sceneName));
        }
        ShowMain();
    }
    private void Update()
    {
        if (storyPanel != null && storyPanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ShowMain();
        }
    }
    public void ShowStories()
    {
        mainPanel.SetActive(false);
        storyPanel.SetActive(true);
    }
    public void ShowMain()
    {
        storyPanel.SetActive(false);
        mainPanel.SetActive(true);
    }
    public void LoadStory(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("MainMenu: this story has no scene name set");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else 
            Application.Quit();
        #endif
    }
}