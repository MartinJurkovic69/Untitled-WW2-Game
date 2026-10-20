using System.Collections;
using UnityEditor.MPE;
using UnityEngine;
using UnityEngine.InputSystem;

public class GunnerStation : CrewStation
{
    [Header("Gun")]
    public Transform muzzle;
    public float damage = 35f;
    public float roundsPerMinute = 750f;
    public float spreadDegrees = 0.35f;
    public float range = 1500f;
    public float recoilPitch = 0.25f;
    public float recoilYaw = 0.15f;
    public LayerMask hitMask = ~0;
    public Transform ignoreRoot;

    [Header("Ammo")]
    public int beltSize = 250;
    public int reserveBelts = 3;
    public float beltChangeTime = 4f;

    [Header("Objective")]
    public int killsRequired = 0;

    [Header("Audio")]
    public WeaponAudioData audioData;
    public AudioSource audioSource;

    [Header("Effects")]
    public GameObject tracerPrefab;
    public float tracerEvery = 4f;
    public float tracerTime = 0.08f;
    public GameObject muzzleFlash;

    [Header("HUD")]
    public string displayName = "M2 Browning";
    public Sprite weaponIcon;

    public int Kills {get;private set;}

    public int roundsInBelt;
    private bool changingBelt;
    private float nextFireTime;
    private int shotCounter;

    private int shownRounds = -1;
    private int shownReserve = -1;
    private bool shownChanging;

    protected override void Awake()
    {
        base.Awake();
        roundsInBelt = beltSize;
        if (muzzleFlash != null) muzzleFlash.SetActive(false);
    }
    protected override void OnBegin()
    {
        Kills = 0;
        changingBelt = false;
        shownRounds = -1;

        ShowGunInfo();
        UpdateHud();
    }
    protected override void OnEnd()
    {
        StopAllCoroutines();
        changingBelt = false;
        if (muzzleFlash != null) muzzleFlash.SetActive(false);

        AmmoManager ui = AmmoManager.Instance;
        if (ui != null)
        {
            if (ui.reloadPrompt != null) ui.reloadPrompt.gameObject.SetActive(false);
            if (ui.ammoWarning != null) ui.ammoWarning.gameObject.SetActive(false);
        }
    }
    protected override void OnStationUpdate()
    {
        Keyboard k = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (k != null && k.rKey.wasPressedThisFrame && roundsInBelt < beltSize) StartBeltChange();
        if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame && roundsInBelt <= 0 && !changingBelt && audioData != null)
                PlayClip(audioData.emptyClickClip, audioData.emptyClickVolume);
            if (mouse.leftButton.isPressed && !changingBelt && roundsInBelt > 0 && Time.time >= nextFireTime)
                Fire();
        }
        UpdateHud();
    }
    private void StartBeltChange()
    {
        if (changingBelt || reserveBelts <= 0) return;
        StartCoroutine(BeltChange());
    }
    private IEnumerator BeltChange()
    {
        changingBelt = true;
        if (audioData != null) PlayClip(audioData.reloadClip, audioData.reloadVolume);

        yield return new WaitForSeconds(beltChangeTime);

        reserveBelts--;
        roundsInBelt = beltSize;
        changingBelt = false;
    }
    private void Fire()
    {
        nextFireTime = Time.time + 60f / roundsPerMinute;
        roundsInBelt--;
        shotCounter++;

        Transform cam = stationCamera.transform;
        Vector3 dir = Vector3.RotateTowards(cam.forward, Random.onUnitSphere, spreadDegrees * Mathf.Deg2Rad * Random.value, 0f);

        Vector3 origin = cam.position;
        Vector3 end = origin + dir * range;

        RaycastHit[] hits = Physics.RaycastAll(origin,dir,range,hitMask,QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a , b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (ignoreRoot != null && hit.collider.transform.IsChildOf(ignoreRoot)) continue;

            end = hit.point;

            Combatant combatant = hit.collider.GetComponentInParent<Combatant>();
            if (combatant != null && combatant.team == Team.Player) break;

            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                bool wasAlive = combatant == null || !combatant.isDead;
                target.TakeDamage(damage);

                if (combatant != null && wasAlive && combatant.isDead) RegisterKill();
            }
            break;
        }
        if (tracerPrefab != null && tracerEvery > 0 && shotCounter % tracerEvery == 0)
        {
            Vector3 from = muzzle != null ? muzzle.position : origin + cam.forward * 0.5f;
            StartCoroutine(Tracer(from, end));
        }
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(true);
            CancelInvoke(nameof(HideFlash));
            Invoke(nameof(HideFlash), 0.04f);
        }
        PlayFireSound();
        AddRecoil(recoilPitch, Random.Range(-recoilYaw, recoilYaw));
    }
    private void RegisterKill()
    {
        Kills++;
        if (killsRequired > 0 && Kills >= killsRequired) Complete();
    }
    private void HideFlash()
    {
        if (muzzleFlash != null) muzzleFlash.SetActive(false);
    }
    private IEnumerator Tracer(Vector3 from, Vector3 to)
    {
        GameObject tracer = Instantiate(tracerPrefab, from, Quaternion.identity);
        Destroy(tracer, 1f);

        Vector3 direction = to - from;
        if (direction.sqrMagnitude > 0.001f) tracer.transform.rotation = Quaternion.LookRotation(direction);
        for (float t = 0f; t < tracerTime; t += Time.deltaTime)
        {
            if (tracer == null) yield break;
            tracer.transform.position = Vector3.Lerp(from, to, t / tracerTime);
            yield return null;
        }
        if (tracer != null) Destroy(tracer);
    }
    private void PlayFireSound()
    {
        if (audioSource == null || audioData == null) return;

        AudioClip clip = audioData.GetRandomFireClip();
        if (clip == null) return;

        audioSource.pitch = 1f + Random.Range(-audioData.pitchRandomness, audioData.pitchRandomness);
        audioSource.PlayOneShot(clip, audioData.fireVolume);
    }
    private void PlayClip(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(clip, volume);
    }
    private void ShowGunInfo()
    {
        AmmoManager ui = AmmoManager.Instance;
        if (ui == null) return;

        if (ui.weaponIcon != null)
        {
            ui.weaponIcon.sprite = weaponIcon;
            ui.weaponIcon.gameObject.SetActive(weaponIcon != null);
        }
        if (ui.weaponNameText != null) ui.weaponNameText.text = displayName;
    }

    private void UpdateHud()
    {
        AmmoManager ui = AmmoManager.Instance;
        if (ui == null) return;

        int reserveRounds = roundsInBelt;
        shownReserve = reserveRounds;
        shownChanging = changingBelt;

        if (ui.ammoDisplay != null) ui.ammoDisplay.text = roundsInBelt.ToString();
        if (ui.reserveDisplay != null) ui.reserveDisplay.text = reserveRounds.ToString();

        if (ui.reloadPrompt != null)
            ui.reloadPrompt.gameObject.SetActive(roundsInBelt <= 0 && reserveBelts > 0 && !changingBelt);

        if (ui.ammoWarning != null)
        {
            if (roundsInBelt <= 0 && reserveBelts <= 0)
            {
                ui.ammoWarning.text = "No Ammo";
                ui.ammoWarning.color = Color.red;
                ui.ammoWarning.gameObject.SetActive(true);
            }
            else if (reserveBelts <= 1)
            {
                ui.ammoWarning.text = "Low Ammo";
                ui.ammoWarning.color = Color.yellow;
                ui.ammoWarning.gameObject.SetActive(true);
            }
            else
            {
                ui.ammoWarning.gameObject.SetActive(false);
            }
        }
    }
}