using TMPro;
using UnityEngine;

public class AmmoManager : MonoBehaviour
{
        public static AmmoManager Instance { get; set; }

        //UI
        public TextMeshProUGUI ammoDisplay;
        public TextMeshProUGUI reloadPrompt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
}