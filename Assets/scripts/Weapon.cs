using TMPro;
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
    public bool isReloading;

    public Vector3 heldLocalPosition = Vector3.zero;
    public Vector3 heldLocalEuler = Vector3.zero;

    public GameObject muzzleEffect;
    ParticleSystem muzzleParticles;

    public enum WeaponSlot
    {
        Primary,
        Secondary
    }

    [Header("Inventory")]
    public WeaponSlot weaponSlot;

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
    }

    private void UpdateAmmoDisplay()
    {
        if (AmmoManager.Instance == null || AmmoManager.Instance.ammoDisplay == null) return;

        if (currentShootingMode == ShootingMode.Burst)
        {
            // Show remaining / max bursts (rounded up so a partial burst still counts)
            int bursts = Mathf.CeilToInt((float)bulletsLeft / bulletsPerBurst);
            int maxBursts = Mathf.CeilToInt((float)magazineSize / bulletsPerBurst);
            AmmoManager.Instance.ammoDisplay.text = $"{bursts}/{maxBursts}";
        }
        else
        {
            AmmoManager.Instance.ammoDisplay.text = $"{bulletsLeft}/{magazineSize}";
        }
    }

    private void FireWeapon()
    {
        // Safety net: burst continuation or reload could have changed things since it was scheduled
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
                // More bullets in this burst
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
        isReloading = true;
        SoundManager.Instance.PlayClip(weaponAudio.reloadClip, bulletSpawnPoint.position, weaponAudio.reloadVolume);
        animator.SetTrigger("RELOAD");
        Invoke(nameof(ReloadComplete), reloadTime);
    }
    private void UpdateReloadPrompt()
    {
        if (AmmoManager.Instance == null ||AmmoManager.Instance.reloadPrompt == null) return;

        bool show = bulletsLeft <= 0 && !isReloading;
        AmmoManager.Instance.reloadPrompt.gameObject.SetActive(show);
    }
    private void ReloadComplete()
    {
        bulletsLeft = magazineSize;
        isReloading = false;
    }

    private void ResetShot()
    {
        readyToShoot = true;
    }

    public Vector3 CalculateShootingDirectionAndSpread()
    {
        // Shooting from middle of screen (crosshair)
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000); // arbitrary far point if nothing is hit
        }

        Vector3 direction = (targetPoint - bulletSpawnPoint.position).normalized;

        float spreadX = Random.Range(-spreadIntensity, spreadIntensity);
        float spreadY = Random.Range(-spreadIntensity, spreadIntensity);

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
        }
    }
        
}   
