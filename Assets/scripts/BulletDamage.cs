using UnityEngine;

// Added to bullets so they hurt things. Weapon.cs sets the damage when it fires.
// Bullets never damage the team in "ignoreTeam" (so your own bullets can't hurt you or your allies).
public class BulletDamage : MonoBehaviour
{
    public float damage = 25f;
    public Team ignoreTeam = Team.Player;
    public bool logHits = true;      // prints what the bullet hit, turn off once everything works

    private bool hasHit;

    private void Awake()
    {
        // Fast bullets can otherwise skip through thin colliders
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.collider);
    }

    // In case the bullet prefab's collider is set to Is Trigger
    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger) TryHit(other);
    }

    private void TryHit(Collider col)
    {
        if (hasHit) return;

        Combatant combatant = col.GetComponentInParent<Combatant>();
        if (combatant != null && combatant.team == ignoreTeam) return;

        hasHit = true;

        IDamageable target = col.GetComponentInParent<IDamageable>();

        if (logHits)
            Debug.Log($"Bullet hit '{col.name}' | damageable: {target != null} | damage: {damage}", col);

        if (target != null) target.TakeDamage(damage);

        // Destroying the bullet stays in your existing code, so nothing is removed here
    }
}