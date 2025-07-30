using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [Header("Fuel Settings")]
    [SerializeField]
    private float maxFuel = 100f;

    [SerializeField]
    private float currentFuel = 100f;

    [SerializeField]
    private float fuelConsumptionRate = 10f; // Fuel per second while moving

    [SerializeField]
    private float wallCollisionFuelRate = 20f; // Extra fuel consumption when hitting walls

    [SerializeField]
    private float refuelRate = 30f; // Fuel per second when refueling

    [SerializeField]
    private float lowFuelThreshold = 20f; // Warning threshold

    [Header("Refuel Area")]
    [SerializeField]
    private Transform startAreaCenter;

    [SerializeField]
    private float refuelAreaRadius = 5f;

    [SerializeField]
    private bool autoTeleportOnEmpty = true;

    [SerializeField]
    private float teleportDelay = 1f; // Delay before teleporting when fuel is empty

    [Header("Debug Settings")]
    [SerializeField]
    private bool debugFuel = true;

    [SerializeField]
    private bool debugRefuelArea = true;

    [SerializeField]
    private float debugLogInterval = 1f;

    // Components
    private PlayerController playerController;

    // Fuel state variables
    private bool isInRefuelArea = false;
    private bool isRefueling = false;
    private bool isOutOfFuel = false;
    private bool isTeleporting = false;
    private float teleportTimer = 0f;

    // Wall collision detection
    private bool isCollidingWithWall = false;
    private float wallCollisionTimer = 0f;
    private const float wallCollisionCooldown = 0.1f; // Prevent rapid fuel drain

    // Debug variables
    private float lastDebugTime = 0f;
    private float lastFuelAmount = 0f;

    // Events for UI and other systems
    public System.Action<float, float> OnFuelChanged; // currentFuel, maxFuel
    public System.Action OnFuelEmpty;
    public System.Action OnFuelRefilled;
    public System.Action<bool> OnRefuelAreaEntered; // true = entered, false = exited
    public System.Action<bool> OnLowFuelWarning; // true = low fuel, false = fuel okay

    void Start()
    {
        InitializeComponents();
        InitializeFuel();
        SetupStartArea();
    }

    void InitializeComponents()
    {
        playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[FuelSystem] PlayerController component not found!");
        }
    }

    void InitializeFuel()
    {
        currentFuel = maxFuel;
        lastFuelAmount = currentFuel;

        if (debugFuel)
        {
            Debug.Log($"[FuelSystem] Initialized with {currentFuel}/{maxFuel} fuel");
        }

        // Trigger initial fuel update
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
    }

    void SetupStartArea()
    {
        // If no start area is assigned, use current position
        if (startAreaCenter == null)
        {
            GameObject startAreaGO = new GameObject("StartArea");
            startAreaCenter = startAreaGO.transform;
            startAreaCenter.position = transform.position;

            if (debugRefuelArea)
            {
                Debug.Log(
                    $"[FuelSystem] Created start area at position: {startAreaCenter.position}"
                );
            }
        }
    }

    void Update()
    {
        HandleFuelConsumption();
        HandleRefueling();
        CheckRefuelArea();
        HandleTeleportation();
        HandleWarnings();

        // Debug logging
        if (debugFuel && Time.time - lastDebugTime >= debugLogInterval)
        {
            LogFuelDebugInfo();
            lastDebugTime = Time.time;
        }
    }

    void HandleFuelConsumption()
    {
        if (isOutOfFuel || isTeleporting || isRefueling)
            return;

        bool isPlayerMoving = playerController != null && playerController.IsMoving();
        bool isPlayerTryingToMove =
            playerController != null && playerController.GetJoystickInput().magnitude > 0.1f;

        // Consume fuel if player is moving OR trying to move (including against walls)
        if (isPlayerMoving || isPlayerTryingToMove)
        {
            float baseFuelConsumption = fuelConsumptionRate * Time.deltaTime;
            float wallFuelConsumption = 0f;

            // Extra fuel consumption when colliding with walls while trying to move
            if (isCollidingWithWall && isPlayerTryingToMove)
            {
                wallFuelConsumption = wallCollisionFuelRate * Time.deltaTime;

                if (debugFuel)
                {
                    Debug.Log($"[FuelSystem] Wall collision fuel drain: {wallFuelConsumption:F2}");
                }
            }

            float totalFuelConsumption = baseFuelConsumption + wallFuelConsumption;
            ConsumeFuel(totalFuelConsumption);
        }

        // Update wall collision timer
        if (wallCollisionTimer > 0f)
        {
            wallCollisionTimer -= Time.deltaTime;
            if (wallCollisionTimer <= 0f)
            {
                isCollidingWithWall = false;
            }
        }
    }

    void HandleRefueling()
    {
        if (isInRefuelArea && !isOutOfFuel && !isTeleporting)
        {
            if (!isRefueling)
            {
                isRefueling = true;
                if (debugFuel)
                {
                    Debug.Log("[FuelSystem] Started refueling in start area");
                }
            }

            // Refuel
            float fuelToAdd = refuelRate * Time.deltaTime;
            AddFuel(fuelToAdd);
        }
        else if (isRefueling)
        {
            isRefueling = false;
            if (debugFuel)
            {
                Debug.Log("[FuelSystem] Stopped refueling - left start area");
            }
        }
    }

    void CheckRefuelArea()
    {
        if (startAreaCenter == null)
            return;

        float distanceToStartArea = Vector3.Distance(transform.position, startAreaCenter.position);
        bool wasInRefuelArea = isInRefuelArea;
        isInRefuelArea = distanceToStartArea <= refuelAreaRadius;

        // Trigger events when entering/exiting refuel area
        if (isInRefuelArea != wasInRefuelArea)
        {
            OnRefuelAreaEntered?.Invoke(isInRefuelArea);

            if (debugRefuelArea)
            {
                Debug.Log(
                    $"[FuelSystem] {(isInRefuelArea ? "Entered" : "Exited")} refuel area. Distance: {distanceToStartArea:F2}"
                );
            }
        }
    }

    void HandleTeleportation()
    {
        if (isOutOfFuel && autoTeleportOnEmpty && !isTeleporting)
        {
            teleportTimer += Time.deltaTime;

            if (teleportTimer >= teleportDelay)
            {
                TeleportToStartArea();
            }
        }
    }

    void HandleWarnings()
    {
        bool wasLowFuel = lastFuelAmount <= lowFuelThreshold;
        bool isLowFuel = currentFuel <= lowFuelThreshold;

        if (wasLowFuel != isLowFuel)
        {
            OnLowFuelWarning?.Invoke(isLowFuel);

            if (debugFuel)
            {
                Debug.Log($"[FuelSystem] Low fuel warning: {isLowFuel}");
            }
        }

        lastFuelAmount = currentFuel;
    }

    void ConsumeFuel(float amount)
    {
        currentFuel = Mathf.Max(0f, currentFuel - amount);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (currentFuel <= 0f && !isOutOfFuel)
        {
            isOutOfFuel = true;
            teleportTimer = 0f;
            OnFuelEmpty?.Invoke();

            if (debugFuel)
            {
                Debug.Log("[FuelSystem] Fuel depleted! Preparing to teleport to start area.");
            }

            // Stop player movement
            if (playerController != null)
            {
                playerController.ResetMomentum();
            }
        }
    }

    void AddFuel(float amount)
    {
        float previousFuel = currentFuel;
        currentFuel = Mathf.Min(maxFuel, currentFuel + amount);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        // Check if fuel was fully refilled
        if (previousFuel < maxFuel && currentFuel >= maxFuel)
        {
            OnFuelRefilled?.Invoke();

            if (debugFuel)
            {
                Debug.Log("[FuelSystem] Fuel tank fully refilled!");
            }
        }
    }

    void TeleportToStartArea()
    {
        if (startAreaCenter == null)
            return;

        isTeleporting = true;

        // Reset player physics
        if (playerController != null)
        {
            playerController.ResetMomentum();
        }

        // Teleport to start area
        transform.position = startAreaCenter.position;
        transform.rotation = Quaternion.Euler(new Vector3(0f, -90f, 0f)); // start rotation

        // Refill fuel
        currentFuel = maxFuel;
        isOutOfFuel = false;
        isTeleporting = false;
        teleportTimer = 0f;

        OnFuelChanged?.Invoke(currentFuel, maxFuel);
        OnFuelRefilled?.Invoke();

        // 🔁 Chunk'ları yeniden spawn et
        MapChunkSpawner chunkSpawner = FindObjectOfType<MapChunkSpawner>();
        if (chunkSpawner != null)
        {
            chunkSpawner.SpawnChunks();
        }

        if (debugFuel)
        {
            Debug.Log(
                $"[FuelSystem] Player teleported to start area and refueled. Position: {transform.position}"
            );
        }
    }

    // Called by PlayerController when colliding with walls
    public void OnWallCollision(Collision collision)
    {
        // Check if the collision is with a wall (not ground)
        Vector3 collisionNormal = collision.contacts[0].normal;
        float wallThreshold = 0.7f; // Angle threshold to determine if it's a wall

        if (Mathf.Abs(collisionNormal.y) < wallThreshold) // Not a ground collision
        {
            isCollidingWithWall = true;
            wallCollisionTimer = wallCollisionCooldown;

            if (debugFuel)
            {
                Debug.Log(
                    $"[FuelSystem] Wall collision detected with {collision.gameObject.name}. Normal: {collisionNormal}"
                );
            }
        }
    }

    void LogFuelDebugInfo()
    {
        string refuelStatus = isRefueling ? " [REFUELING]" : "";
        string outOfFuelStatus = isOutOfFuel ? " [OUT OF FUEL]" : "";
        string wallCollisionStatus = isCollidingWithWall ? " [WALL COLLISION]" : "";

        Debug.Log(
            $"[FuelSystem] Fuel: {currentFuel:F1}/{maxFuel} | "
                + $"In Refuel Area: {isInRefuelArea} | "
                + $"Player Moving: {(playerController != null ? playerController.IsMoving() : false)} | "
                + $"{refuelStatus}{outOfFuelStatus}{wallCollisionStatus}"
        );
    }

    // Public methods for external access
    public float GetCurrentFuel() => currentFuel;

    public float GetMaxFuel() => maxFuel;

    public float GetFuelPercentage() => currentFuel / maxFuel;

    public bool IsRefueling() => isRefueling;

    public bool IsOutOfFuel() => isOutOfFuel;

    public bool IsInRefuelArea() => isInRefuelArea;

    public bool IsLowFuel() => currentFuel <= lowFuelThreshold;

    // Methods to modify fuel externally
    public void AddFuelAmount(float amount)
    {
        AddFuel(amount);
    }

    public void ConsumeFuelAmount(float amount)
    {
        ConsumeFuel(amount);
    }

    public void RefillFuel()
    {
        currentFuel = maxFuel;
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
        OnFuelRefilled?.Invoke();
    }

    public void SetFuel(float amount)
    {
        currentFuel = Mathf.Clamp(amount, 0f, maxFuel);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
    }

    // Teleport methods
    public void TeleportToStartAreaManually()
    {
        TeleportToStartArea();
    }

    public void SetStartAreaPosition(Vector3 position)
    {
        if (startAreaCenter != null)
        {
            startAreaCenter.position = position;
        }
    }

    // Debug visualization
    void OnDrawGizmos()
    {
        if (startAreaCenter != null && debugRefuelArea)
        {
            // Draw refuel area
            Gizmos.color = isInRefuelArea ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(startAreaCenter.position, refuelAreaRadius);

            // Draw area boundary on ground
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.3f);
            Vector3 groundPos = new Vector3(
                startAreaCenter.position.x,
                transform.position.y,
                startAreaCenter.position.z
            );
            Gizmos.DrawSphere(groundPos, refuelAreaRadius);

            // Draw line to start area
            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, startAreaCenter.position);
            }
        }

        // Draw fuel indicator above player
        if (Application.isPlaying)
        {
            Vector3 fuelBarPos = transform.position + Vector3.up * 3f;
            float fuelPercentage = GetFuelPercentage();

            // Background bar
            Gizmos.color = Color.red;
            Gizmos.DrawLine(fuelBarPos - Vector3.right, fuelBarPos + Vector3.right);

            // Fuel bar
            Gizmos.color = fuelPercentage > 0.2f ? Color.green : Color.red;
            Vector3 fuelBarEnd = Vector3.Lerp(
                fuelBarPos - Vector3.right,
                fuelBarPos + Vector3.right,
                fuelPercentage
            );
            Gizmos.DrawLine(fuelBarPos - Vector3.right, fuelBarEnd);

            // Out of fuel indicator
            if (isOutOfFuel)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.3f);
            }
        }
    }
}
