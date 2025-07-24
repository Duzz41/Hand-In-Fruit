using UnityEngine;

public class MobilePlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 8f;

    [Header("Physics Settings")]
    [SerializeField] private float maxVelocity = 15f;
    [SerializeField] private float dragCoefficient = 2f;

    [Header("Input Settings")]
    [SerializeField] private bool useBuiltInInput = true;
    [SerializeField] private bool useVirtualJoystick = true;

    // Components
    private Rigidbody rb;
    private SimpleVirtualJoystick virtualJoystick;

    // Input variables
    private Vector2 joystickInput;
    private Vector3 moveDirection;
    private float currentSpeed;
    private Vector3 targetVelocity;

    void Start()
    {
        InitializeComponents();
        SetupPhysics();

        // Find virtual joystick if using it
        if (useVirtualJoystick)
        {
            virtualJoystick = FindFirstObjectByType<SimpleVirtualJoystick>();
        }
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning("Rigidbody was missing and has been added automatically.");
        }
    }

    void SetupPhysics()
    {
        rb.mass = 2f;
        rb.linearDamping = dragCoefficient;
        rb.angularDamping = 5f;
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                        RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ;
    }

    void Update()
    {
        HandleInput();
        CalculateMovement();
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyRotation();
        LimitVelocity();
    }

    void HandleInput()
    {
        Vector2 keyboardInput = Vector2.zero;
        Vector2 joystickInputFromUI = Vector2.zero;

        // Get keyboard input
        if (useBuiltInInput)
        {
            keyboardInput.x = Input.GetAxis("Horizontal");
            keyboardInput.y = Input.GetAxis("Vertical");
        }

        // Get virtual joystick input
        if (useVirtualJoystick && virtualJoystick != null)
        {
            joystickInputFromUI = virtualJoystick.GetInputVector();
        }

        // Combine inputs (virtual joystick takes priority if being used)
        if (joystickInputFromUI.magnitude > 0.1f)
        {
            joystickInput = joystickInputFromUI;
        }
        else
        {
            joystickInput = keyboardInput;
        }

        // Clamp joystick input
        joystickInput = Vector2.ClampMagnitude(joystickInput, 1f);
    }

    void CalculateMovement()
    {
        moveDirection = new Vector3(joystickInput.x, 0f, joystickInput.y);
        float targetSpeed = moveDirection.magnitude * moveSpeed;

        if (moveDirection.magnitude > 0.1f)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
        }

        targetVelocity = moveDirection.normalized * currentSpeed;
    }

    void ApplyMovement()
    {
        Vector3 velocityDifference = targetVelocity - new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 forceToApply = velocityDifference * rb.mass * acceleration;
        forceToApply = Vector3.ClampMagnitude(forceToApply, rb.mass * moveSpeed * 2f);
        rb.AddForce(forceToApply, ForceMode.Force);
    }

    void ApplyRotation()
    {
        if (moveDirection.magnitude > 0.1f && currentSpeed > 0.5f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            float rotationStep = rotationSpeed * Time.fixedDeltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
        }
    }

    void LimitVelocity()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxVelocity)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * maxVelocity;
            rb.linearVelocity = new Vector3(limitedVelocity.x, rb.linearVelocity.y, limitedVelocity.z);
        }
    }

    // Keep your existing public methods
    public void SetJoystickInput(Vector2 input)
    {
        joystickInput = Vector2.ClampMagnitude(input, 1f);
    }

    public void SetJoystickInput(float x, float y)
    {
        joystickInput = Vector2.ClampMagnitude(new Vector2(x, y), 1f);
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    public Vector2 GetJoystickInput()
    {
        return joystickInput;
    }

    public bool IsMoving()
    {
        return currentSpeed > 0.1f;
    }

    // For Debug reasons
    void OnDrawGizmos()
    {
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + moveDirection * 3f);

            Gizmos.color = Color.blue;
            Vector3 velocityDirection = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Gizmos.DrawLine(transform.position, transform.position + velocityDirection);
        }
    }
}