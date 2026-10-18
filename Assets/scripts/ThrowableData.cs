using UnityEngine;

public enum ThrowableType
{
    ThrowingKnife,
    FragGrenade,
    SmokeGrenade,
    Molotov,
    Lure
}
public interface IDamageable
{
    void TakeDamage(float amount);
}

[System.Serializable]
public class ThrowableStats
{
    [HideInInspector] public bool isFilled;
    [Header("Throw")]
    public float throwSpeed = 12f; // M/s 
    public float throwUpward = 2f; // arc
    [Range(0f,1f)] public float gravityScale = 1f;
    public float spinSpeed = 8f; // rad/s
    public bool verticalSpin; 
    public float mass = 0.5f;
    public float lifetime = 10f;
    [Header("Trigger")]
    public float fuseTime = 0f;
    public bool triggerOnImpact;

    [Header("Effect")]
    public float damage = 0f; //frag - centre, knife - instant, molly - per second
    public float radius = 0f; //frag - blast, smoke - cloud, molly - fire area, lure - sound area
    public float duration = 0f;//smoke, fire
    public float explosionForce = 0f;
    public float tickInterval = 0.5f; //fire damage tick, lure pulse
    public GameObject effectPrefab;

    public static ThrowableStats DefaultFor(ThrowableType type)
    {
        ThrowableStats s = new ThrowableStats {isFilled = true};

        switch (type)
        {
            case ThrowableType.ThrowingKnife:
                s.throwSpeed = 38f; s.throwUpward = 0.3f; s.gravityScale = 0.15f;
                s.spinSpeed = 25f; s.verticalSpin = true; s.mass = 0.2f;    
                s.triggerOnImpact = true;
                s.damage = 90f; s.duration = 6f; s.lifetime = 8f;
                break;
            case ThrowableType.FragGrenade:
                s.throwSpeed = 15f; s.throwUpward = 3f; s.spinSpeed = 6f; s.mass = 0.6f;
                s.fuseTime = 3.5f;
                s.damage = 150f; s.radius = 8f; s.explosionForce = 15f;
                s.lifetime = 10f;
                break;
            case ThrowableType.SmokeGrenade:
                s.throwSpeed = 14f; s.throwUpward = 3f; s.spinSpeed = 5f; s.mass = 0.5f;
                s.fuseTime = 2f;
                s.radius = 6f; s.duration = 20f; s.lifetime = 10f;
                break;
            case ThrowableType.Molotov:
                s.throwSpeed = 14f; s.throwUpward = 3f; s.spinSpeed = 5f; s.mass = 0.5f;
                s.triggerOnImpact = true;
                s.damage = 20f; s.radius = 3.5f; s.duration = 7f; s.tickInterval = 0.5f;
                s.lifetime = 8f;
                break;
            case ThrowableType.Lure:
                s.throwSpeed = 13f; s.throwUpward = 3f; s.spinSpeed = 5f; s.mass = 0.4f;
                s.triggerOnImpact = true;
                s.radius = 30f; s.duration = 10f; s.tickInterval = 1f;
                s.lifetime = 8f;
                break;
        }
        return s;
    }
}