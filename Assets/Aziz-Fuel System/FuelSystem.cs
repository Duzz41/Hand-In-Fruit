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

    // Components
    private PlayerController playerController;

    // Fuel state variables
    private bool isInRefuelArea = false;
    private bool isOutOfFuel = false;
    private bool isTeleporting = false;
    private float teleportTimer = 0f;

    [SerializeField]
    private RunIntroManager introManager;

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
        OnFuelChanged?.Invoke(currentFuel, maxFuel); // Trigger initial fuel update
    }

    void SetupStartArea()
    {
        if (startAreaCenter == null)
        {
            GameObject startAreaGO = new GameObject("StartArea");
            startAreaCenter = startAreaGO.transform;
            startAreaCenter.position = transform.position;
        }
    }

    void Update()
    {
        HandleFuelConsumption();
        HandleRefueling();
        CheckRefuelArea();
        HandleTeleportation();
    }

    void HandleFuelConsumption()
    {
        if (isOutOfFuel || isTeleporting)
            return;

        bool isPlayerMoving = playerController != null && playerController.IsMoving();
        if (isPlayerMoving)
        {
            ConsumeFuel(fuelConsumptionRate * Time.deltaTime);
        }
    }

    void HandleRefueling()
    {
        if (isInRefuelArea && !isOutOfFuel && !isTeleporting)
        {
            AddFuel(refuelRate * Time.deltaTime);
        }
    }

    void CheckRefuelArea()
    {
        if (startAreaCenter == null)
            return;

        float distanceToStartArea = Vector3.Distance(transform.position, startAreaCenter.position);
        isInRefuelArea = distanceToStartArea <= refuelAreaRadius;

        if (isInRefuelArea)
        {
            if (introManager != null)
            {
                introManager.StartIntro(); // Start intro sequence
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

    void ConsumeFuel(float amount)
    {
        currentFuel = Mathf.Max(0f, currentFuel - amount);
        OnFuelChanged?.Invoke(currentFuel, maxFuel); // Notify fuel change

        if (currentFuel <= 0f && !isOutOfFuel)
        {
            isOutOfFuel = true;
            teleportTimer = 0f;
            OnFuelEmpty?.Invoke();
        }
    }

    void AddFuel(float amount)
    {
        currentFuel = Mathf.Min(maxFuel, currentFuel + amount);
        OnFuelChanged?.Invoke(currentFuel, maxFuel); // Notify fuel change

        // Check if fuel was fully refilled
        if (currentFuel >= maxFuel)
        {
            OnFuelRefilled?.Invoke();
        }
    }

    void TeleportToStartArea()
    {
        if (startAreaCenter == null)
            return;

        isTeleporting = true;
        transform.position = startAreaCenter.position;

        // Reset fuel and state
        currentFuel = maxFuel;
        isOutOfFuel = false;
        isTeleporting = false;
        teleportTimer = 0f;

        // Yeni dağ oluştur
        if (introManager != null)
        {
            introManager.StartIntro(); // Start intro sequence
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
            // Handle wall collision logic here
        }
    }

    // Subscribe to events
    public void SubscribeToEvents()
    {
        OnFuelEmpty += HandleFuelEmpty;
        OnFuelRefilled += HandleFuelRefilled;
    }

    // Unsubscribe from events
    public void UnsubscribeFromEvents()
    {
        OnFuelEmpty -= HandleFuelEmpty;
        OnFuelRefilled -= HandleFuelRefilled;
    }

    private void HandleFuelEmpty()
    {
        // Handle fuel empty logic here
        Debug.Log("Fuel is empty!");
    }

    private void HandleFuelRefilled()
    {
        // Handle fuel refilled logic here
        Debug.Log("Fuel has been refilled!");
    }
}
