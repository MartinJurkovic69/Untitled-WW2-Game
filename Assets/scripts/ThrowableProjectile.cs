using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ThrowableProjectile : MonoBehaviour
{
    // Hooks for future enemy system:
    public static readonly List<ThrowableProjectile> ActiveSmokes = new List<ThrowableProjectile>();
    // Lure:
    public static event System.Action<Vector3, float> LureAlert;
    // if line from A to B goes through active smoke
    public static bool IsLineBlockedBySmoke(Vector3 from, Vector3 to)
    {
        foreach (ThrowableProjectile smoke in ActiveSmokes)
        {
            Vector3 d = to - from;
            float len2 = d.sqrMagnitude;
            float t = len2 > 0f ? Mathf.Clamp01(Vector3.Dot(smoke.smokeCenter - from, d) / len2 ): 0f;
            Vector3 closest = from + d * t;

            if ((closest - smoke.smokeCenter).sqrMagnitude <= smoke.smokeRadius * smoke.smokeRadius) return true;
        }
        return false;
    }
    private ThrowableType type;
    private ThrowableStats stats;
    private Rigidbody rb;
    private float spawnTime;
    private bool activated;

    private Vector3 smokeCenter;
    private float smokeRadius;

    public void Launch(ThrowableType throwableType, ThrowableStats throwableStats, Transform cam)
    {
        type = throwableType;
        stats = throwableStats;
        spawnTime = Time.time;

        Collider[] cols = GetComponentsInChildren<Collider>();
        if (cols.Length == 0) gameObject.AddComponent<SphereCollider>();
        else foreach (Collider c in cols) c.isTrigger = false;

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb.gameObject.AddComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError($"{name}: could not create a Rigidbody, check the prefab", this);
            Destroy(gameObject);
            return;
        }
        rb.constraints = RigidbodyConstraints.None;

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.mass = stats.mass;
        rb.linearDamping = 0f;
        rb.angularDamping = stats.verticalSpin ? 0f : 0.3f;
        rb.maxAngularVelocity = Mathf.Max(7f, stats.spinSpeed);
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.AddForce(cam.forward * stats.throwSpeed + cam.up * stats.throwUpward, ForceMode.VelocityChange);

        if (stats.verticalSpin) rb.angularVelocity = cam.right * stats.spinSpeed;
        else rb.AddTorque(Random.insideUnitSphere * stats.spinSpeed, ForceMode.VelocityChange);
    }
    private void FixedUpdate()
    {
        if (rb != null && !rb.isKinematic && !activated)
            rb.AddForce(Physics.gravity * stats.gravityScale, ForceMode.Acceleration);
    }
    private void Update()
    {
        if (activated || stats == null) return;

        if (stats.fuseTime > 0f && Time.time - spawnTime >= stats.fuseTime)
        {
            Activate(transform.position, Vector3.up);
            return;
        }
        if (Time.time - spawnTime >= stats.lifetime)
            Destroy(gameObject);
    }
    private void CollisionEnter(Collision collision)
    {
        if (activated || stats == null) return;

        ContactPoint contact = collision.GetContact(0);

        if (type == ThrowableType.ThrowingKnife)
        {
            KnifeHit(collision);
            return;
        }
        if (stats.triggerOnImpact)
            Activate(contact.point, contact.normal);
    }
    private void OnDestroy()
    {
        ActiveSmokes.Remove(this);
    }
    private void KnifeHit(Collision collision)
    {
        activated = true;

        IDamageable target = collision.collider.GetComponentInChildren<IDamageable>();
        if (target != null) target.TakeDamage(stats.damage);

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Destroy(gameObject, target != null ? 0.05f : stats.duration);
    }
    private void Activate(Vector3 point, Vector3 normal)
    {
        activated = true;
        if (rb != null){
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        transform.position = point;

        switch (type)
        {
            case ThrowableType.FragGrenade: Explode(point); break;
            case ThrowableType.SmokeGrenade: StartCoroutine(SmokeRoutine(point)); break;
            case ThrowableType.Molotov: StartCoroutine(FireRoutine(point + normal * 0.05f)); break;
            case ThrowableType.Lure: StartCoroutine(LureRoutine(point)); break;
            default: Destroy(gameObject); break;
        }
    }
    private void Explode(Vector3 center)
    {
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        foreach (Collider c in Physics.OverlapSphere(center, stats.radius))
        {
            Rigidbody body = c.attachedRigidbody;
            if (body != null && body != rb && !rb.isKinematic)
                body.AddExplosionForce(stats.explosionForce, center, stats.radius, 0.5f, ForceMode.Impulse);

            IDamageable target = c.GetComponentInParent<IDamageable>();
            if (target == null || damaged.Add(target)) continue;

            float dist = Vector3.Distance(center, c.bounds.ClosestPoint(center));
            float falloff = 1f - Mathf.Clamp01(dist / stats.radius);
            target.TakeDamage(stats.damage * falloff);
        }

        StartCoroutine(ExplosionFlash(center));
    }
    private IEnumerator ExplosionFlash(Vector3 center)
    {
        bool custom = stats.effectPrefab != null;
        GameObject visual = custom
            ? Instantiate(stats.effectPrefab, center, Quaternion.identity)
            : CreatePlaceholder(PrimitiveType.Sphere, new Color(1f,0.55f,0.1f), center, Vector3.one * 0.5f);
        float duration = custom ? 3f : 0.3f;
        float maxSize = stats.radius * 1.2f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (!custom) visual.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, maxSize, t/duration);
            yield return null;
        }
        Destroy(visual);
        Destroy(gameObject);
    }
    private IEnumerator SmokeRoutine(Vector3 center)
    {
        smokeCenter = center;
        smokeRadius = 0.5f;
        ActiveSmokes.Add(this);

        GameObject outer = null, inner = null, custom = null;
        if (stats.effectPrefab != null)
        {
            custom = Instantiate(stats.effectPrefab, center, Quaternion.identity);
        }
        else
        {
            Color grey = new Color(0.62f, 0.62f, 0.64f);
            outer = CreatePlaceholder(PrimitiveType.Sphere, grey, center, Vector3.one);
            inner = CreatePlaceholder(PrimitiveType.Sphere, grey, center, Vector3.one);
        }
        //Grow
        float growTime = 1.5f;
        for (float t = 0f; t < growTime; t += Time.deltaTime)
        {
            smokeRadius = Mathf.Lerp(0.5f,stats.radius, t / growTime);
            ApplySmokeSize(outer, inner);
            yield return null;
        }
        smokeRadius = stats.radius;
        ApplySmokeSize(outer, inner);

        yield return new WaitForSeconds(stats.duration);

        float fadeTime = 2f;
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            smokeRadius = Mathf.Lerp(stats.radius, 0f,t / fadeTime);
            ApplySmokeSize(outer, inner);
            yield return null;
        }

        ActiveSmokes.Remove(this);
        if (outer != null) Destroy(outer);
        if (inner != null) Destroy(inner);
        if (custom != null) Destroy (custom);
        Destroy(gameObject);
    }
    private void ApplySmokeSize(GameObject outer, GameObject inner)
    {
        float d = smokeRadius * 2f;
        if (outer != null) outer.transform.localScale = Vector3.one * d;
        if (inner != null) inner.transform.localScale = new Vector3(-d,d,d);
    }
    private IEnumerator FireRoutine(Vector3 center)
    {
        bool custom = stats.effectPrefab != null;
        GameObject visual = custom
            ? Instantiate(stats.effectPrefab, center, Quaternion.identity)
            : CreatePlaceholder(PrimitiveType.Sphere, new Color(1f, 0.35f, 0.05f), center, new Vector3(stats.radius * 2f, 0.2f , stats.radius * 2f));

        float end = Time.time + stats.duration;
        float nextTick = Time.time;

        while (Time.time < end)
        {
            if (Time.time >= nextTick)
            {
                nextTick = Time.time + stats.tickInterval;
                DamageInRadius(center, stats.radius, stats.damage * stats.tickInterval);
            }

            if (!custom)
            {
                float flicker = 1f + Mathf.Sin(Time.time * 18f) * 0.05f;
                visual.transform.localScale = new Vector3(stats.radius * 2f * flicker, 0.2f, stats.radius * 2f * flicker);
            }
            yield return null;
        }
        Destroy(visual);
        Destroy(gameObject);
    }
    private void DamageInRadius(Vector3 center, float radius, float amount)
    {
        HashSet<IDamageable> hit = new HashSet<IDamageable>();

        foreach (Collider c in Physics.OverlapSphere(center, radius))
        {
            IDamageable target = c.GetComponentInParent<IDamageable>();
            if (target != null && hit.Add(target)) target.TakeDamage(amount);
        }
    }
    private IEnumerator LureRoutine(Vector3 center)
    {
        bool custom = stats.effectPrefab !=null;
        GameObject visual = custom
            ? Instantiate(stats.effectPrefab, center, Quaternion.identity)
            : CreatePlaceholder(PrimitiveType.Sphere, new Color(0.2f, 0.6f, 1f), center + Vector3.up * 0.15f, Vector3.one * 0.3f);
        
        float end = Time.time + stats.duration;
        float nextPulse = Time.time;
        bool first = true;

        while (Time.time < end)
        {
            if (Time.time >= nextPulse)
            {
                nextPulse = Time.time + stats.tickInterval;
                AlertEnemies(center, stats.radius, first);
                first = false;
            }

            if (!custom)
                visual.transform.localScale = Vector3.one * (0.3f + Mathf.Abs(Mathf.Sin(Time.time * 6f))*0.15f);
            
            yield return null;
        }
        Destroy(visual);
        Destroy(gameObject);
    }
    // For future: enemies investigate the position
    private static void AlertEnemies(Vector3 position, float radius, bool log)
    {
        if (log) Debug.Log($"Lure activated at {position} (noise radius {radius})");
        LureAlert?.Invoke(position, radius);
    }
    private static GameObject CreatePlaceholder(PrimitiveType shape, Color color, Vector3 position, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(shape);

        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        go.transform.position = position;
        go.transform.localScale = scale;

        Renderer r = go.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Material m = r.material;
        if (m.HasProperty("_BaseColor")) m.SetColor ("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);

        return go;
    }
}
