using System.Collections.Generic;
using TMPro;
using Unity.AI.Assistant;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ThrowablesManager : MonoBehaviour
{
    public static ThrowablesManager Instance { get; private set; }

    [System.Serializable]
    public class Throwable
    {
        public string displayName;
        public Sprite icon;
        public int count;
        public GameObject prefab;
    }
    public List<Throwable> throwables = new List<Throwable>
    {
        new Throwable {displayName = "Frag Grenade", count = 2},
        new Throwable {displayName = "Smoke Grenade", count = 2},
        new Throwable {displayName = "Molotov", count = 2},
        new Throwable {displayName = "Throwing knife", count = 4},
        new Throwable {displayName = "Lure", count = 10},
    };
    public int equippedIndex = 0;
    [Header("Throwing")]
    public GameObject defaultThrowablePrefab;
    public float throwForce = 12f;
    public float throwUpwardForce = 2f;
    public float throwCooldown = 2f;
    public float throwableLifetime = 3f;

    private float nextThrowTime;
    private Camera cam;

    [Header("Indicator")]
    public Image indicatorIcon;
    public TextMeshProUGUI indicatorCount;
    public Image panelBackground;
    public float panelFadeSpeed = 6f;
    public CanvasGroup keyHint;
    [Header("DeadZone")]
    public RectTransform deadZoneVisual;
    private Canvas canvas;
    [Header("Wheel")]
    public RectTransform wheelBackground;
    [Range(0.4f,0.85f)] public float radiusFraction = 0.63f;
    public GameObject wheelRoot;
    public RectTransform slotContainer;
    public ThrowableWheelSlot slotPrefab;
    public float wheelRadius = 170f;
    public Vector2 slotSize = new Vector2(150f, 150f);
    public float deadZone = 50f;
    public float cursorSensitivity = 0.6f;
    public bool isWheelOpen {get; private set;}
    public Throwable Equipped =>(equippedIndex >= 0 && equippedIndex < throwables.Count) ? throwables[equippedIndex] : null;
    private readonly List<ThrowableWheelSlot> slots = new List<ThrowableWheelSlot>();
    private Vector2 virtualCursor;
    private int hoveredIndex = -1;
    private int shownIndex = -1;
    private int shownCount = -1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        cam = Camera.main;
    }
    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        if (wheelBackground != null)
            wheelRadius = wheelBackground.rect.width * 0.5f * radiusFraction;
        BuildWheel();
        if (wheelRoot != null) wheelRoot.SetActive(false);
        RefreshIndicator();
    }
    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.gKey.wasPressedThisFrame) OpenWheel();

            if (isWheelOpen)
            {
                UpdateWheelSelection();
                if (Keyboard.current.gKey.wasReleasedThisFrame) CloseWheel(true);
            }
        }
        HandleThrow();
        UpdatePanelBackground();
        RefreshIndicator();
    }
    private void BuildWheel()
    {
        if (slotPrefab == null || slotContainer == null) return;
        for (int i = 0; i < throwables.Count; i++)
        {
            ThrowableWheelSlot slot = Instantiate(slotPrefab, slotContainer);
            float angle = i * Mathf.PI * 2f / throwables.Count;
            RectTransform rt = (RectTransform)slot.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)* wheelRadius);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = slotSize;   
            rt.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * wheelRadius;
            slots.Add(slot);
        }
    }
    private void OpenWheel()
    {
        if (wheelRoot == null) return;
        isWheelOpen = true;
        virtualCursor = Vector2.zero;
        hoveredIndex = -1;
        for (int i = 0; i < slots.Count && i < throwables.Count; i++)
            slots[i].Setup(throwables[i].displayName, throwables[i].icon, throwables[i].count);
        wheelRoot.SetActive(true);
    }
    private void UpdateWheelSelection()
    {
        UpdateDeadzoneVisual();

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        if (Mouse.current != null)
             virtualCursor += Mouse.current.delta.ReadValue() / scale * cursorSensitivity;
        virtualCursor = Vector2.ClampMagnitude(virtualCursor, wheelRadius);

        int newHover = -1;
        if (virtualCursor.magnitude > deadZone && throwables.Count > 0)
        {
            float angle = Mathf.Atan2(virtualCursor.x, virtualCursor.y) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;

            float step = 360f / throwables.Count;
            newHover = Mathf.RoundToInt(angle / step) % throwables.Count;
        }
        hoveredIndex = newHover;

        for (int i = 0; i < slots.Count; i++)
            slots[i].SetHighlighted(i == hoveredIndex);
    }
    private void CloseWheel(bool confirm)
    {
        if (confirm && hoveredIndex >= 0) equippedIndex = hoveredIndex;
        isWheelOpen = false;
        if (wheelRoot != null) wheelRoot.SetActive(false);
    }
    private void UpdatePanelBackground()
    {
        if (panelBackground == null) return;
        bool hasWeapon = InteractionManager.Instance != null && InteractionManager.Instance.activeWeapon != null;
        float targetAlpha = hasWeapon ? 0f : 150f / 255f;

        Color c = panelBackground.color;
        c.a = Mathf.MoveTowards(c.a, targetAlpha, panelFadeSpeed * Time.deltaTime);
        panelBackground.color = c;
    }
    private void UpdateDeadzoneVisual()
    {
        if (deadZoneVisual == null) return;
        deadZoneVisual.sizeDelta = Vector2.one * deadZone * 2f;
    }
    private void RefreshIndicator()
    {
        Throwable t = Equipped;
        if (t == null) return;
        if (equippedIndex == shownIndex && t.count == shownCount) return;

        shownIndex = equippedIndex;
        shownCount = t.count;

        Color c = new Color(1f, 1f, 1f, t.count > 0 ? 1f: 0.3f);
        if (keyHint != null) keyHint.alpha = t.count > 0 ? 1f : 0.3f;

        if (indicatorIcon != null)
        {
            indicatorIcon.sprite = t.icon;
            indicatorIcon.enabled = t.icon != null;
            indicatorIcon.color = c;
        }
        if (indicatorCount != null)
        {
            indicatorCount.text = t.count.ToString();
            indicatorCount.color = c;
        }
    }
    private void HandleThrow()
    {
        if (isWheelOpen || Time.time < nextThrowTime) return;
        if (Keyboard.current == null || !Keyboard.current.qKey.wasPressedThisFrame) return;

        Throwable t = Equipped;
        if (t == null || t.count <=0)return;

        GameObject prefab = t.prefab != null ? t.prefab : defaultThrowablePrefab;
        if (prefab == null)return;
        t.count--;
        nextThrowTime = Time.time + throwCooldown;

        Vector3 origin = cam.transform.position + cam.transform.forward * 0.6f;
        GameObject thrown = Instantiate(prefab, origin, cam.transform.rotation);

        Collider[] cols = thrown.GetComponentsInChildren<Collider>();
        if (cols.Length == 0)
        {
            thrown.AddComponent<SphereCollider>();
        }
        else
        {
            foreach (Collider c in cols) c.isTrigger = false;
        }

        Rigidbody rb = thrown.GetComponent<Rigidbody>();
        if (rb == null) rb = thrown.AddComponent<Rigidbody>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 0.5f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0.3f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        rb.AddForce(cam.transform.forward * throwForce + cam.transform.up * throwUpwardForce, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);

        Destroy (thrown , throwableLifetime);
    }
}
