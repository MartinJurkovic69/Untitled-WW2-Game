using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    public float interactionDistance = 5f;
    public float dropForce = 2f;
    public Transform weaponHolder;              
    public TextMeshProUGUI pickupPrompt;        

    [Header("Weapon Slots")]
    public Weapon primaryWeapon;               
    public Weapon secondaryWeapon;
    public Weapon activeWeapon;                 

    private Camera cam;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        cam = Camera.main;
    }

    private void Start()
    {
        HidePrompt();

        if (primaryWeapon != null) Holster(primaryWeapon);
        if (secondaryWeapon != null) Holster(secondaryWeapon);

        Weapon starting = primaryWeapon != null ? primaryWeapon : secondaryWeapon;
        if (starting != null) Draw(starting);
    }

    private void Update()
    {
        if (PauseMenu.IsPaused) return;
        HandleSwitchInput();
        HandleInteraction();
        UpdateWeaponUI();
    }


    private void HandleSwitchInput()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SwitchTo(primaryWeapon);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SwitchTo(secondaryWeapon);
        }

        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                SwitchTo(activeWeapon == primaryWeapon ? secondaryWeapon : primaryWeapon);
            }
        }
    }

    private void HandleInteraction()
    {
        Weapon targetWeapon = null;
        AmmoBox targetBox = null;

        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            Weapon weapon = hit.collider.GetComponentInParent<Weapon>();

            if (weapon != null)
            {
                if (weapon != primaryWeapon && weapon != secondaryWeapon)
                    targetWeapon = weapon;
            }
            else
            {
                targetBox = hit.collider.GetComponentInParent<AmmoBox>();
            }
        }

        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;

        if (targetWeapon != null)
        {
            string weaponName = string.IsNullOrEmpty(targetWeapon.displayName) ? targetWeapon.name : targetWeapon.displayName;
            string slotName = targetWeapon.weaponSlot == Weapon.WeaponSlot.Primary ? "primary" : "secondary";

            ShowPrompt($"Press 'E' to pick up {weaponName} to your {slotName} slot");

            if (ePressed) PickUp(targetWeapon);
        }
        else if (targetBox != null && activeWeapon != null)
        {
            if (activeWeapon.IsReserveFull)
            {
                ShowPrompt("You have max reserve ammo");
            }
            else
            {
                ShowPrompt("Press 'E' to restock ammo");
                if (ePressed) activeWeapon.RestockReserve();
            }
        }
        else
        {
            HidePrompt();   
        }
    }

    private void ShowPrompt(string text)
    {
        if (pickupPrompt == null) return;
        pickupPrompt.text = text;
        pickupPrompt.gameObject.SetActive(true);
    }

    private void HidePrompt()
    {
        if (pickupPrompt == null) return;
        pickupPrompt.gameObject.SetActive(false);
    }


    private void SwitchTo(Weapon target)
    {
        if (target == null || target == activeWeapon) return;

        if (activeWeapon != null) Holster(activeWeapon);
        Draw(target);
    }

    private void Draw(Weapon weapon)
    {
        if (weapon == null) return;

        weapon.gameObject.SetActive(true);
        weapon.SetEquipped(true);       // applies the held pose and turns the animator on
        SetWeaponLayer(weapon.gameObject, true);
        activeWeapon = weapon;
    }

    private void Holster(Weapon weapon)
    {
        if (weapon == null) return;

        weapon.SetEquipped(false);      
        weapon.gameObject.SetActive(false);
        if (activeWeapon == weapon) activeWeapon = null;
    }

    private void PickUp(Weapon weapon)
    {
        Holster(activeWeapon);

        Weapon existing = weapon.weaponSlot == Weapon.WeaponSlot.Primary ? primaryWeapon : secondaryWeapon;
        if (existing != null) Drop(existing);

        if (weapon.weaponSlot == Weapon.WeaponSlot.Primary) primaryWeapon = weapon;
        else secondaryWeapon = weapon;

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
            c.enabled = false;

        weapon.transform.SetParent(weaponHolder);

        Draw(weapon);
    }

    private void Drop(Weapon weapon)
    {
        if (weapon == primaryWeapon) primaryWeapon = null;
        if (weapon == secondaryWeapon) secondaryWeapon = null;
        if (weapon == activeWeapon) activeWeapon = null;

        weapon.gameObject.SetActive(true);   
        weapon.SetEquipped(false);
        weapon.transform.SetParent(null);
        SetWeaponLayer(weapon.gameObject, false);

        foreach (Collider c in weapon.GetComponentsInChildren<Collider>())
            c.enabled = true;

        Rigidbody rb = weapon.GetComponent<Rigidbody>();
        if (rb == null) rb = weapon.gameObject.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(cam.transform.forward * dropForce, ForceMode.Impulse);
    }


    private void UpdateWeaponUI()
    {
        if (AmmoManager.Instance == null) return;

        AmmoManager ui = AmmoManager.Instance;
        bool hasWeapon = activeWeapon != null;
        
        if (ui.weaponPanel != null)
            ui.weaponPanel.SetActive(hasWeapon);

        if (ui.ammoDisplay != null)
            ui.ammoDisplay.enabled = hasWeapon;
        if (ui.weaponIcon != null)
        {
            bool show = hasWeapon && activeWeapon.weaponIcon != null;
            ui.weaponIcon.gameObject.SetActive(show);
            if (show) ui.weaponIcon.sprite = activeWeapon.weaponIcon;
        }
        if (ui.weaponNameText != null)
        {
            ui.weaponNameText.gameObject.SetActive(hasWeapon);
            if (hasWeapon)
            {
                ui.weaponNameText.text = string.IsNullOrEmpty(activeWeapon.displayName)
                    ? activeWeapon.name : activeWeapon.displayName;
            }
        }
        if (!hasWeapon)
        {
            if (ui.reloadPrompt != null) ui.reloadPrompt.gameObject.SetActive(false);
            if (ui.ammoWarning != null) ui.ammoWarning.gameObject.SetActive(false);
        }
    }

    private void SetWeaponLayer(GameObject obj, bool held)
    {
        int weaponLayer = LayerMask.NameToLayer("Weapon");
        if (weaponLayer < 0) return;

        SetLayerRecursively(obj, held ? weaponLayer : 0);
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}