using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.VisualScripting;
public interface IShovable
{
    void Shove(Vector3 direction, float distance);
}
public class MeleeAttack : MonoBehaviour
{
    public static MeleeAttack Instance { get ; private set;}

    [Header("Attack")]
    public float damage = 60f;
    public float range = 2f;
    public float radius = 0.5f;
    public float windup = 0.15f;
    public float recovery = 0.35f;

    [Header("Shove")]
    public float shoveDistance = 2f;
    public float physicsPush = 6f;

    public bool IsBusy { get; private set;}

    private Camera cam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cam = Camera.main;
    }
    private void Update()
    {
        if (PauseMenu.IsPaused) return;
        if (IsBusy || Keyboard.current == null) return;
        if (!Keyboard.current.vKey.wasPressedThisFrame) return;
        if (ThrowablesManager.Instance != null && ThrowablesManager.Instance.isWheelOpen) return;

        StartCoroutine(MeleeRoutine());
    }
    private IEnumerator MeleeRoutine()
    {
        IsBusy = true;

        Weapon weapon = InteractionManager.Instance != null ? InteractionManager.Instance.activeWeapon : null;
        if (weapon != null) weapon.PlayMeleeAnimation();

        yield return new WaitForSeconds(windup);
        DoHit();

        yield return new WaitForSeconds(recovery);
        IsBusy = false;
    }
    private void DoHit()
    {
        Vector3 origin = cam.transform.position;
        Vector3 forward = cam.transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, forward, range, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Collider col = hit.collider;

            Combatant combatant = col.GetComponentInParent<Combatant>();
            if (combatant != null && combatant.team == Team.Player) continue;

            IDamageable target = col.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);

                IShovable shovable = col.GetComponentInParent<IShovable>();
                if (shovable != null)
                {
                    Vector3 flat = new Vector3(forward.x ,0f , forward.z);
                    shovable.Shove(flat, shoveDistance);
                }
                return;
            }
            Rigidbody body = col.attachedRigidbody;
            if (body != null && body.isKinematic)
                body.AddForce(forward * physicsPush, ForceMode.Impulse);
            
            return;
        }
    }
}
