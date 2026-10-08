using UnityEngine;

public class GlobalReferences : MonoBehaviour
{
        public static GlobalReferences Instance { get; set; }

        public GameObject bulletImpactEffectPrefab; // Reference to the bullet impact effect prefab

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
