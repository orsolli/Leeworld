using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float mouseSensitivity = 5.0f;
    public float pitchMin = -80.0f;
    public float pitchMax = 80.0f;
    private bool started = false;

    private Vector3 moveDirection = Vector3.zero;
    private float yaw = 0.0f;
    private float pitch = 0.0f;
    CharacterController controller;

    void Start()
    {
        controller = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        if (!started)
        {
            if (Input.GetButton("Jump"))
            {
                started = true;
            }
            else
            {
                return;
            }
        }
        // Rotate the character based on mouse movement
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, pitchMin, pitchMax);
        controller.transform.eulerAngles = new Vector3(pitch, yaw, 0.0f);

        if (controller.isGrounded)
        {
            // We are grounded, so recalculate move direction based on inputs
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection = controller.transform.TransformDirection(moveDirection);
            moveDirection *= speed;

            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpSpeed;
            }
        }

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        // Move the character
        controller.Move(moveDirection * Time.deltaTime);
    }
}
