using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    //Shooting
    public bool isShooting, readyToShoot = true;
    bool allowReset = true;
    public float shootingDelay = 1f;
    //Spread
    public float spreadIntensity;
    //Bullet
    public GameObject bulletPrefab;
    public Transform bulletSpawnPoint;
    public float bulletSpeed = 20f;
    public float bulletLifetime = 3f;

    public GameObject muzzleEffect;

    public enum ShootingMode
    {
        SemiAutomatic,
        Automatic
    }
    [Header("Audio Data")]
    public WeaponAudioData weaponAudio;
    public ShootingMode currentShootingMode;

    private Animator animator;

    private void Awake()
    {
        readyToShoot = true;
        animator = GetComponent<Animator>();
    }
    void Update()
{
    // 1. Always track if the button is currently being held down (useful for animations/effects)
    isShooting = Mouse.current.leftButton.isPressed;

    // 2. Handle firing logic based on the shooting mode
    if (currentShootingMode == ShootingMode.SemiAutomatic)
    {
        // Semi-Auto: Only fires ONCE per click
        if (Mouse.current.leftButton.wasPressedThisFrame && readyToShoot)
        {
            FireWeapon();
            readyToShoot = false; 
        }
    }
    else if (currentShootingMode == ShootingMode.Automatic)
    {
        // Full-Auto: Fires continuously as long as it's held down
        if (isShooting && readyToShoot)
        {
            FireWeapon();
        }
    }
}

        private void FireWeapon()
    {
        readyToShoot = false;

        muzzleEffect.GetComponent<ParticleSystem>().Play();
        SoundManager.Instance.PlayWeaponShot(weaponAudio, bulletSpawnPoint.position);
        animator.SetTrigger("RECOIL"); 

        // 1. Get the direction aiming at the crosshair (with spread included)
        Vector3 shootingDirection = CalculateShootingDirectionAndSpread().normalized;

        // Instantiate the bullet
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);

        // 2. Make the visual bullet model face the target point
        bullet.transform.forward = shootingDirection; 

        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        
        // FIX: Change bulletSpawnPoint.forward to shootingDirection so physics matches your math!
        bulletRb.AddForce(shootingDirection * bulletSpeed, ForceMode.Impulse);

        Destroy(bullet, bulletLifetime);

        // Check if we are done shooting
        if (allowReset)
        {
            Invoke("ResetShot", shootingDelay);
            allowReset = false;
        }
    }

    private void ResetShot()
    {
        readyToShoot = true;
        allowReset = true;
    }

        public Vector3 CalculateShootingDirectionAndSpread()
    {
        // Shooting from middle of screen (crosshair)
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;

        Vector3 targetPoint;
        if (Physics.Raycast(ray, out hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.GetPoint(1000); // Arbitrary far point if nothing is hit
        }

        // Get the base direction from the gun barrel to the crosshair target point
        Vector3 direction = (targetPoint - bulletSpawnPoint.position).normalized;

        // Calculate random spread offsets
        float spreadX = Random.Range(-spreadIntensity, spreadIntensity);
        float spreadY = Random.Range(-spreadIntensity, spreadIntensity);

        // FIX: Instead of adding to global X/Y, project the spread relative to the camera's view
        Vector3 spreadVector = Camera.main.transform.right * spreadX + Camera.main.transform.up * spreadY;

        // Return the final direction combined with the relative spread
        return direction + spreadVector;    
    }   
}