using UnityEngine;
using UnityEngine.AI;

// Temporary: add to an enemy, press Play and read the Console. Delete afterwards.
public class EnemyDebugger : MonoBehaviour
{
    public float interval = 1f;

    private float timer;
    private EnemyAI ai;
    private Combatant me;
    private NavMeshAgent agent;

    private void Awake()
    {
        ai = GetComponent<EnemyAI>();
        me = GetComponent<Combatant>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        Debug.Log($"[{name}] START | AI enabled: {ai.enabled} | agent enabled: {agent.enabled} | on NavMesh: {agent.isOnNavMesh} | my team: {me.team} | registered combatants: {Combatant.All.Count}", this);

        foreach (Combatant c in Combatant.All)
            Debug.Log($"    registered: {c.name}, team {c.team}, dead {c.isDead}", c);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = interval;

        Combatant nearest = null;
        float best = float.MaxValue;

        foreach (Combatant c in Combatant.All)
        {
            if (c == me || c.isDead || c.team == me.team) continue;

            float d = Vector3.Distance(transform.position, c.transform.position);
            if (d < best) { best = d; nearest = c; }
        }

        if (nearest == null)
        {
            Debug.Log($"[{name}] no hostile Combatant exists. Does the player have a Combatant with Team = Player?", this);
            return;
        }

        Vector3 eye = me.EyePosition;
        Vector3 aim = nearest.AimPosition;
        Vector3 to = aim - eye;
        float dist = to.magnitude;

        float angle = Vector3.Angle(transform.forward, new Vector3(to.x, 0f, to.z));
        bool inRange = dist <= ai.viewDistance;
        bool inFov = dist <= ai.closeSenseRange || angle <= ai.viewAngle * 0.5f;
        bool smoke = ThrowableProjectile.IsLineBlockedBySmoke(eye, aim);

        string blocker = "nothing";
        RaycastHit[] hits = Physics.RaycastAll(eye, to.normalized, dist, ai.sightMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit h in hits)
        {
            Combatant hc = h.collider.GetComponentInParent<Combatant>();
            if (hc == me || hc == nearest) continue;
            blocker = h.collider.name;
            break;
        }

        Debug.Log($"[{name}] target {nearest.name} | dist {dist:F1}/{ai.viewDistance} (in range: {inRange}) | angle {angle:F0}° (in FOV: {inFov}) | smoke: {smoke} | blocked by: {blocker}", this);
    }
}