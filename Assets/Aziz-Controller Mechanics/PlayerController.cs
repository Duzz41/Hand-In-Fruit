using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 8f;

    [Header("Physics Settings")]
    [SerializeField] private float maxVelocity = 15f;
    [SerializeField] private float dragCoefficient = 0.8f; // Düzeltildi: 2f'den 0.8f'ye

    [Header("Collision Recovery")]
    [SerializeField] private bool autoRecoverFromCollision = true;
    [SerializeField] private float recoveryForceMultiplier = 1.2f; // Düzeltildi: 1.5f'den 1.2f'ye
    [SerializeField] private float collisionRecoveryTime = 1f; // Düzeltildi: 2f'den 1f'ye
    [SerializeField] private float minCollisionForceForRecovery = 3f; // Yeni: Minimum çarp??ma kuvveti

    [Header("Input Settings")]
    [SerializeField] private bool useBuiltInInput = true;
    [SerializeField] private bool useVirtualJoystick = true;

    [Header("Debug Settings")]
    [SerializeField] private bool debugVelocity = true;
    [SerializeField] private bool debugCollisions = true;
    [SerializeField] private float debugLogInterval = 0.5f;

    // Components
    private Rigidbody rb;
    private SimpleVirtualJoystick virtualJoystick;

    // Input variables
    private Vector2 joystickInput;
    private Vector3 moveDirection;
    private float currentSpeed;
    private Vector3 targetVelocity;

    // Physics backup variables
    private float originalMass;
    private float originalDrag;
    private float originalAngularDrag;

    // Collision recovery variables
    private bool isRecoveringFromCollision;
    private float collisionRecoveryTimer;
    private Vector3 preCollisionVelocity;
    private float momentumPreservation = 0.7f; // Yeni: Momentum koruma faktörü

    // Debug variables
    private float lastDebugTime;

    void Start()
    {
        InitializeComponents();
        SetupPhysics();
        BackupOriginalPhysicsValues();

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
        rb.linearDamping = dragCoefficient; // Art?k daha dü?ük de?er
        rb.angularDamping = 3f; // Düzeltildi: 5f'den 3f'ye
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                        RigidbodyConstraints.FreezeRotationX |
                        RigidbodyConstraints.FreezeRotationZ;
    }

    void BackupOriginalPhysicsValues()
    {
        originalMass = rb.mass;
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
    }

    void Update()
    {
        HandleInput();
        CalculateMovement();
        HandleCollisionRecovery();
        MonitorPhysicsValues();

        // Debug velocity logging
        if (debugVelocity && Time.time - lastDebugTime >= debugLogInterval)
        {
            DebugVelocityInfo();
            lastDebugTime = Time.time;
        }
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

        // Combine inputs
        if (joystickInputFromUI.magnitude > 0.1f)
        {
            joystickInput = joystickInputFromUI;
        }
        else
        {
            joystickInput = keyboardInput;
        }

        joystickInput = Vector2.ClampMagnitude(joystickInput, 1f);
    }

    void CalculateMovement()
    {
        moveDirection = new Vector3(joystickInput.x, 0f, joystickInput.y);
        float targetSpeed = moveDirection.magnitude * moveSpeed;

        if (moveDirection.magnitude > 0.1f)
        {
            float currentAcceleration = isRecoveringFromCollision ?
                acceleration * recoveryForceMultiplier : acceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAcceleration * Time.deltaTime);
        }
        else
        {
            // Düzeltildi: Recovery s?ras?nda daha yava? yava?lama
            float currentDeceleration = isRecoveringFromCollision ? deceleration * 0.5f : deceleration;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, currentDeceleration * Time.deltaTime);
        }

        targetVelocity = moveDirection.normalized * currentSpeed;
    }

    void ApplyMovement()
    {
        if (isRecoveringFromCollision)
        {
            // Düzeltildi: Recovery s?ras?nda momentum korumal? yumu?ak geçi?
            Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 preservedMomentum = preCollisionVelocity * momentumPreservation;
            Vector3 blendedVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity + preservedMomentum * 0.3f, Time.fixedDeltaTime * 2f);

            rb.linearVelocity = new Vector3(blendedVelocity.x, rb.linearVelocity.y, blendedVelocity.z);
        }
        else
        {
            // Normal hareket logic'i
            Vector3 velocityDifference = targetVelocity - new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 forceToApply = velocityDifference * rb.mass * acceleration;
            forceToApply = Vector3.ClampMagnitude(forceToApply, rb.mass * moveSpeed * 2f);
            rb.AddForce(forceToApply, ForceMode.Force);
        }
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
            // Düzeltildi: Daha yumu?ak h?z s?n?rlama
            Vector3 limitedVelocity = horizontalVelocity.normalized * maxVelocity;
            Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 smoothLimited = Vector3.Lerp(currentVelocity, limitedVelocity, Time.fixedDeltaTime * 5f);

            rb.linearVelocity = new Vector3(smoothLimited.x, rb.linearVelocity.y, smoothLimited.z);

            if (debugVelocity)
            {
                Debug.Log($"[PlayerController] Velocity limited! Original: {horizontalVelocity.magnitude:F2}, Limited to: {maxVelocity}");
            }
        }
    }

    void HandleCollisionRecovery()
    {
        if (isRecoveringFromCollision)
        {
            collisionRecoveryTimer -= Time.deltaTime;
            if (collisionRecoveryTimer <= 0f)
            {
                isRecoveringFromCollision = false;
                if (debugCollisions)
                {
                    Debug.Log("[PlayerController] Collision recovery completed.");
                }
            }
        }
    }

    void MonitorPhysicsValues()
    {
        // Reset physics values if they've been changed unexpectedly
        if (Mathf.Abs(rb.mass - originalMass) > 0.1f)
        {
            if (debugCollisions)
            {
                Debug.LogWarning($"[PlayerController] Mass changed from {originalMass} to {rb.mass}. Resetting.");
            }
            rb.mass = originalMass;
        }

        if (Mathf.Abs(rb.linearDamping - originalDrag) > 0.1f)
        {
            if (debugCollisions)
            {
                Debug.LogWarning($"[PlayerController] Linear damping changed from {originalDrag} to {rb.linearDamping}. Resetting.");
            }
            rb.linearDamping = originalDrag;
        }

        if (Mathf.Abs(rb.angularDamping - originalAngularDrag) > 0.1f)
        {
            if (debugCollisions)
            {
                Debug.LogWarning($"[PlayerController] Angular damping changed from {originalAngularDrag} to {rb.angularDamping}. Resetting.");
            }
            rb.angularDamping = originalAngularDrag;
        }
    }

    // Collision detection
    void OnCollisionEnter(Collision collision)
    {
        if (debugCollisions)
        {
            
             Debug.Log($"[PlayerController] Collision with {collision.gameObject.name}. " +
                     $"Impact force: {collision.impulse.magnitude:F2}");
            
        }

        // Düzeltildi: Sadece güçlü çarp??malarda recovery ba?lat
        if (autoRecoverFromCollision && collision.relativeVelocity.magnitude > minCollisionForceForRecovery)
        {
            preCollisionVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // Sadece horizontal velocity
            isRecoveringFromCollision = true;
            collisionRecoveryTimer = collisionRecoveryTime;

            if (debugCollisions)
            {
                Debug.Log($"[PlayerController] Starting collision recovery mode. Impact: {collision.relativeVelocity.magnitude:F2}");
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Düzeltildi: Daha kontrollü push force
        if (autoRecoverFromCollision && moveDirection.magnitude > 0.1f)
        {
            Vector3 pushForce = moveDirection.normalized * rb.mass * moveSpeed * 0.3f; // 0.5f'den 0.3f'ye
            rb.AddForce(pushForce, ForceMode.Force);
        }
    }

    void DebugVelocityInfo()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        string recoveryStatus = isRecoveringFromCollision ? " [RECOVERING]" : "";

        Debug.Log($"[PlayerController] Current Velocity: {currentVelocity} | " +
                  $"Horizontal Speed: {horizontalVelocity.magnitude:F2} | " +
                  $"Target Speed: {currentSpeed:F2} | " +
                  $"Moving: {IsMoving()}{recoveryStatus}");
    }

    // Public methods
    public void SetJoystickInput(Vector2 input) { }
    public void SetJoystickInput(float x, float y) { }
    public float GetCurrentSpeed() => currentSpeed;
    public Vector2 GetJoystickInput() => joystickInput;
    public bool IsMoving() => currentSpeed > 0.1f;

    // New public methods
    public void ForceResetPhysics()
    {
        rb.mass = originalMass;
        rb.linearDamping = originalDrag;
        rb.angularDamping = originalAngularDrag;
        Debug.Log("[PlayerController] Physics values manually reset.");
    }

    public bool IsRecoveringFromCollision() => isRecoveringFromCollision;

    // Yeni: Momentum'u manuel olarak s?f?rlama
    public void ResetMomentum()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        currentSpeed = 0f;
        isRecoveringFromCollision = false;
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (Application.isPlaying && rb != null)
        {
            // Movement direction
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + moveDirection * 3f);

            // Velocity direction
            Gizmos.color = Color.blue;
            Vector3 velocityDirection = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Gizmos.DrawLine(transform.position, transform.position + velocityDirection);

            // Recovery mode indicator
            if (isRecoveringFromCollision)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);
            }
        }
    }
}