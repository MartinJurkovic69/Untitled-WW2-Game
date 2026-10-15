using UnityEngine;
using System.Collections.Generic;


public enum Team
{
    Player,
    Enemy
}
public class Combatant : MonoBehaviour, IDamageable
{
    public static readonly  List<Combatant> All = new List<Combatant>();

    public Team team = Team.Enemy;
    public float maxHealth = 100f;
    public float health;
    public bool logDamage = true;

    [Header("Sight / aim points")]
    [Tooltip("Offsets from the pivot. (Assumes a 2m tall capsule with pivot in middle )")]
    public float eyeHeight = 0.7f;
    public float aimHeight = 0.3f;

    public bool isDead {get; private set;}
    public event System.Action<Combatant> Died;

    public Vector3 EyePosition => transform.position + Vector3.up * eyeHeight;
    public Vector3 AimPosition => transform.position + Vector3.up * aimHeight;

    private void Awake()
    {
        health = maxHealth;
    }
    private void OnEnable()
    {
       if (!All.Contains(this)) All.Add(this); 
    }
    private void OnDisable()
    {
        All.Remove(this);
    }
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        if (logDamage)
            Debug.Log($"{name} took {amount:F0} damage ({Mathf.Max(health, 0f):F0} HP left)", this);

        if (health <= 0f) Die();
    }
    private void Die()
    {
        isDead = true;
        Debug.Log($"{name} died", this);
        Died?.Invoke(this);
    }
}
