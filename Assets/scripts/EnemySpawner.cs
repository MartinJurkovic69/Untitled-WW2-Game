using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning")]
    public GameObject enemyPrefab;
    [Tooltip("Empty objects placed on the NavMesh. If empty, enemies spawn at this object's position.")]
    public Transform[] spawnPoints;
    public float interval = 30f;
    public bool spawnImmediately = true;     // first enemy right at the start instead of after one interval
    [Tooltip("Spawning pauses while this many are alive (0 = no limit)")]
    public int maxAlive = 10;
    public bool spawning = true;
 
    [Header("Rules")]
    [Tooltip("A spawn point is skipped if the player is closer than this")]
    public float minPlayerDistance = 10f;
    public float retryDelay = 2f;            // wait this long before trying again when no spawn point is usable
    [Tooltip("Spawned enemies charge at the player straight away (otherwise they hold the spawn point)")]
    public bool chargeOnSpawn = true;
 
    [Header("Debug")]
    [SerializeField] private float nextSpawnIn;
 
    private readonly List<Combatant> alive = new List<Combatant>();
    private int nextPointIndex;
    private bool warnedNoNavMesh;

    private void Start()
    {
        Debug.Log($"{name}: spawner started | prefab: {(enemyPrefab != null ? enemyPrefab.name : "NONE")} | points: {(spawnPoints != null ? spawnPoints.Length : 0)}", this);
        nextSpawnIn = spawnImmediately ? 0f : interval;
    }
    private void Update()
    {
        if (!spawning || enemyPrefab == null) return;
        nextSpawnIn -= Time.deltaTime;
        if (nextSpawnIn > 0f) return;

        nextSpawnIn = TrySpawn() ? interval : retryDelay;
    }
    private bool TrySpawn()
    {
        alive.RemoveAll(c => c == null || c.isDead);
        if (maxAlive > 0 && alive.Count >= maxAlive) return true;

        int count = (spawnPoints != null && spawnPoints.Length > 0) ? spawnPoints.Length : 1;

        for (int i = 0; i < count; i++)
        {
            int index = (nextPointIndex + i) % count;
            Transform point = (spawnPoints != null && spawnPoints.Length > 0) ? spawnPoints[index] : transform;

            if (!IsUsable(point.position, out Vector3 navPosition)) continue;
            nextPointIndex = (index + 1) % count;
            Spawn(navPosition, point.rotation);
            return true;
        }
        return false;
    }
    private bool IsUsable(Vector3 position, out Vector3 navPosition)
    {
        navPosition = position;

        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            if (!warnedNoNavMesh)
            {
                Debug.LogWarning($"{name}: a spawn point is not on the NavMesh. Move it onto the baked area.", this);
                warnedNoNavMesh = true;
            }
            return false;
        }
        navPosition = hit.position;
        
        foreach (Combatant c in Combatant.All)
        {
            if (c.team != Team.Player || c.isDead) continue;
            if (Vector3.Distance(c.transform.position, navPosition) < minPlayerDistance)
                {
                    Debug.Log($"{name}: skipped a spawn point because the player is too close", this);
                    return false;
                }
        }
        return true;
    }
    private void Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject enemy = Instantiate(enemyPrefab, position, rotation);

        Combatant combatant = enemy.GetComponent<Combatant>();
        if (combatant != null) alive.Add(combatant);

        if (chargeOnSpawn)
        {
            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai != null) StartCoroutine(OrderAssaultNextFrame(ai));
        }
    }
    private IEnumerator OrderAssaultNextFrame(EnemyAI ai)
    {
        yield return null;
        if (ai != null) ai.OrderAssault();
    }
    public void StartSpawning() { spawning = true;}
    public void StopSpawning() { spawning = false;}

    [ContextMenu("Spawn Now")]
    private void SpawnNow()
    {
        if (enemyPrefab != null) TrySpawn();
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Gizmos.DrawWireSphere(transform.position , 0.5f);
            return;
        }
        foreach (Transform p in spawnPoints)
        {
            if ( p == null) continue;
            Gizmos.DrawWireSphere(p.position , 0.5f);
            Gizmos.DrawRay(p.position , p.forward * 1.5f);
        }
    }
}
