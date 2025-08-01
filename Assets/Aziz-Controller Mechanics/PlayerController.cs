using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField]
    private float moveSpeed = 10f;

    [SerializeField]
    private float rotationSpeed = 100f;

    [SerializeField]
    private float acceleration = 5f;

    [SerializeField]
    private float deceleration = 8f;

    [Header("Physics Settings")]
    [SerializeField]
    private float maxVelocity = 15f;

    [SerializeField]
    private float dragCoefficient = 0.8f;

    [Header("Collision Recovery")]
    [SerializeField]
    private bool autoRecoverFromCollision = true;

    [SerializeField]
    private float recoveryForceMultiplier = 1.2f;

    [SerializeField]
    private float collisionRecoveryTime = 1f;

    [SerializeField]
    private float minCollisionForceForRecovery = 3f;

    [Header("Input Settings")]
    [SerializeField]
    private bool useBuiltInInput = true;

    [SerializeField]
    private bool useVirtualJoystick = true;

    [Header("Debug Settings")]
    [SerializeField]
    private bool debugVelocity = true;

    [SerializeField]
    private bool debugCollisions = true;

    [SerializeField]
    private float debugLogInterval = 0.5f;

    // Components
    private Rigidbody rb;
    private SimpleVirtualJoystick virtualJoystick;
    private FuelSystem fuelSystem; // New: Fuel system reference

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
    private float momentumPreservation = 0.7f;

    [Header("Audio Settings")]
    [SerializeField]
    private AudioClip engineSound;

    [SerializeField]
    private AudioClip drillSound;

    private AudioSource engineSource;
    private AudioSource sfxSource;

    // Debug variables
    private float lastDebugTime;

    // New: Fuel system integration
    private bool canMove = true;

    void Start()
    {
        InitializeComponents();
        SetupPhysics();
        BackupOriginalPhysicsValues();
        // Ses kaynaklarını oluştur

        // Find virtual joystick if using it
        if (useVirtualJoystick)
        {
            virtualJoystick = FindFirstObjectByType<SimpleVirtualJoystick>();
        }

        // Initialize fuel system integration
        InitializeFuelSystem();
        SetupAudioSource();
    }

    void SetupAudioSource()
    {
        engineSource = gameObject.GetComponent<AudioSource>();
        engineSource.clip = engineSound;
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        engineSource.spatialBlend = 0f; // 2D ses

        sfxSource = gameObject.GetComponent<AudioSource>();

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;

        // Ses kaynaklarını SoundManager'a kayıt et
        SoundManager.instance.RegisterAudioSource(engineSource);
        SoundManager.instance.RegisterAudioSource(sfxSource);
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning("Rigidbody was missing and has been added automatically.");
        }

        // Get fuel system component
        fuelSystem = GetComponent<FuelSystem>();
        if (fuelSystem == null)
        {
            Debug.LogWarning(
                "[PlayerController] FuelSystem component not found. Movement will not be restricted by fuel."
            );
        }
    }

    void InitializeFuelSystem()
    {
        if (fuelSystem != null)
        {
            // Subscribe to fuel system events
            fuelSystem.OnFuelEmpty += OnFuelEmpty;
            fuelSystem.OnFuelRefilled += OnFuelRefilled;

            if (debugVelocity)
            {
                Debug.Log("[PlayerController] Fuel system integration initialized.");
            }
        }
    }

    void SetupPhysics()
    {
        rb.mass = 2f;
        rb.linearDamping = dragCoefficient;
        rb.angularDamping = 3f;
        rb.constraints =
            RigidbodyConstraints.FreezePositionY
            | RigidbodyConstraints.FreezeRotationX
            | RigidbodyConstraints.FreezeRotationZ;
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
        HandleEngineSound();

        // Debug velocity logging
        if (debugVelocity && Time.time - lastDebugTime >= debugLogInterval)
        {
            DebugVelocityInfo();
            lastDebugTime = Time.time;
        }
    }

    void FixedUpdate()
    {
        // Check if player can move (not out of fuel)
        if (canMove)
        {
            ApplyMovement();
            ApplyRotation();
        }
        else
        {
            // If out of fuel, gradually stop the player
            ApplyFuelEmptyDeceleration();
        }

        LimitVelocity();
    }

    void HandleEngineSound()
    {
        if (IsMoving() && !engineSource.isPlaying && CanMove())
        {
            SoundCallManager.instance.PlaySound("EngineSound");
        }
        else if ((!IsMoving() || !CanMove()) && engineSource.isPlaying)
        {
            SoundCallManager.instance.StopSound("EngineSound");
        }
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
        //moveDirection = new Vector3(joystickInput.x, 0f, joystickInput.y);
        //float targetSpeed = moveDirection.magnitude * moveSpeed;
        moveDirection = new Vector3(-joystickInput.y, 0f, joystickInput.x);
        float targetSpeed = moveDirection.magnitude * moveSpeed;

        // Only calculate movement if player can move
        if (canMove && moveDirection.magnitude > 0.1f)
        {
            float currentAcceleration = isRecoveringFromCollision
                ? acceleration * recoveryForceMultiplier
                : acceleration;
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                targetSpeed,
                currentAcceleration * Time.deltaTime
            );
        }
        else
        {
            // Decelerate when not moving or out of fuel
            float currentDeceleration = isRecoveringFromCollision
                ? deceleration * 0.5f
                : deceleration;
            currentSpeed = Mathf.MoveTowards(
                currentSpeed,
                0f,
                currentDeceleration * Time.deltaTime
            );
        }

        targetVelocity = moveDirection.normalized * currentSpeed;
    }

    void ApplyMovement()
    {
        if (isRecoveringFromCollision)
        {
            Vector3 currentHorizontalVelocity = new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );
            Vector3 preservedMomentum = preCollisionVelocity * momentumPreservation;
            Vector3 blendedVelocity = Vector3.Lerp(
                currentHorizontalVelocity,
                targetVelocity + preservedMomentum * 0.3f,
                Time.fixedDeltaTime * 2f
            );

            rb.linearVelocity = new Vector3(
                blendedVelocity.x,
                rb.linearVelocity.y,
                blendedVelocity.z
            );
        }
        else
        {
            Vector3 velocityDifference =
                targetVelocity - new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 forceToApply = velocityDifference * rb.mass * acceleration;
            forceToApply = Vector3.ClampMagnitude(forceToApply, rb.mass * moveSpeed * 2f);
            rb.AddForce(forceToApply, ForceMode.Force);
        }
    }

    void ApplyFuelEmptyDeceleration()
    {
        // Gradually slow down the player when out of fuel
        Vector3 currentHorizontalVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );
        Vector3 deceleratedVelocity = Vector3.Lerp(
            currentHorizontalVelocity,
            Vector3.zero,
            Time.fixedDeltaTime * deceleration
        );
        rb.linearVelocity = new Vector3(
            deceleratedVelocity.x,
            rb.linearVelocity.y,
            deceleratedVelocity.z
        );

        // Also reduce current speed
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime * 2f);
    }

    //void ApplyRotation()
    //{
    //    if (moveDirection.magnitude > 0.1f && currentSpeed > 0.5f && canMove)
    //    {
    //        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
    //        float rotationStep = rotationSpeed * Time.fixedDeltaTime;
    //        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationStep);
    //    }
    //}

    void ApplyRotation()
    {
        if (moveDirection.magnitude > 0.1f && currentSpeed > 0.5f && canMove)
        {
            // The vector for LookRotation needs the horizontal axis to be non-inverted
            // to ensure the player turns in the correct direction.
            Vector3 lookDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            float rotationStep = rotationSpeed * Time.fixedDeltaTime;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationStep
            );
        }
    }

    void LimitVelocity()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxVelocity)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * maxVelocity;
            Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 smoothLimited = Vector3.Lerp(
                currentVelocity,
                limitedVelocity,
                Time.fixedDeltaTime * 5f
            );

            rb.linearVelocity = new Vector3(smoothLimited.x, rb.linearVelocity.y, smoothLimited.z);

            if (debugVelocity)
            {
                Debug.Log(
                    $"[PlayerController] Velocity limited! Original: {horizontalVelocity.magnitude:F2}, Limited to: {maxVelocity}"
                );
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
        if (Mathf.Abs(rb.mass - originalMass) > 0.1f)
        {
            if (debugCollisions)
            {
                Debug.LogWarning(
                    $"[PlayerController] Mass changed from {originalMass} to {rb.mass}. Resetting."
                );
            }
            rb.mass = originalMass;
        }

        if (Mathf.Abs(rb.linearDamping - originalDrag) > 0.1f)
        {
            if (debugCollisions)
            {
                Debug.LogWarning(
                    $"[PlayerController] Linear damping changed from {originalDrag} to {rb.linearDamping}. Resetting."
                );
            }
            rb.linearDamping = originalDrag;
        }

        if (Mathf.Abs(rb.angularDamping - originalAngularDrag) > 0.1f)
        {
            if (debugCollisions)
            {
                Debug.LogWarning(
                    $"[PlayerController] Angular damping changed from {originalAngularDrag} to {rb.angularDamping}. Resetting."
                );
            }
            rb.angularDamping = originalAngularDrag;
        }
    }

    // Collision detection - Updated to work with fuel system
    void OnCollisionEnter(Collision collision)
    {
        if (debugCollisions)
        {
            Debug.Log(
                $"[PlayerController] Collision with {collision.gameObject.name}. "
                    + $"Impact force: {collision.impulse.magnitude:F2}"
            );
        }

        // Notify fuel system about wall collision
        if (fuelSystem != null)
        {
            fuelSystem.OnWallCollision(collision);
        }

        // SFX: Çarpışma sesi sadece belirli kuvvetin üstünde olunca çalsın
        // SFX: Çarpışma sesi sadece belirli kuvvetin üstünde olunca çalsın
        if (collision.relativeVelocity.magnitude > 2f)
        {
            SoundCallManager.instance.PlayOneShot("SFXSound", drillSound);
        }

        // Eğer otomatik kurtarma açıksa ve çarpışma yeterince güçlüyse
        if (
            autoRecoverFromCollision
            && collision.relativeVelocity.magnitude > minCollisionForceForRecovery
        )
        {
            preCollisionVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            isRecoveringFromCollision = true;
            collisionRecoveryTimer = collisionRecoveryTime;

            if (debugCollisions)
            {
                Debug.Log(
                    $"[PlayerController] Starting collision recovery mode. Impact: {collision.relativeVelocity.magnitude:F2}"
                );
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Continue notifying fuel system while colliding
        if (fuelSystem != null)
        {
            fuelSystem.OnWallCollision(collision);
        }

        if (autoRecoverFromCollision && moveDirection.magnitude > 0.1f && canMove)
        {
            Vector3 pushForce = moveDirection.normalized * rb.mass * moveSpeed * 0.3f;
            rb.AddForce(pushForce, ForceMode.Force);
        }
    }

    // Fuel system event handlers
    void OnFuelEmpty()
    {
        canMove = false;
        if (debugVelocity)
        {
            Debug.Log("[PlayerController] Movement disabled - out of fuel!");
        }
    }

    void OnFuelRefilled()
    {
        canMove = true;
        if (debugVelocity)
        {
            Debug.Log("[PlayerController] Movement enabled - fuel refilled!");
        }
    }

    void DebugVelocityInfo()
    {
        Vector3 currentVelocity = rb.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        string recoveryStatus = isRecoveringFromCollision ? " [RECOVERING]" : "";
        string fuelStatus = !canMove ? " [NO FUEL]" : "";
        string fuelLevel =
            fuelSystem != null
                ? $" Fuel: {fuelSystem.GetCurrentFuel():F1}/{fuelSystem.GetMaxFuel()}"
                : "";

        Debug.Log(
            $"[PlayerController] Current Velocity: {currentVelocity} | "
                + $"Horizontal Speed: {horizontalVelocity.magnitude:F2} | "
                + $"Target Speed: {currentSpeed:F2} | "
                + $"Moving: {IsMoving()}{recoveryStatus}{fuelStatus}{fuelLevel}"
        );
    }

    // Public methods - Updated
    public void SetJoystickInput(Vector2 input) { }

    public void SetJoystickInput(float x, float y) { }

    public float GetCurrentSpeed() => currentSpeed;

    public Vector2 GetJoystickInput() => joystickInput;

    public bool IsMoving() => currentSpeed > 0.1f;

    public bool CanMove() => canMove; // New: Check if player can move

    // New public methods
    public void ForceResetPhysics()
    {
        rb.mass = originalMass;
        rb.linearDamping = originalDrag;
        rb.angularDamping = originalAngularDrag;
        Debug.Log("[PlayerController] Physics values manually reset.");
    }

    public bool IsRecoveringFromCollision() => isRecoveringFromCollision;

    public void ResetMomentum()
    {
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        currentSpeed = 0f;
        isRecoveringFromCollision = false;
    }

    // New: Enable/disable movement (used by fuel system)
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
    }

    // Cleanup
    void OnDestroy()
    {
        if (fuelSystem != null)
        {
            fuelSystem.OnFuelEmpty -= OnFuelEmpty;
            fuelSystem.OnFuelRefilled -= OnFuelRefilled;
        }
    }

    // Debug visualization - Updated
    void OnDrawGizmos()
    {
        if (Application.isPlaying && rb != null)
        {
            // Movement direction
            Gizmos.color = canMove ? Color.green : Color.red;
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

            // No fuel indicator
            if (!canMove)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2.5f, Vector3.one * 0.5f);
            }
        }
    }
}
