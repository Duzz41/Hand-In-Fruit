using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    // === Referanslar ===
    [Header("Referanslar")]
    public DrillForceBuildup drillForceBuildup;
    public FuelSystem fuelSystem;
    public FogOfWarManager fogOfWarManager;

    // === Para Sistemi ===
    public int currentMoney = 0;

    // === Drill Güç Sistemi ===
    public int drillPowerLevel = 1;

    // === Fuel Sistemi ===
    [Header("Fuel Settings")]
    [Space(10)]
    public float currentFuel = 100f;
    public float maxFuelCapacity = 100f;
    public int fuelUpgradeLevel = 0;
    public int baseFuelCost = 100;
    public float fuelIncreasePerLevel = 25f;

    [Header("X-RAY Upgrade Settings")]
    [Space(10)]
    public int xrayLevel = 0;

    [Tooltip("İlk seviye için temel ücret")]
    public int baseXRayCost = 150;

    [Tooltip("Her seviye başına süredeki artış (saniye)")]
    public float xrayDurationIncreasePerLevel = 2f;

    [Tooltip("X-Ray toplam süresi")]
    public float xrayDuration = 0f;

    // === Maden Sistemi ===
    public int collectedMinesCount = 0;
    public int mineValue = 10;

    // === UI Referansları ===
    public TMP_Text DrillCostText;
    public TMP_Text FuelCapacityText;
    public TMP_Text XRayStatusText;
    public TMP_Text DrillLevelText;
    private TMP_Text globalMoneyText;

    // === Singleton ===
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (drillForceBuildup != null)
            drillForceBuildup.forceIncreaseRate = 500f + (drillPowerLevel - 1) * 100f;

        if (fuelSystem != null)
            fuelSystem.SetMaxFuel(maxFuelCapacity);

        // Eğer daha önce seviye varsa süreyi baştan ayarla
        if (xrayLevel > 0)
        {
            xrayDuration = xrayLevel * xrayDurationIncreasePerLevel;

            if (fogOfWarManager != null)
                fogOfWarManager.xrayDuration = xrayDuration;
        }

        UpdateXRayUI();

        UpdateFuelUI();
    }

    // === Genel Para Fonksiyonları ===
    public void SetUIRefs(TMP_Text moneyText)
    {
        globalMoneyText = moneyText;
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateGlobalMoneyUI();
    }

    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateGlobalMoneyUI();
            return true;
        }
        return false;
    }

    public void UpdateGlobalMoneyUI()
    {
        if (globalMoneyText != null)
            globalMoneyText.text = currentMoney.ToString();
    }

    // === Maden Toplama / Satış ===
    public void CollectMine(int count = 1)
    {
        collectedMinesCount += count;
    }

    public int SellAllMines()
    {
        int stonenvalue = InventoryManager.Instance.GetResourceCount("Stone") * 50;
        int ironvalue = InventoryManager.Instance.GetResourceCount("Iron") * 100;
        int earned = stonenvalue + ironvalue;
        AddMoney(earned);
        collectedMinesCount = 0;
        return earned;
    }

    // === Drill Upgrade ===
    public void UpgradeDrillPower()
    {
        int cost = drillPowerLevel * 100;
        if (SpendMoney(cost))
        {
            drillPowerLevel++;

            if (drillForceBuildup != null)
                drillForceBuildup.forceIncreaseRate = 500f + (drillPowerLevel - 1) * 100f;

            UpdateDrillUI();
        }
    }

    public void UpdateDrillUI()
    {
        if (DrillLevelText != null)
            DrillLevelText.text = "DRILL LEVEL " + drillPowerLevel;

        if (DrillCostText != null)
            DrillCostText.text = "Upgrade Cost: " + (drillPowerLevel * 100) + "$";
    }

    // === Fuel Upgrade ===
    public void UpgradeFuelCapacity()
    {
        int cost = (fuelUpgradeLevel + 1) * baseFuelCost;
        float newCapacity = maxFuelCapacity + fuelIncreasePerLevel;

        if (SpendMoney(cost))
        {
            fuelUpgradeLevel++;
            maxFuelCapacity = newCapacity;

            if (fuelSystem != null)
                fuelSystem.SetMaxFuel(maxFuelCapacity);

            UpdateFuelUI();
        }
    }

    public void UpdateFuelUI()
    {
        if (FuelCapacityText != null)
        {
            FuelCapacityText.text =
                $"MAX FUEL: {maxFuelCapacity}L\n"
                + $"LEVEL: {fuelUpgradeLevel} | Next: {(fuelUpgradeLevel + 1) * baseFuelCost}$";
        }
    }

    // === X-Ray Upgrade ===
    public void UpgradeXRayVision()
    {
        int upgradeCost = (xrayLevel + 1) * baseXRayCost;

        if (SpendMoney(upgradeCost))
        {
            xrayLevel++;
            xrayDuration = xrayLevel * xrayDurationIncreasePerLevel;

            if (fogOfWarManager != null)
            {
                fogOfWarManager.xrayDuration = xrayDuration;
            }

            UpdateXRayUI();
        }
        else
        {
            Debug.Log("Yeterli paran yok paşam!");
        }
    }

    public void UpdateXRayUI()
    {
        if (XRayStatusText != null)
        {
            int nextCost = (xrayLevel + 1) * baseXRayCost;

            XRayStatusText.text =
                $"<b>X-RAY LEVEL:</b> {xrayLevel}\n"
                + $"<b>Duration:</b> {xrayDuration:F1} s\n"
                + $"<b>Next Upgrade:</b> {nextCost}$";
        }
    }
}
