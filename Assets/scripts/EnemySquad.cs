using UnityEngine;
using System.Collections.Generic;

public class EnemySquad : MonoBehaviour
{
    public List<EnemyAI> members = new List<EnemyAI>();

    [Header("Order Targets")]
    public Transform[] posts; //DefendPosts(): member i defends post i
    public Transform movePoint; //MoveToPoint(): everyone runs to point and holds
    public Transform[] patrolRoute; //PatrolRoute(): everybody walks this loop
    public float moveSpread = 1.5f; 

    public int AliveCount
    {
        get
        {
            int count = 0;
            foreach (EnemyAI m in members)
                if (m != null && !m.IsDead) count++;
            return count;
        }
    }
    private void Awake()
    {
        if (members.Count == 0)
            members.AddRange(GetComponentsInChildren<EnemyAI>());
    }
    public void Charge()
    {
        foreach (EnemyAI m in Alive()) m.OrderAssault();      
    }
    public void DefendPosts()
    {
        if (posts == null || posts.Length == 0) return;

        int i = 0;
        foreach (EnemyAI m in Alive())
        {
            Transform post = posts[i % posts.Length];
            m.OrderDefend(post.position , post.rotation);
            i++;
        }
    }
    public void MoveToPoint()
    {
        if (movePoint == null) return;

        List<EnemyAI>alive = new List<EnemyAI>(Alive());
        for (int i = 0; i <alive.Count; i++)
        {
            Vector3 offset = Vector3.zero;
            if (alive.Count > 1)
            {
                float angle = i * (360 / alive.Count);
                offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * moveSpread;
            }

            alive[i].OrderMoveTo(movePoint.position + offset);
        }
    }
    public void HoldAll()
    {
        foreach (EnemyAI m in Alive()) m.OrderPatrol(patrolRoute);
    }
    public void PatrolRoute()
    {
        foreach (EnemyAI m in Alive()) m.OrderPatrol(patrolRoute);
    }
    private IEnumerable<EnemyAI> Alive()
    {
        foreach (EnemyAI m in members)
            if (m != null && !m.IsDead) yield return m;
    }
    [ContextMenu("Find Members in Children")]
    private void FindMembers()
    {
        members.Clear();
        members.AddRange(GetComponentsInChildren<EnemyAI>());
    }
}
