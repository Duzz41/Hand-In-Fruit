using UnityEngine;
using System.Collections;

public class DrillForceBuildup : MonoBehaviour
{
    [Header("Force Buildup Settings")]
    [SerializeField] private float forceIncreaseRate = 100f; // Force increase per second
    [SerializeField] private float forceApplicationMagnitude = 500f; // Magnitude of force to apply
    [SerializeField] private float frontAngleThreshold = 60f; // Angle threshold to consider "front" collision
    [SerializeField] private float minMovementThreshold = 0.1f; // Minimum movement to consider player moving

    [Header("Debug Settings")]
    [SerializeField] private bool debugForceBuildup = true;
    [SerializeField] private bool showDebugGizmos = true;

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
    }

    void Update()
    {
        UpdateForceBuildup();
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
                Debug.Log($"[ForceBuildup] Building force: {forceValue:F1} (Target: {GetTargetMinimumForce():F1})");
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
            Debug.Log($"[ForceBuildup] Movement alignment: {movementAlignment:F2} (Threshold: 0.5)");
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
        Vector3 forceDirection = (currentTargetObject.transform.position - transform.position).normalized;
        Vector3 forceVector = forceDirection * forceApplicationMagnitude;

        if (debugForceBuildup)
        {
            Debug.Log($"[ForceBuildup] Applying force {forceApplicationMagnitude} to {currentTargetObject.name}");
            Debug.Log($"[ForceBuildup] Force direction: {forceDirection}");
        }

        // Apply force to the target object if it has a rigidbody
        if (targetRigidbody != null)
        {
            targetRigidbody.AddForce(forceVector, ForceMode.Impulse);

            if (debugForceBuildup)
            {
                Debug.Log($"[ForceBuildup] Force applied to rigidbody on {targetRigidbody.gameObject.name}");
            }
        }

        // Call the fracture method
        currentTargetScript.Fracture();

        if (debugForceBuildup)
        {
            Debug.Log($"[ForceBuildup] Fracture() called on {currentTargetObject.name}");
        }

        // Reset and mark as applied
        forceValue = 0f;
        forceAlreadyApplied = true;

        // Start coroutine to reset the applied flag after a short delay
        StartCoroutine(ResetForceAppliedFlag());
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
                Debug.Log($"[ForceBuildup] Exited collision with target: {collision.gameObject.name}");
            }

            // Clear current collision state
            isInValidCollision = false;
            currentTargetObject = null;
            currentTargetScript = null;
            currentCollision = null;
            forceValue = 0f;
            forceAlreadyApplied = false;
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
                Debug.Log($"[ForceBuildup] Valid front collision with {collision.gameObject.name} " +
                         $"(Angle: {angle:F1}°, Min Force Required: {unfreezeScript.MinimumCollisionForce:F1})");
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
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying)
            return;

        // Draw force buildup indicator
        if (isInValidCollision && forceValue > 0f)
        {
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, forceValue / GetTargetMinimumForce());
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.3f + (forceValue / GetTargetMinimumForce()) * 0.7f);
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
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f,
                           transform.position + Vector3.up * 0.5f + movementDir * 2f);
        }
    }

    // Optional UI/Debug display method
    void OnGUI()
    {
        if (!debugForceBuildup)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"Force Value: {forceValue:F1}");

        if (currentTargetScript != null)
        {
            float targetForce = GetTargetMinimumForce();
            GUILayout.Label($"Target Force: {targetForce:F1}");
            GUILayout.Label($"Progress: {(forceValue / targetForce * 100f):F1}%");
        }

        GUILayout.Label($"Valid Collision: {isInValidCollision}");
        GUILayout.Label($"Force Applied: {forceAlreadyApplied}");

        if (currentTargetObject != null)
        {
            GUILayout.Label($"Target: {currentTargetObject.name}");
        }

        GUILayout.EndArea();
    }
}