using System.Collections.Generic;
using TMPro;
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
        public ThrowableType type;
        public ThrowableStats stats = new ThrowableStats();
        public GameObject prefab;
    }
    public List<Throwable> throwables = new List<Throwable>
    {
        new Throwable {displayName = "Frag Grenade", count = 2, type = ThrowableType.ThrowingKnife},
        new Throwable {displayName = "Smoke Grenade", count = 2, type = ThrowableType.SmokeGrenade},
        new Throwable {displayName = "Molotov", count = 2, type = ThrowableType.Molotov},
        new Throwable {displayName = "Throwing knife", count = 4, type = ThrowableType.ThrowingKnife},
        new Throwable {displayName = "Lure", count = 10, type = ThrowableType.Lure},
    };
    public int equippedIndex = 0;

    [Header("Indicator")]
    public Image indicatorIcon;
    public TextMeshProUGUI indicatorCount;
    public Image panelBackground;
    public float panelFadeSpeed = 6f;
    public CanvasGroup keyHint;

    [Header("Wheel")]
    public GameObject wheelRoot;
    public RectTransform slotContainer;
    public ThrowableWheelSlot slotPrefab;
    public Vector2 slotSize = new Vector2(150f, 150f);
    public float wheelRadius = 170f;
    public RectTransform wheelBackground;
    [Range(0.4f,0.85f)] public float radiusFraction = 0.63f;
    public float deadZone = 50f;
    public float cursorSensitivity = 0.6f;
    public RectTransform deadZoneVisual;    

    [Header("Throwing")]
    public GameObject defaultThrowablePrefab;
    public float throwCooldown = 0.8f;
    public bool isWheelOpen {get; private set;}
    public Throwable Equipped =>(equippedIndex >= 0 && equippedIndex < throwables.Count) ? throwables[equippedIndex] : null;

    private readonly List<ThrowableWheelSlot> slots = new List<ThrowableWheelSlot>();
    private Vector2 virtualCursor;
    private int hoveredIndex = -1;
    private int shownIndex = -1;
    private int shownCount = -1;
    private float nextThrowTime;
    private Camera cam;
    private Canvas canvas;

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
        foreach (Throwable t in throwables)
        {
            if (t.stats == null || !t.stats.isFilled)
                t.stats = ThrowableStats.DefaultFor(t.type);
        }
        BuildWheel();
        if (wheelRoot != null) wheelRoot.SetActive(false);
        RefreshIndicator();
    }
        // Right-click the component header to run this once: sets each entry's type by list order
    // (Knife, Frag, Smoke, Molotov, Lure) and fills the stats with the defaults so you can tune them.
    [ContextMenu("Reset Types And Stats To Defaults")]
    private void ResetTypesAndStats()
    {
        for (int i = 0; i < throwables.Count; i++)
        {
            if (i < 5) throwables[i].type = (ThrowableType)i;
            throwables[i].stats = ThrowableStats.DefaultFor(throwables[i].type);
        }
 
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
    #endif
    }
    [ContextMenu("Fill Stats From Current Types")]
    private void FillStatsFromCurrentTypes()
    {
        foreach (Throwable t in throwables)
            t.stats = ThrowableStats.DefaultFor(t.type);

    #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
    #endif
    }
    private void Update()
    {
        if (PauseMenu.IsPaused)
        {
            if (isWheelOpen) CloseWheel(false);
            return;
        }
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

        ThrowableProjectile projectile = thrown.GetComponent<ThrowableProjectile>();
        if (projectile == null) projectile = thrown.AddComponent<ThrowableProjectile>();

        projectile.Launch(t.type, t.stats, cam.transform);
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
    private void UpdateDeadzoneVisual()
    {
        if (deadZoneVisual == null) return;
        deadZoneVisual.sizeDelta = Vector2.one * deadZone * 2f;
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
    
}
