using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.VisualScripting;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }
    public float interactionDistance = 5f;
    public Transform weaponHolder; 
    public float dropForce = 1f; 
    public TextMeshProUGUI pickupPrompt;
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
        UpdateWeaponUI();
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
        Weapon target = null;
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Weapon weapon = hit.collider.GetComponentInParent<Weapon>();
            if (weapon != null && weapon != primaryWeapon && weapon != secondaryWeapon)
            {
                target = weapon;
            }
        }
        ShowPickupPrompt(target);
        if (target != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Pickup(target);
        }
    }
    private void ShowPickupPrompt(Weapon weapon)
    {
        if (pickupPrompt == null) return;
        if (weapon == null)
        {
            pickupPrompt.gameObject.SetActive(false);
            return;
        }
        string weaponName = string.IsNullOrEmpty(weapon.displayName) ? weapon.name : weapon.displayName;
        string slotName = weapon.weaponSlot == Weapon.WeaponSlot.Primary ? "Primary" : "Secondary";
        pickupPrompt.text = $"Press 'E' to pick up {weaponName} ({slotName})";
        pickupPrompt.gameObject.SetActive(true);
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
    private void UpdateWeaponUI()
    {
        if (AmmoManager.Instance == null) return;
        bool hasWeapon = activeWeapon != null;
        if (AmmoManager.Instance.ammoDisplay != null)
        {
            AmmoManager.Instance.ammoDisplay.enabled = hasWeapon;
        }
        if (!hasWeapon)
    {
            if (AmmoManager.Instance.reloadPrompt != null){
                AmmoManager.Instance.reloadPrompt.gameObject.SetActive(false);
            }
            if (AmmoManager.Instance.ammoWarning != null){
                AmmoManager.Instance.ammoWarning.gameObject.SetActive(false);
            }
        }
    }
}
