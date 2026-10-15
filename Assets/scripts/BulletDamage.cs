using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    public float damage = 25f;
    public Team ignoreTeam = Team.Player;
    private void OllisionEnter(Collision collision)
    {
        Combatant combatant = collision.collider.GetComponentInParent<Combatant>();;
        if (combatant != null && combatant.team == ignoreTeam) return;

        IDamageable target = collision.collider.GetComponentInParent<IDamageable>();
        if (target != null ) target.TakeDamage(damage);  
    }
}
