using UnityEngine;

public class DamageTarget : MonoBehaviour, IDamageable
{
    public float health = 100f;

    public void TakeDamage(float amount)
    {
        health -= amount;
        Debug.Log($"{name} took {amount:F0} damage ({Mathf.Max(health, 0f):F0} HP left)", this);

        if (health <= 0f) Destroy(gameObject);
    }
}
