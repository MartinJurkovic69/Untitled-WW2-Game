using TMPro;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public bool isActiveWeapon = false; // Is this the currently equipped weapon?
    // Shooting
    public bool isShooting, readyToShoot = true;
    public float shootingDelay = 0.1f;      // delay between shots (semi/auto) or between whole bursts

    // Burst
    public int bulletsPerBurst = 3;
    public float burstBulletInterval = 0.08f; // delay between bullets INSIDE a burst
    public bool burstRequiresNewClick = true; // false = holding the button keeps firing bursts
    int burstBulletsLeft;
    bool isBursting;

    // Spread
    public float spreadIntensity;

    // Bullet
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 20f;
    public float bulletLifetime = 3f;

    // Loading
    public float reloadTime;
    public int magazineSize, bulletsLeft;
    public int reserveAmmo;
    public int maxReserveAmmo;
    public bool isReloading;
    public bool IsReserveFull => reserveAmmo >= maxReserveAmmo;

    public Vector3 heldLocalPosition = Vector3.zero;
    public Vector3 heldLocalEuler = Vector3.zero;

    public GameObject muzzleEffect;
    ParticleSystem muzzleParticles;

    public enum WeaponSlot
    {
        Primary,
        Secondary
    }
    public string displayName;
    [Header("UI")]
    public Sprite weaponIcon;
    public Sprite ammoTypeIcon;

    [Header("Inventory")]
    public WeaponSlot weaponSlot;

    [Header("AimingDownSights")]
    public Vector3 adsHolderOffset;
    public float adsFOV = 40f;
    public float adsSpeed = 5f;
    [Range(0f, 1f)] public float adsSpreadMultiplier = 0.5f;
    [Range(0f, 1f)] public float adsSensitivityMultiplier = 0.5f;
    public bool adsUsesAnimation;

    public enum ShootingMode
    {
        SemiAutomatic,
        Automatic,
        Burst
    }

    [Header("Audio Data")]
    public WeaponAudioData weaponAudio;
    public ShootingMode currentShootingMode;

    private Animator animator;
    private Camera cam;

    private void Awake()
    {
        readyToShoot = true;
        animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = isActiveWeapon;
        cam = Camera.main;
        if (muzzleEffect != null) muzzleParticles = muzzleEffect.GetComponent<ParticleSystem>();

        bulletsLeft = magazineSize;
    }

    private void OnDisable()
    {
        CancelInvoke();
        isReloading = false;
        isBursting = false;
        readyToShoot = true;
        if (AmmoManager.Instance != null && AmmoManager.Instance.reloadPrompt != null)
        {
            AmmoManager.Instance.reloadPrompt.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!isActiveWeapon) return;
        if (ThrowablesManager.Instance != null && ThrowablesManager.Instance.isWheelOpen) return;
        if (Mouse.current == null) return;

        isShooting = Mouse.current.leftButton.isPressed;

        bool wantsToShoot = false;
        switch (currentShootingMode)
        {
            case ShootingMode.SemiAutomatic:
                wantsToShoot = Mouse.current.leftButton.wasPressedThisFrame;
                break;
            case ShootingMode.Automatic:
                wantsToShoot = isShooting;
                break;
            case ShootingMode.Burst:
                wantsToShoot = burstRequiresNewClick
                    ? Mouse.current.leftButton.wasPressedThisFrame
                    : isShooting;
                break;
        }

        if (wantsToShoot && readyToShoot && !isReloading && bulletsLeft > 0)
        {
            if (currentShootingMode == ShootingMode.Burst)
            {
                isBursting = true;
                burstBulletsLeft = bulletsPerBurst;
            }
            FireWeapon();
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame && !isReloading && !isBursting && bulletsLeft <= 0)
        {
            SoundManager.Instance.PlayClip(weaponAudio.emptyClickClip, bulletSpawnPoint.position, weaponAudio.emptyClickVolume);
        }
        // Reload
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame
            && bulletsLeft < magazineSize && !isReloading && !isBursting)
        {
            Reload();
        }
        UpdateAmmoDisplay();
        UpdateReloadPrompt();
        UpdateAmmoWarning();
    }

    private void FireWeapon()
    {

        if (bulletsLeft <= 0 || isReloading)
        {
            EndShotCycle();
            return;
        }

        readyToShoot = false;
        bulletsLeft--;

        if (muzzleParticles != null) muzzleParticles.Play();
        SoundManager.Instance.PlayWeaponShot(weaponAudio, bulletSpawnPoint.position);
        bool isFirstShotOfBurst = currentShootingMode != ShootingMode.Burst || burstBulletsLeft == bulletsPerBurst;
        if (isFirstShotOfBurst)
        {
            animator.SetTrigger("RECOIL");
        }

        Vector3 shootingDirection = CalculateShootingDirectionAndSpread().normalized;

        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        bullet.transform.forward = shootingDirection;

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.AddForce(shootingDirection * bulletSpeed, ForceMode.Impulse);

        Destroy(bullet, bulletLifetime);

        if (currentShootingMode == ShootingMode.Burst && isBursting)
        {
            burstBulletsLeft--;

            if (burstBulletsLeft > 0 && bulletsLeft > 0)
            {
                Invoke(nameof(FireWeapon), burstBulletInterval);
                return;
            }

            isBursting = false;
        }

        Invoke(nameof(ResetShot), shootingDelay);
    }

    private void EndShotCycle()
    {
        isBursting = false;
        burstBulletsLeft = 0;
        readyToShoot = true;
    }

    private void Reload()
    {
        if (reserveAmmo <= 0 || bulletsLeft == magazineSize) return;
        isReloading = true;
        SoundManager.Instance.PlayClip(weaponAudio.reloadClip, bulletSpawnPoint.position, weaponAudio.reloadVolume);
        Invoke(nameof(ReloadComplete), reloadTime);
    }
    private void UpdateReloadPrompt()
    {
        if (AmmoManager.Instance == null ||AmmoManager.Instance.reloadPrompt == null) return;

        bool show = bulletsLeft <= 0 && reserveAmmo > 0 && !isReloading;
        AmmoManager.Instance.reloadPrompt.gameObject.SetActive(show);
    }
    private void ReloadComplete()
    {
        int needed = magazineSize - bulletsLeft;
        int toReload = Mathf.Min(needed, reserveAmmo);
        bulletsLeft += toReload;
        reserveAmmo -= toReload;
        isReloading = false;
    }
    private void UpdateAmmoDisplay()
    {
        if (AmmoManager.Instance == null || AmmoManager.Instance.ammoDisplay == null) return;

        int loaded = bulletsLeft;
        int reserve = reserveAmmo;

        if (currentShootingMode == ShootingMode.Burst)
        {
            loaded = Mathf.CeilToInt((float)bulletsLeft / bulletsPerBurst);
            reserve = Mathf.CeilToInt((float)reserveAmmo / bulletsPerBurst);
        }
        AmmoManager.Instance.ammoDisplay.text = loaded.ToString();
        if (AmmoManager.Instance.reserveDisplay != null)
            AmmoManager.Instance.reserveDisplay.text = reserve.ToString();
    }
    private void UpdateAmmoWarning()
    {
        if (AmmoManager.Instance == null || AmmoManager.Instance.ammoWarning == null) return;

        TextMeshProUGUI warning = AmmoManager.Instance.ammoWarning;

        if (bulletsLeft <= 0 && reserveAmmo <= 0)
        {
            warning.text = "No ammo";
            warning.color = Color.red;
            warning.gameObject.SetActive(true);
        }
        else if (reserveAmmo <= magazineSize)
        {
            warning.text = "Low ammo";
            warning.color = Color.yellow;
            warning.gameObject.SetActive(true);
        }
        else
        {
            warning.gameObject.SetActive(false);
        }
    }
    private void ResetShot()
    {
        readyToShoot = true;
    }

    public Vector3 CalculateShootingDirectionAndSpread()
    {
        
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000); 
        }

        Vector3 direction = (targetPoint - bulletSpawnPoint.position).normalized;

        float aim = AimController.Instance != null ? AimController.Instance.AimAmount : 0f;
        float spread = spreadIntensity * Mathf.Lerp(1f, adsSpreadMultiplier, aim);

        float spreadX = Random.Range(-spread, spread);
        float spreadY = Random.Range(-spread, spread);

        Vector3 spreadVector = cam.transform.right * spreadX + cam.transform.up * spreadY;

        return direction + spreadVector;
    }
    public void SetEquipped(bool equipped)
    {
        isActiveWeapon = equipped;
        if (equipped)
        {
            transform.localPosition = heldLocalPosition;
            transform.localRotation = Quaternion.Euler(heldLocalEuler);
        }
        if (animator != null)
        {
            animator.enabled = true;
            animator.Rebind();
            animator.Update(0f);
            if (adsUsesAnimation) animator.SetBool("ADS" , false);
        }
        else
        {
            if  (animator != null) animator.enabled = false;

            CancelInvoke();
            isReloading = false;
            isBursting = false;
            readyToShoot = true;
            if (AmmoManager.Instance != null && AmmoManager.Instance.reloadPrompt != null)
            {
                AmmoManager.Instance.reloadPrompt.gameObject.SetActive(false);
            }
            if (AmmoManager.Instance.ammoWarning != null)
            {
                AmmoManager.Instance.ammoWarning.gameObject.SetActive(false);
            }
        }
    }
    public void RestockReserve()
    {
        reserveAmmo = maxReserveAmmo;
    }   
    public void SetAiming(bool aiming)
    {
        if (animator != null && animator.isActiveAndEnabled && adsUsesAnimation)
        {
            animator.SetBool("ADS", aiming);
        }
    }
}   
