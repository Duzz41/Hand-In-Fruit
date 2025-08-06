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

    [Header("Y-Position Lock Settings")]
    [SerializeField]
    private bool lockYPosition = true;

    [SerializeField]
    private float fixedYPosition = 0f;

    [SerializeField]
    private bool autoSetYPositionOnStart = true;

    [Header("Collision Recovery")]
    [SerializeField]
    private bool autoRecoverFromCollision = true;

    [SerializeField]
    private float recoveryForceMultiplier = 1.2f;

    [SerializeField]
    private float collisionRecoveryTime = 1f;

    [SerializeField]
    private float minCollisionForceForRecovery = 3f;

    [Header("Drilling Settings")]
    [SerializeField]
    private bool stopSlidingWhileDrilling = true;

    [SerializeField]
    private float drillingDragMultiplier = 3f;

    [SerializeField]
    private float drillingDeceleration = 15f;

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

    [Header("Sound Settings")]
    [Tooltip("SoundCallManager'da tanımlı ses isimleri")]
    [SerializeField]
    private string engineStartSoundName = "EngineStart";

    [SerializeField]
    private string engineNoiseSoundName = "EngineNoise";

    [SerializeField]
    private string drillWorkingSoundName = "DrillWorking";

    [SerializeField]
    private string drillBreakingSoundName = "DrillBreaking";

    // Components
    private Rigidbody rb;
    private SimpleVirtualJoystick virtualJoystick;
    private FuelSystem fuelSystem;
    private UpgradeManager upgradeManager;

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

    // Drilling state variables
    private bool isDrilling;
    private float drillingTimer;
    private int continuousCollisionCount;

    // Engine sound state variables
    private bool isEngineRunning = false;
    private bool wasMovingLastFrame = false;

    // Debug variables
    private float lastDebugTime;

    // Fuel system integration
    private bool canMove = true;

    void Start()
    {
        InitializeComponents();
        SetupPhysics();
        BackupOriginalPhysicsValues();
        SetupYPositionLock();

        if (useVirtualJoystick)
        {
            virtualJoystick = FindFirstObjectByType<SimpleVirtualJoystick>();
        }

        InitializeFuelSystem();

        // upgradeManager = UpgradeManager.Instance;
        if (upgradeManager == null)
        {
            Debug.LogError("UpgradeManager instance not found!");
        }
    }

    void HandleDrillingState()
    {
        if (drillingTimer > 0f)
        {
            drillingTimer -= Time.deltaTime;
        }

        bool wasDrilling = isDrilling;
        isDrilling = drillingTimer > 0f;

        if (wasDrilling != isDrilling && debugCollisions)
        {
            Debug.Log($"[PlayerController] Drilling state changed: {isDrilling}");
        }
    }

    void ApplyDrillingPhysics()
    {
        float currentDrag = rb.linearDamping;
        float targetDrag = originalDrag * drillingDragMultiplier;

        if (currentDrag < targetDrag)
        {
            rb.linearDamping = Mathf.Lerp(currentDrag, targetDrag, Time.fixedDeltaTime * 5f);
        }

        Vector3 currentVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (currentVelocity.magnitude > 0.1f)
        {
            Vector3 decelerationForce =
                -currentVelocity.normalized * rb.mass * drillingDeceleration;
            if (lockYPosition)
            {
                decelerationForce.y = 0f;
            }
            rb.AddForce(decelerationForce, ForceMode.Force);
        }
    }

    void OnDestroy()
    {
        // Motor sesini durdur
        StopEngine();

        if (fuelSystem != null)
        {
            fuelSystem.OnFuelEmpty -= OnFuelEmpty;
            fuelSystem.OnFuelRefilled -= OnFuelRefilled;
        }
    }

    void SetupYPositionLock()
    {
        if (lockYPosition)
        {
            if (autoSetYPositionOnStart)
            {
                fixedYPosition = transform.position.y;
            }

            Vector3 pos = transform.position;
            pos.y = fixedYPosition;
            transform.position = pos;

            if (debugVelocity)
            {
                Debug.Log($"[PlayerController] Y position locked to: {fixedYPosition}");
            }
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
        HandleDrillingState();
        MonitorPhysicsValues();
        HandleEngineSound(); // Basit motor sesi sistemi

        if (debugVelocity && Time.time - lastDebugTime >= debugLogInterval)
        {
            DebugVelocityInfo();
            lastDebugTime = Time.time;
        }
    }

    // BASİT MOTOR SESİ SİSTEMİ
    void HandleEngineSound()
    {
        bool isCurrentlyMoving = IsMoving() && CanMove();

        // Motor çalışma durumu kontrolü
        if (isCurrentlyMoving && !isEngineRunning)
        {
            StartEngine();
        }
        else if (!isCurrentlyMoving && isEngineRunning)
        {
            StopEngine();
        }

        wasMovingLastFrame = isCurrentlyMoving;
    }

    void StartEngine()
    {
        if (!isEngineRunning && SoundCallManager.instance != null)
        {
            isEngineRunning = true;

            // Motor çalıştırma sesi çal (tek seferlik)
            SoundCallManager.instance.PlayOneShot(engineStartSoundName);

            // Motor sürekli ses çal (loop)
            SoundCallManager.instance.PlaySound(engineNoiseSoundName);

            Debug.Log("[PlayerController] Motor çalıştırıldı!");
        }
    }

    void StopEngine()
    {
        if (isEngineRunning && SoundCallManager.instance != null)
        {
            isEngineRunning = false;

            // Motor sesini durdur
            SoundCallManager.instance.StopSound(engineNoiseSoundName);

            Debug.Log("[PlayerController] Motor durduruldu!");
        }
    }

    void FixedUpdate()
    {
        if (canMove)
        {
            ApplyMovement();
            ApplyRotation();
        }
        else
        {
            ApplyFuelEmptyDeceleration();
        }

        if (isDrilling && stopSlidingWhileDrilling)
        {
            ApplyDrillingPhysics();
        }

        LimitVelocity();
        EnforceYPositionLock();
    }

    void EnforceYPositionLock()
    {
        if (!lockYPosition)
            return;

        Vector3 currentPos = transform.position;
        if (Mathf.Abs(currentPos.y - fixedYPosition) > 0.001f)
        {
            currentPos.y = fixedYPosition;
            transform.position = currentPos;

            if (debugVelocity && Time.fixedTime % 1f < Time.fixedDeltaTime)
            {
                Debug.Log($"[PlayerController] Corrected Y position to {fixedYPosition}");
            }
        }

        Vector3 currentVelocity = rb.linearVelocity;
        if (Mathf.Abs(currentVelocity.y) > 0.001f)
        {
            currentVelocity.y = 0f;
            rb.linearVelocity = currentVelocity;
        }

        if ((rb.constraints & RigidbodyConstraints.FreezePositionY) == 0)
        {
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
            if (debugVelocity)
            {
                Debug.Log("[PlayerController] Restored Y position constraint on Rigidbody");
            }
        }
    }

    void HandleInput()
    {
        Vector2 keyboardInput = Vector2.zero;
        Vector2 joystickInputFromUI = Vector2.zero;

        if (useBuiltInInput)
        {
            keyboardInput.x = Input.GetAxis("Horizontal");
            keyboardInput.y = Input.GetAxis("Vertical");
        }

        if (useVirtualJoystick && virtualJoystick != null)
        {
            joystickInputFromUI = virtualJoystick.GetInputVector();
        }

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
        moveDirection = new Vector3(-joystickInput.y, 0f, joystickInput.x);
        float targetSpeed = moveDirection.magnitude * moveSpeed;

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
        if (isDrilling && stopSlidingWhileDrilling)
        {
            Vector3 currentHorizontalVelocity = new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );
            Vector3 deceleratedVelocity = Vector3.Lerp(
                currentHorizontalVelocity,
                Vector3.zero,
                Time.fixedDeltaTime * drillingDeceleration
            );
            rb.linearVelocity = new Vector3(
                deceleratedVelocity.x,
                lockYPosition ? 0f : rb.linearVelocity.y,
                deceleratedVelocity.z
            );
            return;
        }

        if (isRecoveringFromCollision && !isDrilling)
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
                lockYPosition ? 0f : rb.linearVelocity.y,
                blendedVelocity.z
            );
        }
        else
        {
            Vector3 velocityDifference =
                targetVelocity - new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 forceToApply = velocityDifference * rb.mass * acceleration;
            forceToApply = Vector3.ClampMagnitude(forceToApply, rb.mass * moveSpeed * 2f);

            if (lockYPosition)
            {
                forceToApply.y = 0f;
            }

            rb.AddForce(forceToApply, ForceMode.Force);
        }
    }

    void ApplyFuelEmptyDeceleration()
    {
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
            lockYPosition ? 0f : rb.linearVelocity.y,
            deceleratedVelocity.z
        );
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime * 2f);
    }

    void ApplyRotation()
    {
        if (moveDirection.magnitude > 0.1f && currentSpeed > 0.5f && canMove)
        {
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
            rb.linearVelocity = new Vector3(
                smoothLimited.x,
                lockYPosition ? 0f : rb.linearVelocity.y,
                smoothLimited.z
            );

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

        if (!isDrilling && Mathf.Abs(rb.linearDamping - originalDrag) > 0.1f)
        {
            if (debugCollisions)
            {
                Debug.LogWarning(
                    $"[PlayerController] Linear damping changed from {originalDrag} to {rb.linearDamping}. Resetting."
                );
            }
            rb.linearDamping = originalDrag;
        }
        else if (!isDrilling && rb.linearDamping != originalDrag)
        {
            rb.linearDamping = Mathf.Lerp(rb.linearDamping, originalDrag, Time.deltaTime * 3f);
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

        if (lockYPosition && (rb.constraints & RigidbodyConstraints.FreezePositionY) == 0)
        {
            rb.constraints |= RigidbodyConstraints.FreezePositionY;
            if (debugCollisions)
            {
                Debug.LogWarning(
                    "[PlayerController] Y position constraint was removed. Restoring."
                );
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (debugCollisions)
        {
            Debug.Log(
                $"[PlayerController] Collision with {collision.gameObject.name}. Impact force: {collision.impulse.magnitude:F2}"
            );
        }

        if (fuelSystem != null)
        {
            fuelSystem.OnWallCollision(collision);
        }

        if (collision.relativeVelocity.magnitude > 2f)
        {
            // Delme sesi çal
            if (SoundCallManager.instance != null)
            {
                SoundCallManager.instance.PlayOneShot(drillWorkingSoundName);
            }

            if (stopSlidingWhileDrilling)
            {
                isDrilling = true;
                drillingTimer = 0.5f;
                continuousCollisionCount++;

                if (debugCollisions)
                {
                    Debug.Log(
                        $"[PlayerController] Drilling started. Collision count: {continuousCollisionCount}"
                    );
                }
            }
        }

        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = fixedYPosition;
            transform.position = pos;
        }

        if (
            autoRecoverFromCollision
            && collision.relativeVelocity.magnitude > minCollisionForceForRecovery
            && !isDrilling
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
        if (fuelSystem != null)
        {
            fuelSystem.OnWallCollision(collision);
        }

        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = fixedYPosition;
            transform.position = pos;
        }

        if (stopSlidingWhileDrilling)
        {
            drillingTimer = Mathf.Max(drillingTimer, 0.2f);
        }

        if (autoRecoverFromCollision && moveDirection.magnitude > 0.1f && canMove && !isDrilling)
        {
            Vector3 pushForce = moveDirection.normalized * rb.mass * moveSpeed * 0.3f;
            if (lockYPosition)
            {
                pushForce.y = 0f;
            }
            rb.AddForce(pushForce, ForceMode.Force);
        }
    }

    void OnFuelEmpty()
    {
        canMove = false;
        StopEngine(); // Yakıt bittiğinde motoru durdur
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

        string fuelLevel = "";
        if (upgradeManager != null)
        {
            //$" fuelLevel =Fuel: {upgradeManager.currentFuel:F1}/{upgradeManager.maxFuelCapacity:F1}";
        }
        else if (fuelSystem != null)
        {
            //fuelLevel = $" Fuel: {fuelSystem.GetCurrentFuel():F1}/{fuelSystem.GetMaxFuel()}";
        }
        else
        {
            fuelLevel = " Fuel: N/A";
        }

        string recoveryStatus = isRecoveringFromCollision ? " [RECOVERING]" : "";
        string fuelStatus = !canMove ? " [NO FUEL]" : "";
        string yPositionStatus = lockYPosition ? $" Y-Lock: {fixedYPosition:F2}" : "";
        string drillingStatus = isDrilling ? " [DRILLING]" : "";
        string engineStatus = isEngineRunning ? " [ENGINE ON]" : " [ENGINE OFF]";

        Debug.Log(
            $"[PlayerController] Current Velocity: {currentVelocity} | "
                + $"Horizontal Speed: {horizontalVelocity.magnitude:F2} | "
                + $"Target Speed: {currentSpeed:F2} | "
                + $"Moving: {IsMoving()}{recoveryStatus}{fuelStatus}{fuelLevel}{yPositionStatus}{drillingStatus}{engineStatus}"
        );
    }

    // Public methods
    public void SetJoystickInput(Vector2 input)
    {
        joystickInput = input;
    }

    public void SetJoystickInput(float x, float y)
    {
        joystickInput = new Vector2(x, y);
    }

    public Vector2 GetJoystickInput() => joystickInput;

    public float GetCurrentSpeed() => currentSpeed;

    public bool IsMoving() => currentSpeed > 0.1f;

    public bool CanMove() => canMove;

    public void SetYPositionLock(bool lockY)
    {
        lockYPosition = lockY;
        if (lockY)
        {
            fixedYPosition = transform.position.y;
            EnforceYPositionLock();
        }
    }

    public void SetFixedYPosition(float yPos)
    {
        fixedYPosition = yPos;
        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = fixedYPosition;
            transform.position = pos;
        }
    }

    public float GetFixedYPosition() => fixedYPosition;

    public bool IsYPositionLocked() => lockYPosition;

    public void ForceResetPhysics()
    {
        rb.mass = originalMass;
        rb.linearDamping = originalDrag;
        rb.angularDamping = originalAngularDrag;

        if (lockYPosition)
        {
            rb.constraints =
                RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
        }

        Debug.Log("[PlayerController] Physics values manually reset.");
    }

    public bool IsRecoveringFromCollision() => isRecoveringFromCollision;

    public bool IsDrilling() => isDrilling;

    public void ResetMomentum()
    {
        rb.linearVelocity = new Vector3(0f, lockYPosition ? 0f : rb.linearVelocity.y, 0f);
        currentSpeed = 0f;
        isRecoveringFromCollision = false;
        isDrilling = false;
        drillingTimer = 0f;
        continuousCollisionCount = 0;

        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = fixedYPosition;
            transform.position = pos;
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
        if (!enabled)
        {
            StopEngine(); // Hareket devre dışı bırakılırsa motoru durdur
        }
    }

    public bool IsEngineRunning() => isEngineRunning;

    // Debug visualization
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

            // Drilling indicator
            if (isDrilling)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.3f);
            }

            // Engine running indicator
            if (isEngineRunning)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.4f);
            }

            // Y position lock indicator
            if (lockYPosition)
            {
                Gizmos.color = Color.yellow;
                Vector3 lockLineStart = new Vector3(
                    transform.position.x - 1f,
                    fixedYPosition,
                    transform.position.z - 1f
                );
                Vector3 lockLineEnd = new Vector3(
                    transform.position.x + 1f,
                    fixedYPosition,
                    transform.position.z + 1f
                );
                Gizmos.DrawLine(lockLineStart, lockLineEnd);

                lockLineStart = new Vector3(
                    transform.position.x + 1f,
                    fixedYPosition,
                    transform.position.z - 1f
                );
                lockLineEnd = new Vector3(
                    transform.position.x - 1f,
                    fixedYPosition,
                    transform.position.z + 1f
                );
                Gizmos.DrawLine(lockLineStart, lockLineEnd);
            }
        }
    }
}
