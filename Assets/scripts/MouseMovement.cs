using UnityEngine;
using UnityEngine.InputSystem;

public class MouseMovement : MonoBehaviour
{
    public float sensitivity = 0.2f;
    public Transform playerBody; // assign the player/cylinder here
    public float topClamp = 90f;
    public float bottomClamp = -90f;

    float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (PauseMenu.IsPaused) return;
        if (ThrowablesManager.Instance != null && ThrowablesManager.Instance.isWheelOpen) return;
    
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float aimMultiplier = AimController.Instance != null ? AimController.Instance.SensitivityMultiplier : 1f;

        float mouseX = mouseDelta.x * sensitivity * aimMultiplier * Time.deltaTime;
        float mouseY = mouseDelta.y * sensitivity * aimMultiplier * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, bottomClamp, topClamp);

        // Camera only pitches (up/down)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);


        // Player body only yaws (left/right)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}