using UnityEngine;
using UnityEngine.InputSystem;

public class AimController : MonoBehaviour
{
    public static AimController Instance {get; private set;}
    [Header("References")]
    public Transform weaponHolder;

    [Header("Tuning")]
    public bool forceAim;
    public float AimAmount {get; private set;}
    public float SensitivityMultiplier  {get; private set;} = 1f;
    public bool IsAiming => AimAmount >0.5f;
    private Camera cam;
    private float hipFOV;
    private Vector3 holderHipPosition;
    private Weapon lastWeapon;
    private bool lastWantsAim;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        cam = Camera.main;
    }
    private void Start()
    {
        hipFOV = cam.fieldOfView;
        holderHipPosition = weaponHolder.localPosition;
    }
    private void Update()
    {
        Weapon weapon = InteractionManager.Instance != null ? InteractionManager.Instance.activeWeapon : null;
        if (weapon != lastWeapon)
        {
            if (lastWeapon != null) lastWeapon.SetAiming(false);
            AimAmount = 0f;
            lastWantsAim = false;
            lastWeapon = weapon;
        }
        bool wantsAim = weapon != null && !weapon.isReloading && (forceAim || (Mouse.current.rightButton.isPressed));
        if (weapon != null && wantsAim != lastWantsAim)
        {
            weapon.SetAiming(wantsAim);
            lastWantsAim = wantsAim;
        }

        float speed = weapon != null ? weapon.adsSpeed : 10f;
        AimAmount = Mathf.MoveTowards(AimAmount, wantsAim ? 1f : 0f, speed * Time.deltaTime);
        float eased = Mathf.SmoothStep(0f,1f,AimAmount);

        Vector3 offset = (weapon != null && !weapon.adsUsesAnimation) ? weapon.adsHolderOffset : Vector3.zero;
        float targetFOV = weapon != null ? weapon.adsFOV : hipFOV;
        float sensitivityMultiplier = weapon != null ? weapon.adsSensitivityMultiplier : 1f;

        weaponHolder.localPosition = holderHipPosition + offset * eased;
        cam.fieldOfView = Mathf.Lerp(hipFOV, targetFOV, eased);
        SensitivityMultiplier = Mathf.Lerp(1f, sensitivityMultiplier, eased);
    }

}