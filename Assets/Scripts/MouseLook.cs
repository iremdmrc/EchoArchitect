using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;
    public float mouseSensitivity = 120f;
    public float verticalClamp = 90f;

    private float xRotation = 0f;
    private float YRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (!EchoArchitectGameState.IsGameplayActive)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalClamp, verticalClamp);

        YRotation += mouseX; 

        transform.localRotation = Quaternion.Euler(xRotation, YRotation, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
