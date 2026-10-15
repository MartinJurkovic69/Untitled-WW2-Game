using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class OrderTrigger : MonoBehaviour
{
    public Team triggeredBy = Team.Player;
    public bool oneShot = true;
    public float delay = 0f;

    public UnityEvent onTriggered;
    private bool fired;
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
    private void OggerEnter(Collider other)
    {
        if (oneShot && fired) return;

        Combatant combatant = other.GetComponentInParent<Combatant>();
        if (combatant == null || combatant.team != triggeredBy) return;

        fired = true;

        if (delay > 0f) Invoke(nameof(Fire), delay);
        else Fire();

    }
    private void Fire()
    {
        onTriggered?.Invoke();
    }
}
