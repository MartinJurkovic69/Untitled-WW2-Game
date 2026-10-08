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

    
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * sensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, bottomClamp, topClamp);

        // Camera only pitches (up/down)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);


        // Player body only yaws (left/right)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}