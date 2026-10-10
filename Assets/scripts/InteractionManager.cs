using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }
    public float interactionDistance = 5f;
    private Camera mainCamera;
    public Transform weaponHolder; 
    public Weapon equippedWeapon; 
    public float dropForce = 1f; 
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        mainCamera = Camera.main;
    }

    private void Update()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            Weapon weapon = hit.transform.GetComponent<Weapon>();
            if (weapon != null && weapon != equippedWeapon)
            {
                if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                {
                    EquipWeapon(weapon);
                }
            }
        }
    }
    private void EquipWeapon(Weapon weapon)
    {
        if (equippedWeapon != null)
        {
            Drop(equippedWeapon);
        } 

        equippedWeapon = weapon;
        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
        weapon.transform.SetParent(weaponHolder);
        weapon.SetEquipped(true);
    }
    private void Drop(Weapon weapon)
    {
        weapon.SetEquipped(false);
        weapon.transform.SetParent(null);
        foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
        {
            c.enabled = true;
        }
        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb == null) rb = weapon.gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(mainCamera.transform.forward * dropForce, ForceMode.Impulse);
    }
}
