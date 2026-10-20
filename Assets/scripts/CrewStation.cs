using UnityEngine;
using UnityEngine.InputSystem;

public class CrewStation : MonoBehaviour
{
    [Header("Camera")]
    public Camera stationCamera;
    [Tooltip("The object the mouse rotates (usually the camera itself, or a pivot the camera is parented to)")]
    public Transform lookPivot;
    public float sensitivity = 0.1f;

    [Header("Look limits")]
    public float yawLimit = 90f;
    public float pitchUpLimit = 60f;
    public float pitchDownLimit = 45f;

    [Header("Station")]
    public GameObject hudRoot;
    [Tooltip("Ends this station after this many seconds (0 = it only ends when its task is done or an event calls Complete)")]
    public float autoCompleteAfter = 0f;

    public bool IsActive {get;private set;}
    public event System.Action<CrewStation> Completed;

    private float yaw;
    private float pitch;
    private Quaternion baseRotation = Quaternion.identity;
    private float timeActive;
    private AudioListener listener;
    private bool initialised;

    protected virtual void Awake()
    {
        Initialise();
        SetPresentation(false);
    }
    private void Initialise()
    {
        if (initialised) return;
        initialised = true;

        if (lookPivot == null && stationCamera != null) lookPivot = stationCamera.transform;
        if (lookPivot != null) baseRotation = lookPivot.localRotation;
        if (stationCamera != null) listener = stationCamera.GetComponent<AudioListener>();
    }
    public void Begin()
    {
        Initialise();

        IsActive = true;
        timeActive = 0f;
        yaw = 0f;
        pitch = 0f;
        ApplyLook();

        SetPresentation(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        OnBegin();
    }
    public void End()
    {
        IsActive = false;
        SetPresentation(false);
        OnEnd();
    }
    public void Complete()
    {
        if (!IsActive) return;
        Completed?.Invoke(this);
    }
    private void SetPresentation(bool on)
    {
        if (stationCamera != null) stationCamera.enabled = on;
        if (listener != null) listener.enabled = on;
        if (hudRoot != null) hudRoot.SetActive(on);
    }
    private void Update()
    {
        if (!IsActive || PauseMenu.IsPaused) return;

        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue() * sensitivity;
            yaw = Mathf.Clamp(yaw * delta.x, -yawLimit, yawLimit);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchUpLimit, pitchDownLimit);
            ApplyLook();
        }
        timeActive += Time.deltaTime;
        if (autoCompleteAfter > 0f && timeActive >= autoCompleteAfter)
        {
            Complete();
            return;
        }
        OnStationUpdate();
    }
    private void ApplyLook()
    {
        if (lookPivot != null)
            lookPivot.localRotation = baseRotation * Quaternion.Euler(pitch , yaw , 0f);
    }
    public void AddRecoil(float pitchUp, float yawSideways)
    {
        pitch = Mathf.Clamp(pitch - pitchUp, -pitchUpLimit , pitchDownLimit);
        yaw = Mathf.Clamp(yaw + yawSideways, - yawLimit , yawLimit);
        ApplyLook();
    }
    protected virtual void OnBegin() {}
    protected virtual void OnEnd() {}
    protected virtual void OnStationUpdate() {}
}
