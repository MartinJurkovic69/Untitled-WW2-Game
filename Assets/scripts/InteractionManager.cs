using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }
    public float interactionDistance = 5f;
    public Transform weaponHolder; 
    public float dropForce = 1f; 
    [Header("Weapon Slots")]
    public Weapon primaryWeapon;
    public Weapon secondaryWeapon;
    private Weapon activeWeapon;
    private Camera mainCamera;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        mainCamera = Camera.main;
    }

    private void Start()
    {
        if (primaryWeapon != null)
        {
            Holster(primaryWeapon);
        }
        if (secondaryWeapon != null)
        {
            Holster(secondaryWeapon);
        }
        Weapon starting = primaryWeapon != null ? primaryWeapon : secondaryWeapon;
        if (starting != null)
        {
            Draw(starting);
        }
    }
    private void Update()
    {
        HandleSwitchInput();
        HandlePickup();
    }

    private void HandleSwitchInput()
    {
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchTo(primaryWeapon);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchTo(secondaryWeapon);
        }
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Math.Abs(scroll) > 0.01f)
            {
                SwitchTo(activeWeapon == primaryWeapon ? secondaryWeapon : primaryWeapon);
            }
        }
    }
    private void HandlePickup()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Weapon weapon = hit.collider.GetComponentInParent<Weapon>();
            if (weapon != null && weapon != primaryWeapon && weapon != secondaryWeapon)
            {
                //TODO: Show pickup prompt
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    Pickup(weapon);
                }
            }
        }
    }
    private void SwitchTo(Weapon target)
    {
        if (target == null || target == activeWeapon) return;
        if (activeWeapon != null)
        {
            Holster(activeWeapon);
        }
        Draw(target);
    }
    private void Draw(Weapon weapon)
    {
        if (weapon == null) return;
        weapon.gameObject.SetActive(true);
        weapon.SetEquipped(true);
        activeWeapon = weapon;
    }
    private void Holster(Weapon weapon)
    {
        if (weapon == null) return;
        weapon.SetEquipped(false);
        weapon.gameObject.SetActive(false);
        if (activeWeapon == weapon) activeWeapon = null;
    }
    private void Pickup(Weapon weapon)
    {
        Holster(activeWeapon);
        Weapon existingWeapon = weapon.weaponSlot == Weapon.WeaponSlot.Primary ? primaryWeapon : secondaryWeapon;
        if (existingWeapon != null)
        {
            Drop(existingWeapon);
        }
        if (weapon.weaponSlot == Weapon.WeaponSlot.Primary) primaryWeapon = weapon;
        else secondaryWeapon = weapon;

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
        weapon.transform.SetParent(weaponHolder);
        Draw(weapon);
    }
    
    private void Drop(Weapon weapon)
    {
        if (weapon == primaryWeapon)
        {
            primaryWeapon = null;
        }
        if (weapon == secondaryWeapon)
        {
            secondaryWeapon = null;
        }
        if (weapon == activeWeapon)
        {
            activeWeapon = null;
        }
        weapon.gameObject.SetActive(true);
        weapon.SetEquipped(false);
        weapon.transform.SetParent(null);
        foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
        {
            c.enabled = true;
        }
        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = weapon.gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(mainCamera.transform.forward * dropForce, ForceMode.Impulse);
    }
}
