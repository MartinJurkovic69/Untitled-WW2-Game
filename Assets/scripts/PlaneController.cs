using UnityEngine;

public class PlaneController : MonoBehaviour
{
    [Header("Speed")]
    public float minSpeed = 45f;
    public float maxSpeed = 90f;
    public float acceleration = 5f;
    [Range (0f,1f)] public float startThrottle = 0.6f;

    [Header("Handling")]
    public float pitchRate = 10f;
    public float rollRate = 18f;
    public float yawRate = 5f;
    public float autoLevel = 6f;

    [Header("Story modifiers")]
    [Range (0f,1f)] public float controlAuthority = 1f;
    [Range (0f,1f)] public float thrustAuthority = 1f;
    public float turbulence = 1f;

    public float Throttle {get; private set;}
    public float Speed {get;private set;}
    public Vector3 Velocity => transform.forward * Speed;
    public float Altitude => transform.position.y;

    private float pitchInput;
    private float rollInput;
    private float yawInput;
    private float noiseSeed;

    private void Awake()
    {
        Throttle = startThrottle;
        Speed = Mathf.Lerp(minSpeed, maxSpeed * thrustAuthority, Throttle);
        noiseSeed = Random.value * 100f;
    }
    public void SetInput(float pitch, float roll, float yaw)
    {
        pitchInput = Mathf.Clamp(pitch, -1f , 1f);
        rollInput = Mathf.Clamp(roll, -1f, 1f);
        yawInput = Mathf.Clamp(yaw, -1f, 1f);
    }
    public void ChangeThrottle(float delta)
    {
        Throttle = Mathf.Clamp01(Throttle + delta);
    }
    public void SetControlAuthority(float value) {controlAuthority = Mathf.Clamp01(value);}
    public void SetThrustAuthority(float value) {thrustAuthority = Mathf.Clamp01(value);}
    public void SetTurbulence(float value) {turbulence = Mathf.Max(0f, value);}
    private void Update()
    {
        if (PauseMenu.IsPaused) return;

        float dt = Time.deltaTime;

        float targetSpeed = Mathf.Lerp(minSpeed,maxSpeed *thrustAuthority, Throttle);
        Speed = Mathf.MoveTowards(Speed, targetSpeed, acceleration * dt);

        transform.Rotate(
            pitchInput * pitchRate * controlAuthority * dt,
            yawInput * yawRate * controlAuthority * dt,
            -rollInput * rollRate * controlAuthority * dt,
            Space.Self);
        if (autoLevel > 0f && Mathf.Abs(rollInput) < 0.05f)
        {
            Quaternion level = Quaternion.LookRotation(transform.forward, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, level, autoLevel * dt);
        }

        if (turbulence > 0f)
        {
            float t = Time.time * 0.6f;
            float nx = Mathf.PerlinNoise(noiseSeed, t) - 0.5f;
            float ny = Mathf.PerlinNoise(noiseSeed + 10f , t) - 0.5f;
            float nz = Mathf.PerlinNoise(noiseSeed + 20f , t) - 0.5f;

            transform.Rotate(nx * turbulence * dt * 2f, ny * turbulence * dt, nz * turbulence * dt * 2f, Space.Self);
        }
        transform.position += transform.forward * Speed * dt;
    }
}