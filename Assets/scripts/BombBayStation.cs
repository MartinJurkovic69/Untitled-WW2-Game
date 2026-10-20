using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BombBayStation : CrewStation
{
    private enum Phase {Aligning, Done}
    [Header("Target")]
    public Transform target;

    [Header("Alignment")]
    public float alignAngle = 1.5f;
    public float timeToLock = 2.5f;
    public float lockDecay = 1.5f;
    public bool requireReleaseInput = true;

    [Header("Time")]
    public float timeLimit = 40f;
    public float finishDelay = 2f;

    [Header("Disturbances")]
    public float turbulence = 1.2f;
    public float flakKick = 2.5f;
    public Vector2 flakInterval = new Vector2(2f, 4.5f);

    [Header("Bombs (just visual)")]
    public GameObject bombPrefab;
    public Transform[] bombSlots;

    [Header("HUD root")]
    public Image Reticle;
    public Color reticleColor = Color.white;
    public Color alignedColor = new Color (0.3f, 1f, 0.3f);
    public Image lockFill;
    public Image reticle;
    public TextMeshProUGUI timerText;

    [Header("Events")]
    public UnityEvent onFlak;
    public UnityEvent onLocked;
    public UnityEvent onBombsAway;
    public UnityEvent onFailed;

    public bool Success{get;private set;}

    private Phase phase;
    private float timeLeft;
    private float progress;
    private bool locked;
    private float flakTimer;
    private float pendingPitch;
    private float pendingYaw;
    private float noiseSeed;
    private float finishTimer;
    private int shownSeconds = -1;

    protected override void OnBegin()
    {
        phase = Phase.Aligning;
        timeLeft = timeLimit;
        progress = 0f;
        locked = false;
        Success = false;
        finishTimer = 0f;
        pendingPitch = pendingYaw = 0f;
        noiseSeed = Random.value * 100f;
        flakTimer = Random.Range(flakInterval.x, flakInterval.y);
        shownSeconds = -1;

        UpdateHud(false);
    }
    protected override void OnStationUpdate()
    {
        float dt = Time.deltaTime;

        if (phase == Phase.Done)
        {
            finishTimer += dt;
            if (finishTimer >= finishDelay) Complete();
            return;
        }
        timeLeft -= dt;
        ApplyDisturbances(dt);

        bool alligned = IsAlligned();

        if (!locked)
        {
            if (alligned) progress = Mathf.MoveTowards(progress, 1f , dt / timeToLock);
            else progress = Mathf.MoveTowards(progress , 0f, dt / timeToLock * lockDecay);

            if (progress >= 1f)
            {
                locked = true;
                onLocked.Invoke();
            }
        }
        if (locked && (!requireReleaseInput || ReleasePressed()))
        {
            DropBombs();
            UpdateHud(alligned);
            return;
        }
        if (timeLeft <= 0f)
        {
            Success = false;
            phase = Phase.Done;
            onFailed.Invoke();
        }
        UpdateHud(alligned);
    }
    private bool IsAlligned()
    {
        if (target == null || stationCamera == null) return false;

        Transform cam = stationCamera.transform;
        return Vector3.Angle(cam.forward, target.position - cam.position) <= alignAngle;
    }
    private bool ReleasePressed()
    {
        bool space = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return space || click;
    }
    public void ApplyDisturbances(float dt)
    {
        float t = Time.time * 0.7f;
        float nPitch = Mathf.PerlinNoise(noiseSeed, t) - 0.5f;
        float nYaw = Mathf.PerlinNoise(noiseSeed + 50f , t) -0.5f;
        AddRecoil(nPitch * 2f * turbulence * dt, nYaw * 2f * turbulence * dt);

        flakTimer -= dt;
        if (flakTimer <= 0f)
        {
            flakTimer = Random.Range(flakInterval.x, flakInterval.y);
            pendingPitch += RandomKick();
            pendingYaw += RandomKick();
            onFlak.Invoke();
        }
        float k = 1f - Mathf.Exp(-18f * dt);
        float p = pendingPitch * k;
        float y = pendingYaw * k;
        pendingPitch -= p;
        pendingYaw -= y;
        AddRecoil(p,y);
    }
    private float RandomKick()
    {
        return (Random.value < 0.5f ? -1f : 1f) * Random.Range(0.5f, 1f) * flakKick;
    }
    private void DropBombs()
    {
        Success = true;
        phase = Phase.Done;

        if (bombPrefab != null && bombSlots != null)
        {
            foreach (Transform slot in bombSlots)
            {
                if (slot == null) continue;

                GameObject bomb = Instantiate(bombPrefab, slot.position, slot.rotation);
                if (bomb.GetComponent<Rigidbody>() == null) bomb.AddComponent<Rigidbody>();
                Destroy(bomb, 30f);
            }
        }
        onBombsAway.Invoke();
    }
    private void UpdateHud(bool aligned)
    {
        if (reticle != null) reticle.color = (aligned || locked) ? alignedColor : reticleColor;
        if (lockFill != null) lockFill.fillAmount = locked ? 1f : progress;

        if (timerText != null)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(timeLeft));
            if (seconds != shownSeconds)
            {
                shownSeconds = seconds;
                timerText.text = seconds.ToString();
            }
        }
    }
}