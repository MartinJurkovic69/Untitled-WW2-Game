using System.Collections;
using UnityEngine;
using UnityEngine.AI;



[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Combatant))]
public class EnemyAI : MonoBehaviour , IShovable
{
    public enum AIState {Hold,Patrol,MoveTo,Investigate,Combat,Assault}
    public enum IdleBehaviour {HoldPosition,Patrol}
    [Header("Idle Behaviour")]
    public IdleBehaviour idleBehaviour = IdleBehaviour.HoldPosition;
    public Transform[] patrolPoints;
    public float patrolWaitTime = 3f;
    [Tooltip("True = never leaves its position")]
    public bool holdGround;

    [Header ("Movement")]
    public float walkSpeed = 2f;
    public float runSpeed = 4.5f;
    public float turnSpeed = 240f;
    private Vector3 shoveVelocity;
    private float shoveTimeLeft;
    private float stunTimeLeft;

    [Header("Perception")]
    public float viewDistance = 45f;
    [Range(10f,100f)] public float viewAngle = 110f;
    public float closeSenseRange = 4f; // notices from any angle at this range
    public float reactionTime = 0.6f; // how long target has to be visible to be noticed
    public float perceptionInterval = 0.15f; // how often it checks for targets
    public LayerMask sightMask = ~0; // what blocks line of sight

    [Header("Combat")]
    public float prefferedCombatRange = 18f; // advances untill its this close
    public float assaultCloseRange = 8f;
    public float combatGraceTime = 1f; 
    public float lookAroundTime = 5f;

    [Header("Debug")]
    public Renderer stateIndicator;
    public Transform testOrderPoint;

    [SerializeField] private AIState state;
    public bool IsDead => combatant != null && combatant.isDead;

    private NavMeshAgent agent;
    private Combatant combatant;

    private Vector3 holdPoint;
    private Quaternion holdRotation;
    private Vector3 orderDestination;
    private bool useOrderFacing;

    private bool assaulting;
    private Combatant assaultGoal;

    private int patrolIndex;
    private bool waiting;
    private float waitTimer;

    private Combatant target;
    private bool targetVisible;
    private float awareness;
    private float timeSinceSeen;
    private Vector3 lastKnownPosition;
    private float perceptionTimer;

    private Vector3 investigatePoint;
    private bool investigateArrived;
    private float investigateTimer;
    private float scanBaseYaw;

    private bool facingHandled;
    private Vector3 lastDestination = Vector3 .positiveInfinity;
    private MaterialPropertyBlock mpb;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        combatant = GetComponent<Combatant>();

        agent.updateRotation = false;
        agent.stoppingDistance = 0.3f;

        holdPoint = transform.position;
        holdRotation = transform.rotation;

        combatant.Died += OnDied;
        combatant.Damaged += OnDamaged;
    }

    private void Start()
    {
        if (!agent.isOnNavMesh)
            Debug.LogWarning($"{name} is not on a NavMesh. Bake a NavMesh and place the enemy on it.", this);

            bool patrol = idleBehaviour == IdleBehaviour.Patrol && patrolPoints != null && patrolPoints.Length > 0;
            SetState(patrol ? AIState.Patrol : AIState.Hold);
    }
    private void OnEnable()
    {
        ThrowableProjectile.LureAlert += OnLureAlert;
    }
    private void OnDisable()
    {
        ThrowableProjectile.LureAlert -= OnLureAlert;
    }
    private void OnDestroy()
    {
        if (combatant != null)
        {
            combatant.Died -= OnDied;
            combatant.Damaged -= OnDamaged;
        }
    }
    private void Update()
    {
        if (combatant.isDead) return;
        if (shoveTimeLeft > 0f)
        {
            agent.Move(shoveVelocity * Time.deltaTime);
            shoveTimeLeft -= Time.deltaTime;
        }
        if (stunTimeLeft > 0f)
        {
            stunTimeLeft -= Time.deltaTime;
            return;
        }

        facingHandled = false;

        perceptionTimer -= Time.deltaTime;
        if (perceptionTimer <= 0f)
        {
            perceptionTimer = perceptionInterval;
            Percieve();
        }

        switch (state)
        {
            case AIState.Hold: UpdateHold(); break;
            case AIState.Patrol: UpdatePatrol(); break;
            case AIState.MoveTo: UpdateMoveTo(); break;
            case AIState.Investigate: UpdateInvestigate(); break;
            case AIState.Combat: UpdateCombat(); break;
            case AIState.Assault: UpdateAssault(); break;
        }

        if (!facingHandled && agent.velocity.sqrMagnitude > 0.1f)
            FaceDirection(agent.velocity);
    }
    private void SetState(AIState newState)
    {
        state = newState;

        Color c = Color.white;
        switch (newState)
        {
            case AIState.Hold: c = new Color (0.6f, 0.6f, 0.6f); break;
            case AIState.Patrol: c = new Color (0.2f, 0.8f, 0.3f); break;
            case AIState.MoveTo: c = new Color (0.2f, 0.5f, 1f); break;
            case AIState.Investigate: c = new Color (1f, 0.85f, 0.1f); break;
            case AIState.Combat: c = new Color (1f, 0.15f, 0.15f); break;
            case AIState.Assault: c = new Color (1f, 0.5f, 0f); break;
        }
        SetIndicatorColor(c);
    }
    private void Percieve()
    {
        Combatant seen = FindVisibleTarget();
        if (seen != null)
        {
            awareness = Mathf.Min(awareness + perceptionInterval, reactionTime + 0.5f);

            if (awareness >= reactionTime)
            {
                target = seen;
                if (state != AIState.Combat) SetState(AIState.Combat);
            }
        }
        else
        {
            awareness = Mathf.Max(0f, awareness - perceptionInterval * 0.5f);
        }
        targetVisible = seen != null && seen == target;
    }
    private Combatant FindVisibleTarget()
    {
        Combatant best = null;
        float bestDist = float.MaxValue;

        foreach (Combatant c in Combatant.All)
        {
            if (c == combatant || c.isDead || c.team == combatant.team) continue;
            if (!CanSee(c, out float dist)) continue;

            if (dist < bestDist)
            {
                best = c;
                bestDist = dist;
            }
        }
        return best;
    }
    private Combatant FindNearestHostile()
    {
        Combatant best = null;
        float bestSqr = float.MaxValue;

        foreach (Combatant c in Combatant.All)
        {
            if (c == combatant || c.isDead || c.team == combatant.team) continue;

            float sqr = (c.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                best = c;
                bestSqr = sqr;
            }
        }
        return best;
    }
    private bool CanSee(Combatant other, out float dist)
    {
        Vector3 eyePos = combatant.EyePosition;
        Vector3 targetPos = other.AimPosition;
        Vector3 toTarget = targetPos - eyePos;

        dist = toTarget.magnitude;
        if (dist > viewDistance || dist < 0.01f) return false;

        if (dist > closeSenseRange)
        {
            Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
            if (Vector3.Angle(transform.forward, flat) > viewAngle * 0.5f) return false;
        }

        //Smoke blocks vision
        if (ThrowableProjectile.IsLineBlockedBySmoke(eyePos, targetPos)) return false;

        //Walls / objects block vision
        if (Physics.Raycast(eyePos, toTarget / dist, out RaycastHit hit, dist, sightMask, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<Combatant>() != other) return false;
        }
        return true;
    }
    private void OnLureAlert(Vector3 position, float radius)
    {
        if (combatant.isDead || state == AIState.Combat || assaulting) return;
        if (Vector3.Distance(transform.position, position) > radius) return;

        if (state == AIState.Investigate && (position - investigatePoint).sqrMagnitude <1f) return;

        BeginInvestigate(position);
    }
    private void UpdateHold()
    {
        float dist = Vector3.Distance(transform.position, holdPoint);

        if (dist > 0.7f)
        {
            MoveTo(holdPoint, walkSpeed);
        }
        else
        {
            Stop();
            FaceRotation(holdRotation);
        }
    }
    private void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            UpdateHold();
            return;
        }

        if (!waiting)
        {
            MoveTo(patrolPoints[patrolIndex].position, walkSpeed);

            if (HasArrived())
            {
                waiting = true;
                waitTimer = patrolWaitTime;
            }
        }
        else
        {
            Stop();
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                waiting = false;
            }
        }
    }
    private void UpdateMoveTo()
    {
        MoveTo(orderDestination, runSpeed);

        if (HasArrived())
        {
            if (!useOrderFacing) holdRotation = transform.rotation;
            SetState(AIState.Hold);
        }
    }
    private void UpdateInvestigate()
    {
        if (holdGround)
        {
            FaceDirection(investigatePoint - transform.position);
            Stop();

            investigateTimer += Time.deltaTime;
            if (investigateTimer > lookAroundTime) ReturnToDefault();
            return;
        }
        if (!investigateArrived)
        {
            MoveTo(investigatePoint, runSpeed);
            if (HasArrived())
            {
                investigateArrived = true;
                investigateTimer = 0f;
                scanBaseYaw = transform.eulerAngles.y;
            }
        }
        else
        {
            Stop();
            investigateTimer += Time.deltaTime;

            float yaw = scanBaseYaw + Mathf.Sin(investigateTimer * 1.5f) * 70f;
            FaceRotation(Quaternion.Euler(0f, yaw, 0f));

            if (investigateTimer > lookAroundTime) ReturnToDefault();
        }
    }
    private void UpdateAssault()
    {
        Combatant goal = (assaultGoal != null && !assaultGoal.isDead) ? assaultGoal : FindNearestHostile();

        if (goal == null)
        {
            assaulting = false;
            ReturnToDefault();
            return;
        }
        MoveTo(goal.transform.position, runSpeed);
    }
    private void UpdateCombat()
    {
        if (target == null || target.isDead)
        {
            EndCombat();
            return;
        }

        if (targetVisible)
        {
            timeSinceSeen = 0f;
            lastKnownPosition = target.transform.position;

            FaceDirection(target.AimPosition - transform.position);

            float stopRange = assaulting ? assaultCloseRange : prefferedCombatRange;
            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (!holdGround && dist > stopRange) MoveTo(lastKnownPosition, runSpeed);
            else Stop();
        }
        else
        {
            timeSinceSeen += Time.deltaTime;

            if (timeSinceSeen > combatGraceTime)
            {
                target = null;
                if (assaulting) SetState(AIState.Assault);
                else BeginInvestigate(lastKnownPosition);
            }
        }
    }
    private void EndCombat()
    {
        target = null;
        awareness = 0f;
        if (assaulting) SetState(AIState.Assault);
        else ReturnToDefault();
    }
    private void BeginInvestigate(Vector3 position)
    {
        investigatePoint = position;
        investigateArrived = false;
        investigateTimer = 0f;
        SetState(AIState.Investigate);
    }
    private void ReturnToDefault()
    {
        bool patrol = idleBehaviour == IdleBehaviour.Patrol && patrolPoints != null && patrolPoints.Length > 0;
        waiting = false;
        SetState(patrol ? AIState.Patrol : AIState.Hold);
    }
    public void OrderMoveTo(Vector3 position)
    {
        if (combatant.isDead) return;

        ClearOrderFlags();
        holdGround = false;
        BeginMove(position);
    }  
    public void OrderDefend(Vector3 position, Quaternion facing)
    {
        if (combatant.isDead) return;

        ClearOrderFlags();
        holdGround = true;
        holdRotation = facing;
        useOrderFacing = true;
        BeginMove(position);
    }
    public void OrderHold(bool holdGroundInCombat = true)
    {
        if (combatant.isDead) return;

        ClearOrderFlags();
        holdGround = holdGroundInCombat;
        holdPoint = transform.position;
        holdRotation = transform.rotation;
        idleBehaviour = IdleBehaviour.HoldPosition;
        SetState(AIState.Hold);
    }
    public void OrderAssault(Combatant goal = null)
    {
        if (combatant.isDead) return;

        ClearOrderFlags();
        holdGround = false;
        assaulting = true;
        assaultGoal = goal;
        SetState(AIState.Assault);
    }
    public void OrderPatrol(Transform[] points)
    {
        if (combatant.isDead || points == null || points.Length == 0) return;

        ClearOrderFlags();
        patrolPoints = points;
        patrolIndex = 0;
        waiting = false;
        idleBehaviour = IdleBehaviour.Patrol;
        SetState(AIState.Patrol);
    }

    private void BeginMove(Vector3 position)
    {
        orderDestination = position;
        holdPoint = position;
        idleBehaviour = IdleBehaviour.HoldPosition;
        SetState(AIState.MoveTo);
    }
    private void ClearOrderFlags()
    {
        assaulting = false;
        assaultGoal = null;
        useOrderFacing = false;
    }
    
    
    
    
    //Testing ---- remove later
    [ContextMenu("Order: Move to test point")]
    private void TestOrderMove() {if (testOrderPoint != null) OrderMoveTo(testOrderPoint.position);}
    [ContextMenu("Order: Defend test point")]
    private void TestOrderDefend() {if (testOrderPoint != null) OrderDefend(testOrderPoint.position, testOrderPoint.rotation);}
    [ContextMenu("Order: Hold Here")]
    private void TestOrderHold() { OrderHold(); }
 
    [ContextMenu("Order: Assault")]
    private void TestOrderAssault() { OrderAssault(); }
 
    [ContextMenu("Order: Patrol Route")]
    private void TestOrderPatrol() { OrderPatrol(patrolPoints); }

    public void Shove (Vector3 direction , float distance)
    {
        if (combatant.isDead || !agent.enabled) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;

        const float duration = 0.25f;
        shoveVelocity = direction.normalized * (distance / duration);
        shoveTimeLeft = duration;
        stunTimeLeft = 0.5f;
        Stop();

    }
    private void MoveTo(Vector3 destination, float speed)
    {
        agent.isStopped = false;
        agent.speed = speed;

        if ((destination - lastDestination).sqrMagnitude > 0.25f)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                lastDestination = destination;
            }
        }
    }
    private void Stop()
    {
        agent.isStopped = true;
    }
    private bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;
    }
    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <0.001f) return;

        FaceRotation(Quaternion.LookRotation(direction));
    }
    private void FaceRotation(Quaternion rotation)
    {
        facingHandled = true;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, turnSpeed * Time.deltaTime);
    }
    private void OnDied(Combatant c)
    {
        agent.enabled = false;
        SetIndicatorColor(new Color(0.15f, 0.15f, 0.15f));

        StartCoroutine(DeathRoutine());
        enabled = false; // Stops the AI
    }
    private void OnDamaged(Combatant c)
    {
        if (combatant.isDead || state == AIState.Combat || assaulting) return;

        Combatant attacker = FindNearestHostile();
        if (attacker == null) return;

        if (state == AIState.Investigate && (attacker.transform.position - investigatePoint).sqrMagnitude < 4f) return;

        BeginInvestigate(attacker.transform.position);
    }

    private IEnumerator DeathRoutine()
    {
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.AngleAxis(90f,transform.right) * startRot;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 0.5f;

        const float duration = 0.4f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            transform.rotation = Quaternion.Slerp(startRot, endRot, k);
            transform.position = Vector3.Lerp(startPos, endPos, k);
            yield return null;
        }

        yield return new WaitForSeconds(8f);
        Destroy(gameObject);
    }
    // Debug
    private void SetIndicatorColor(Color c)
    {
        if (stateIndicator == null) return;
        if (mpb == null) mpb = new MaterialPropertyBlock();

        stateIndicator.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c);
        mpb.SetColor("_Color", c);
        stateIndicator.SetPropertyBlock(mpb);
    }
    private void OGizmosSelected()
    {
        Vector3 pos = transform.position;

        //Field of view
        Gizmos.color = Color.yellow;
        Vector3 left = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * transform.forward;
        Gizmos.DrawRay(pos, left * viewDistance);
        Gizmos.DrawRay(pos, right * viewDistance);  

        //Close sense
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(pos, closeSenseRange);
 
        // Patrol route
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] == null) continue;
 
                Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
 
                Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
                if (next != null) Gizmos.DrawLine(patrolPoints[i].position, next.position);
            }
        }
 
        // Line to the current target
        if (Application.isPlaying && target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(combatant.EyePosition, target.AimPosition);
        }
    }
}
