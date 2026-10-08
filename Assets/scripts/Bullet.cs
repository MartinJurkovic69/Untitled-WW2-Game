using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Impact Prefabs")]
    [SerializeField] private GameObject stoneImpactPrefab;
    [SerializeField] private GameObject woodImpactPrefab;
    [SerializeField] private GameObject metalImpactPrefab;
    [SerializeField] private GameObject defaultImpactPrefab; // Fallback if a tag doesn't match

    private void OnCollisionEnter(Collision collision)
    {
        // Don't collide with other bullets
        if (collision.gameObject.CompareTag("Bullet")) return;

        // 1. Create the correct impact effect based on the surface tag
        CreateBulletImpactEffect(collision);

        // 2. Handle specific gameplay logic if needed (like destroying a "Target")
        if (collision.gameObject.CompareTag("Target"))
        {
            print("Target hit: " + collision.gameObject.name);
            // Add custom target destruction or scoring logic here if you want!
        }

        // 3. Destroy the bullet on any valid impact
        Destroy(gameObject);
    }

    void CreateBulletImpactEffect(Collision collision)
    {
        // Get the exact point of impact
        ContactPoint contact = collision.contacts[0];

        // Determine which prefab to use based on the tag
        GameObject prefabToSpawn = defaultImpactPrefab;

        if (collision.gameObject.CompareTag("Stone"))
        {
            prefabToSpawn = stoneImpactPrefab;
        }
        else if (collision.gameObject.CompareTag("Wood"))
        {
            prefabToSpawn = woodImpactPrefab;
        }
        else if (collision.gameObject.CompareTag("Metal"))
        {
            prefabToSpawn = metalImpactPrefab;
        }

        // Check if we have a prefab assigned before instantiating
        if (prefabToSpawn != null)
        {
            // Spawn the effect flat against the surface using Quaternion.LookRotation(contact.normal)
            GameObject impact = Instantiate(prefabToSpawn, contact.point, Quaternion.LookRotation(contact.normal));

            // Parent it to the object so it moves with it (e.g., if a door opens or an object rolls)
            impact.transform.SetParent(collision.gameObject.transform);
        }
    }
}