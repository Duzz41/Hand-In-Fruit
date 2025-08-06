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
    private bool showRefuelAreaGizmo = true; // Refuel alanını göster/gizle

    [SerializeField]
    private Color refuelAreaColor = Color.green; // Refuel alanının rengi

    [SerializeField]
    private Color refuelAreaColorWhenInside = Color.yellow; // İçindeyken rengi

    // Components
    private PlayerController playerController;

    [SerializeField]
    GameObject mountainModel;

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
        {
            Debug.Log(
                $"Fuel consumption blocked - OutOfFuel: {isOutOfFuel}, Teleporting: {isTeleporting}"
            );
            return;
        }

        bool isPlayerMoving = playerController != null && playerController.IsMoving();
        Debug.Log($"Player moving: {isPlayerMoving}, Current fuel: {currentFuel}");

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
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (currentFuel <= 0f && !isOutOfFuel)
        {
            isOutOfFuel = true;
            teleportTimer = 0f;
            OnFuelEmpty?.Invoke();
            // OnFuelRefilled?.Invoke(); // Bu satırı kaldırın!
        }
    }

    public void SetMaxFuel(float newMax)
    {
        maxFuel = newMax;
        currentFuel = Mathf.Min(currentFuel, newMax); // Aşımı önler
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
            if (introManager.firstIntro != false)
                introManager.SpawnChunks(); // Start intro sequence
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

    // Refuel alanını Scene view'da görselleştir
    void OnDrawGizmosSelected()
    {
        if (!showRefuelAreaGizmo || startAreaCenter == null)
            return;

        // Rengi ayarla - oyuncu içindeyse farklı renk
        Color gizmoColor = isInRefuelArea ? refuelAreaColorWhenInside : refuelAreaColor;
        gizmoColor.a = 0.3f; // Şeffaflık

        Gizmos.color = gizmoColor;

        // Refuel alanını çember olarak çiz
        Gizmos.DrawSphere(startAreaCenter.position, refuelAreaRadius);

        // Kenar çizgisi için daha koyu renk
        gizmoColor.a = 0.8f;
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(startAreaCenter.position, refuelAreaRadius);

        // Merkezi işaretle
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(startAreaCenter.position, Vector3.one * 0.5f);
    }

    // Her zaman görünmesi için (opsiyonel)
    void OnDrawGizmos()
    {
        if (!showRefuelAreaGizmo || startAreaCenter == null)
            return;

        // Sadece kenar çizgisini çiz (seçili değilken)
        Gizmos.color = new Color(refuelAreaColor.r, refuelAreaColor.g, refuelAreaColor.b, 0.2f);
        Gizmos.DrawWireSphere(startAreaCenter.position, refuelAreaRadius);
    }
}
