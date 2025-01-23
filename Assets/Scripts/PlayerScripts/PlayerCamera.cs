using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform player; // Reference to the player's transform
    public float distanceFromPlayer = 5f; // Distance of the camera from the player
    public float cameraHeight = 2f; // Height of the camera relative to the player
    public float rotationSpeed = 5f; // Speed of camera rotation
    public float mouseSensitivity = 2f; // Sensitivity of mouse/joystick input

    private float yaw; // Rotation around Y-axis
    private float pitch; // Rotation around X-axis
    private float minPitch = -20f; // Minimum vertical camera angle
    private float maxPitch = 60f; // Maximum vertical camera angle

    void Start()
    {
        // Initialize the yaw and pitch based on the current camera rotation
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        void LateUpdate()
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yaw += mouseX;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

            // Use a raycast to check for obstacles
            Vector3 desiredCameraPos = player.position - (rotation * Vector3.forward * distanceFromPlayer) + Vector3.up * cameraHeight;
            RaycastHit hit;

            if (Physics.Linecast(player.position + Vector3.up * cameraHeight, desiredCameraPos, out hit))
            {
                // If the raycast hits something, move the camera closer
                transform.position = hit.point;
            }
            else
            {
                transform.position = desiredCameraPos;
            }

            transform.LookAt(player.position + Vector3.up * cameraHeight);
        }

    }
}
