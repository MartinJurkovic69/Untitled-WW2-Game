namespace ExpObj
{
    using UnityEngine;

    public class ExplosiveObject : MonoBehaviour
    {
        [SerializeField] GameObject brokenBottlePrefab;

        [Header("Sound Effects")]
        [SerializeField] GameObject soundFX;
        GameObject newSFX;
        [SerializeField] AudioClip[] explosionClips;
        [SerializeField] private float pitchVariationRange = 0.1f;
        [SerializeField] float DestroySoundEffectAfter = 5;

        [Header("Visual Effects")]
        [SerializeField] GameObject explosionVFX;
        GameObject newVFX;
        [SerializeField] float DestroyVisualEffectAfter = 7;

        // Instantiating and initializing effects objects at start frame
        private void Start()
        {
            // sfx
            newSFX = Instantiate(soundFX, transform.position, Quaternion.identity);
            newSFX.GetComponent<AudioSource>().clip = explosionClips[Random.Range(0, explosionClips.Length)];
            float randomPitch = 1 + Random.Range(-pitchVariationRange, pitchVariationRange);
            newSFX.GetComponent<AudioSource>().pitch = randomPitch;

            // vfx
            newVFX = Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        // --- NEW CODE: Detects when something hits this object ---
        private void OnCollisionEnter(Collision collision)
        {
            // Check if the object hitting us is a bullet
            // Note: Make sure your Bullet prefab has the Tag "Bullet" set in the Inspector!
            if (collision.gameObject.CompareTag("Bullet"))
            {
                Explode();
                
                // Optional: Destroy the bullet too so it doesn't keep flying through the explosion
                Destroy(collision.gameObject); 
            }
        }

        // Call Explode() function from other scripts to make this object explode
        public void Explode()
        {
            // sfx
            newSFX.transform.position = transform.position;
            newSFX.GetComponent<AudioSource>().Play();
            Destroy(newSFX, DestroySoundEffectAfter);

            // vfx
            newVFX.transform.position = transform.position;
            newVFX.GetComponent<ParticleSystem>().Emit(1);
            if (newVFX.transform.childCount > 0)
            {
                foreach (Transform child in newVFX.transform)
                {
                    child.GetComponent<ParticleSystem>().Emit(1);
                }
            }
            Destroy(newVFX, DestroyVisualEffectAfter);

            // broken bottle instantiation
            if (brokenBottlePrefab != null)
            {
                GameObject brokenBottle = Instantiate(brokenBottlePrefab, transform.position, transform.rotation);
                brokenBottle.GetComponent<BrokenObject>().RandomVelocities();
            }

            // self-destroy
            Destroy(gameObject);
        }
    }
}