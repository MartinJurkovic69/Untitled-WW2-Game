using TMPro;
using Unity.Hierarchy.Editor;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AmmoManager : MonoBehaviour
{
        public static AmmoManager Instance { get; set; }

        //UI
        public TextMeshProUGUI ammoDisplay;
        public TextMeshProUGUI reserveDisplay;
        public TextMeshProUGUI reloadPrompt;
        public TextMeshProUGUI ammoWarning;

        [Header("Weapon Info")]
        public GameObject weaponPanel;
        public Image weaponIcon;
        public TextMeshProUGUI weaponNameText;

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