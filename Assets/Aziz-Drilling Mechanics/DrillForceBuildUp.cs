using System.Collections;
using UnityEngine;

public class DrillForceBuildup : MonoBehaviour
{
    [Header("Force Buildup Settings")]
    public float forceIncreaseRate = 100f; // Force increase per second

    [SerializeField]
    float forceApplicationMagnitude = 500f; // Magnitude of force to apply

    [SerializeField]
    private float frontAngleThreshold = 60f; // Angle threshold to consider "front" collision

    [SerializeField]
    private float minMovementThreshold = 0.1f; // Minimum movement to consider player moving

    [Header("Object Shake Settings")]
    [SerializeField]
    private float objectShakeMagnitude = 0.02f; // Shake intensity for collided objects

    [SerializeField]
    private float objectShakeSpeed = 30f; // Speed of shake animation for objects

    [SerializeField]
    private bool enableObjectShake = true; // Toggle for object shaking

    [Header("Debug Settings")]
    [SerializeField]
    private bool debugForceBuildup = true;

    [SerializeField]
    private bool showDebugGizmos = true;

    // Current force value that builds up over time
    private float forceValue = 0f;

    // References
    private PlayerController playerController;
    private Rigidbody playerRigidbody;

    // Current collision tracking
    private GameObject currentTargetObject;
    private UnfreezeChilds currentTargetScript;
    private Collision currentCollision;
    private bool isInValidCollision = false;
    private bool forceAlreadyApplied = false;

    // Collision direction tracking
    private Vector3 collisionDirection;
    private Vector3 playerMovementDirection;

    // Vibration related
    private VibratePlayer playerVibrator;

    // Object shaking related
    private Transform currentTargetTransform;
    private Vector3 targetInitialPosition;
    private bool isShakingTarget = false;
    private Coroutine objectShakeCoroutine;

    void Start()
    {
        // Get required components
        playerController = GetComponent<PlayerController>();
        playerRigidbody = GetComponent<Rigidbody>();

        if (playerController == null)
        {
            Debug.LogError("[ForceBuildup] PlayerController component not found!");
        }

        if (playerRigidbody == null)
        {
            Debug.LogError("[ForceBuildup] Rigidbody component not found!");
        }

        playerVibrator = GetComponent<VibratePlayer>();
        if (playerVibrator == null)
        {
            Debug.LogWarning(
                "[ForceBuildup] VibratePlayer component not found. "
                    + "Shake and vibration functionality will be disabled."
            );
        }

        // FIXED: Set all destructible objects to drilling-only mode at start
        SetAllDestructiblesToDrillingMode();
    }

    // FIXED: Ensure all destructible objects use drilling system
    void SetAllDestructiblesToDrillingMode()
    {
        UnfreezeChilds[] allDestructibles = FindObjectsByType<UnfreezeChilds>(
            FindObjectsSortMode.None
        );
        foreach (UnfreezeChilds destructible in allDestructibles)
        {
            destructible.SetDrillingOnlyMode(true);
        }

        if (debugForceBuildup)
        {
            Debug.Log(
                $"[ForceBuildup] Set {allDestructibles.Length} destructible objects to drilling-only mode"
            );
        }
    }

    void Update()
    {
        // FIXED: Check if target object is still valid before processing
        ValidateCurrentTarget();

        bool wasValidForceBuildup = IsValidForceBuildup();

        UpdateForceBuildup();

        bool isValidForceBuildup = IsValidForceBuildup();

        // Handle player vibration
        if (isValidForceBuildup && !forceAlreadyApplied)
        {
            if (playerVibrator != null && !playerVibrator.IsShaking())
            {
                playerVibrator.StartShaking();
            }

            // Start shaking the target object
            if (enableObjectShake && !isShakingTarget && currentTargetTransform != null)
            {
                StartShakingTarget();
            }
        }
        else
        {
            if (playerVibrator != null && playerVibrator.IsShaking())
            {
                playerVibrator.StopShaking();
            }

            // Stop shaking the target object
            if (isShakingTarget)
            {
                StopShakingTarget();
            }
        }
    }

    // FIXED: New method to validate if current target is still valid
    void ValidateCurrentTarget()
    {
        if (currentTargetObject != null && currentTargetScript != null)
        {
            // Check if the target object has been fractured
            if (!currentTargetScript.IsIntact())
            {
                if (debugForceBuildup)
                {
                    Debug.Log(
                        $"[ForceBuildup] Target {currentTargetObject.name} has been fractured. Clearing collision state."
                    );
                }
                ClearCollisionState();
            }
            // Check if the target object has been destroyed
            else if (currentTargetObject == null)
            {
                if (debugForceBuildup)
                {
                    Debug.Log(
                        "[ForceBuildup] Target object has been destroyed. Clearing collision state."
                    );
                }
                ClearCollisionState();
            }
        }
    }

    // FIXED: Extracted method to clear collision state completely
    void ClearCollisionState()
    {
        // Stop shaking before clearing references
        if (isShakingTarget)
        {
            StopShakingTarget();
        }

        // Stop player vibration
        if (playerVibrator != null && playerVibrator.IsShaking())
        {
            playerVibrator.StopShaking();
        }

        // Clear all collision-related state
        isInValidCollision = false;
        currentTargetObject = null;
        currentTargetScript = null;
        currentTargetTransform = null;
        currentCollision = null;
        forceValue = 0f;
        forceAlreadyApplied = false;

        if (debugForceBuildup)
        {
            Debug.Log("[ForceBuildup] Collision state cleared completely.");
        }
    }

    void UpdateForceBuildup()
    {
        // Check if we're in a valid collision state
        bool shouldBuildForce = IsValidForceBuildup();

        if (shouldBuildForce && !forceAlreadyApplied)
        {
            // Increase force over time
            forceValue += forceIncreaseRate * Time.deltaTime;

            if (debugForceBuildup)
            {
                Debug.Log(
                    $"[ForceBuildup] Building force: {forceValue:F1} (Target: {GetTargetMinimumForce():F1})"
                );
            }

            // Check if we've reached the threshold
            float minimumForce = GetTargetMinimumForce();
            if (forceValue >= minimumForce)
            {
                ApplyForceToTarget();
            }
        }
        else
        {
            // Reset force if conditions are not met
            if (forceValue > 0f)
            {
                if (debugForceBuildup)
                {
                    Debug.Log("[ForceBuildup] Resetting force value to 0");
                }
                forceValue = 0f;
                forceAlreadyApplied = false;
            }
        }
    }

    bool IsValidForceBuildup()
    {
        // Must be in collision with a valid target
        if (!isInValidCollision || currentTargetObject == null || currentTargetScript == null)
            return false;

        // FIXED: Double-check that the target is still intact
        if (!currentTargetScript.IsIntact())
            return false;

        // Player must be moving
        if (playerController == null || !playerController.IsMoving() || !playerController.CanMove())
            return false;

        // Player movement direction
        Vector2 joystickInput = playerController.GetJoystickInput();
        playerMovementDirection = new Vector3(-joystickInput.y, 0f, joystickInput.x).normalized;

        if (playerMovementDirection.magnitude < minMovementThreshold)
            return false;

        // Check if player is moving toward the collision point
        Vector3 toCollisionPoint = collisionDirection.normalized;
        float movementAlignment = Vector3.Dot(playerMovementDirection, toCollisionPoint);

        if (debugForceBuildup && Time.frameCount % 30 == 0) // Log every 30 frames to avoid spam
        {
            Debug.Log(
                $"[ForceBuildup] Movement alignment: {movementAlignment:F2} (Threshold: 0.5)"
            );
        }

        // Player must be moving towards the collision (alignment > 0.5 means roughly forward)
        return movementAlignment > 0.5f;
    }

    float GetTargetMinimumForce()
    {
        if (currentTargetScript != null)
        {
            return currentTargetScript.MinimumCollisionForce;
        }
        return float.MaxValue; // If no target script, make it impossible to reach
    }

    void ApplyForceToTarget()
    {
        if (currentTargetObject == null || currentTargetScript == null)
            return;

        // FIXED: Check one more time if target is still intact before applying force
        if (!currentTargetScript.IsIntact())
        {
            ClearCollisionState();
            return;
        }

        // Stop shaking before applying force
        if (isShakingTarget)
        {
            StopShakingTarget();
        }

        // Get the rigidbody of the target object (could be on parent or child)
        Rigidbody targetRigidbody = currentTargetObject.GetComponent<Rigidbody>();
        if (targetRigidbody == null)
        {
            targetRigidbody = currentTargetObject.GetComponentInParent<Rigidbody>();
        }
        if (targetRigidbody == null)
        {
            targetRigidbody = currentTargetObject.GetComponentInChildren<Rigidbody>();
        }

        // Calculate force direction (from player toward object)
        Vector3 forceDirection = (
            currentTargetObject.transform.position - transform.position
        ).normalized;
        Vector3 forceVector = forceDirection * forceApplicationMagnitude;

        if (debugForceBuildup)
        {
            Debug.Log(
                $"[ForceBuildup] Applying force {forceApplicationMagnitude} to {currentTargetObject.name}"
            );
            Debug.Log($"[ForceBuildup] Force direction: {forceDirection}");
        }

        // Apply force to the target object if it has a rigidbody
        if (targetRigidbody != null)
        {
            targetRigidbody.AddForce(forceVector, ForceMode.Impulse);

            if (debugForceBuildup)
            {
                Debug.Log(
                    $"[ForceBuildup] Force applied to rigidbody on {targetRigidbody.gameObject.name}"
                );
            }
        }

        if (playerVibrator != null)
        {
            playerVibrator.StopShaking();
            playerVibrator.VibratePhone(); // Trigger phone vibration
        }

        // Call the fracture method
        currentTargetScript.Fracture();

        if (debugForceBuildup)
        {
            Debug.Log($"[ForceBuildup] Fracture() called on {currentTargetObject.name}");
        }

        // FIXED: Clear collision state immediately after fracturing
        ClearCollisionState();

        // Start coroutine to reset the applied flag after a short delay
        StartCoroutine(ResetForceAppliedFlag());
    }

    private void StartShakingTarget()
    {
        if (currentTargetTransform == null || isShakingTarget)
            return;

        isShakingTarget = true;
        targetInitialPosition = currentTargetTransform.localPosition;
        objectShakeCoroutine = StartCoroutine(ShakeTargetCoroutine());

        if (debugForceBuildup)
        {
            Debug.Log($"[ForceBuildup] Started shaking target: {currentTargetObject.name}");
        }
    }

    private void StopShakingTarget()
    {
        if (!isShakingTarget)
            return;

        isShakingTarget = false;

        if (objectShakeCoroutine != null)
        {
            StopCoroutine(objectShakeCoroutine);
            objectShakeCoroutine = null;
        }

        // Reset target position
        if (currentTargetTransform != null)
        {
            currentTargetTransform.localPosition = targetInitialPosition;
        }

        if (debugForceBuildup)
        {
            Debug.Log($"[ForceBuildup] Stopped shaking target");
        }
    }

    private IEnumerator ShakeTargetCoroutine()
    {
        while (isShakingTarget && currentTargetTransform != null)
        {
            // FIXED: Additional check to ensure target is still valid
            if (currentTargetScript != null && !currentTargetScript.IsIntact())
            {
                break;
            }

            // Calculate a random offset for the shake using Perlin noise
            Vector3 randomOffset =
                new Vector3(
                    Mathf.PerlinNoise(Time.time * objectShakeSpeed, 0) * 2 - 1,
                    Mathf.PerlinNoise(0, Time.time * objectShakeSpeed) * 2 - 1,
                    Mathf.PerlinNoise(Time.time * objectShakeSpeed, Time.time * objectShakeSpeed)
                        * 2
                        - 1
                ) * objectShakeMagnitude;

            currentTargetTransform.localPosition = targetInitialPosition + randomOffset;
            yield return null;
        }

        // Reset the target's position back to its original state
        if (currentTargetTransform != null)
        {
            currentTargetTransform.localPosition = targetInitialPosition;
        }
    }

    IEnumerator ResetForceAppliedFlag()
    {
        // Wait a short time before allowing another force application
        yield return new WaitForSeconds(0.5f);
        forceAlreadyApplied = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        ProcessCollision(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        ProcessCollision(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        // Check if this was our target object
        if (currentTargetObject == collision.gameObject)
        {
            if (debugForceBuildup)
            {
                Debug.Log(
                    $"[ForceBuildup] Exited collision with target: {collision.gameObject.name}"
                );
            }

            // FIXED: Use the centralized method to clear state
            ClearCollisionState();
        }
    }

    void ProcessCollision(Collision collision)
    {
        // Check if the collided object has an UnfreezeChilds script
        UnfreezeChilds unfreezeScript = collision.gameObject.GetComponent<UnfreezeChilds>();
        if (unfreezeScript == null)
        {
            // Try to find it on parent
            unfreezeScript = collision.gameObject.GetComponentInParent<UnfreezeChilds>();
        }

        if (unfreezeScript == null || !unfreezeScript.IsIntact())
        {
            return; // Not a valid target or already fractured
        }

        // FIXED: Ensure this object is set to drilling-only mode
        if (unfreezeScript.CanFractureByCollision())
        {
            unfreezeScript.SetDrillingOnlyMode(true);
            if (debugForceBuildup)
            {
                Debug.Log($"[ForceBuildup] Set {collision.gameObject.name} to drilling-only mode");
            }
        }

        // Calculate collision direction relative to player's forward
        Vector3 playerForward = transform.forward;
        Vector3 toCollisionObject = (collision.transform.position - transform.position).normalized;

        float angle = Vector3.Angle(playerForward, toCollisionObject);

        // Check if collision is in front of player
        if (angle <= frontAngleThreshold)
        {
            // Update current collision state
            currentTargetObject = collision.gameObject;
            currentTargetScript = unfreezeScript;
            currentTargetTransform = collision.transform;
            currentCollision = collision;
            isInValidCollision = true;

            // Store collision direction for movement checking
            if (collision.contactCount > 0)
            {
                collisionDirection = collision.contacts[0].normal * -1f; // Direction into the object
            }
            else
            {
                collisionDirection = toCollisionObject;
            }

            if (debugForceBuildup && Time.frameCount % 30 == 0) // Reduce spam
            {
                Debug.Log(
                    $"[ForceBuildup] Valid front collision with {collision.gameObject.name} "
                        + $"(Angle: {angle:F1}�, Min Force Required: {unfreezeScript.MinimumCollisionForce:F1})"
                );
            }
        }
    }

    // Public methods for external access
    public float GetCurrentForceValue()
    {
        return forceValue;
    }

    public bool IsInValidCollision()
    {
        return isInValidCollision;
    }

    public GameObject GetCurrentTarget()
    {
        return currentTargetObject;
    }

    public void ResetForceValue()
    {
        forceValue = 0f;
        forceAlreadyApplied = false;

        // Also stop any ongoing shaking
        if (isShakingTarget)
        {
            StopShakingTarget();
        }
    }

    // FIXED: New method to force clear all drilling state
    public void ForceStopDrilling()
    {
        ClearCollisionState();
    }

    public bool IsShakingTarget()
    {
        return isShakingTarget;
    }

    public void SetObjectShakeEnabled(bool enabled)
    {
        enableObjectShake = enabled;
        if (!enabled && isShakingTarget)
        {
            StopShakingTarget();
        }
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying)
            return;

        // Draw force buildup indicator
        if (isInValidCollision && forceValue > 0f)
        {
            Gizmos.color = Color.Lerp(
                Color.yellow,
                Color.red,
                forceValue / GetTargetMinimumForce()
            );
            Gizmos.DrawWireSphere(
                transform.position + Vector3.up * 3f,
                0.3f + (forceValue / GetTargetMinimumForce()) * 0.7f
            );
        }

        // Draw collision direction
        if (isInValidCollision && currentTargetObject != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + collisionDirection * 2f);

            // Draw line to target
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, currentTargetObject.transform.position);
        }

        // Draw movement direction
        if (playerController != null && playerController.IsMoving())
        {
            Vector2 joystickInput = playerController.GetJoystickInput();
            Vector3 movementDir = new Vector3(-joystickInput.y, 0f, joystickInput.x).normalized;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                transform.position + Vector3.up * 0.5f,
                transform.position + Vector3.up * 0.5f + movementDir * 2f
            );
        }

        // Indicate if target is shaking
        if (isShakingTarget && currentTargetObject != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(currentTargetObject.transform.position, Vector3.one * 0.5f);
        }
    }

    // Optional UI/Debug display method
    void OnGUI()
    {
        if (!debugForceBuildup)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 180));
        GUILayout.Label($"Force Value: {forceValue:F1}");

        if (currentTargetScript != null)
        {
            float targetForce = GetTargetMinimumForce();
            GUILayout.Label($"Target Force: {targetForce:F1}");
            GUILayout.Label($"Progress: {(forceValue / targetForce * 100f):F1}%");
        }

        GUILayout.Label($"Valid Collision: {isInValidCollision}");
        GUILayout.Label($"Force Applied: {forceAlreadyApplied}");
        GUILayout.Label($"Shaking Target: {isShakingTarget}");

        if (currentTargetObject != null)
        {
            GUILayout.Label($"Target: {currentTargetObject.name}");
        }

        GUILayout.EndArea();
    }

    void OnDisable()
    {
        // Clean up when the component is disabled
        ClearCollisionState();
    }
}
