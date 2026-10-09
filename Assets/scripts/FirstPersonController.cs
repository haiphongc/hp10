using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    public float moveSpeed = 4.5f;
    public float jumpHeight = 1.2f;
    public float gravity = -19.62f;

    [Header("Look Settings")]
    public Transform cameraTransform;
    public float mouseSensitivity = 100.0f;
    public float verticalLookLimit = 85.0f;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool showHealthGUI = true;
    private float currentHealth;
    private bool isDead = false;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0.0f;

    void Start()
    {
        currentHealth = maxHealth;
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        if (isDead) return;

        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        if (cameraTransform == null) return;

        // Get mouse movement inputs
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity * 0.02f;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * 0.02f;

        // Pitch (Up / Down) -> Applied only to Camera
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -verticalLookLimit, verticalLookLimit);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Yaw (Left / Right) -> Applied to Player Body
        transform.Rotate(Vector3.up * mouseX, Space.World);
    }

    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1.0f);
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Debug.Log($"[Player] Took {amount} damage! Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("[Player] Player died!");
    }

    private void OnGUI()
    {
        if (!showHealthGUI) return;

        GUIStyle style = new GUIStyle();
        style.fontSize = 22;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = isDead ? Color.red : (currentHealth > 30f ? new Color(0.2f, 0.9f, 0.3f) : new Color(0.9f, 0.2f, 0.2f));

        string statusText = isDead ? "PLAYER DEAD" : $"HP: {Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        GUI.Label(new Rect(25, Screen.height - 50, 300, 40), statusText, style);
    }
}
