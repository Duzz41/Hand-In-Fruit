using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    public int currentMoney = 0;
    public int drillPowerLevel = 1;
    public bool hasXRayVision = false;

    public float currentFuel = 100f;
    public float maxFuelCapacity = 100f;

    public int collectedMinesCount = 0;
    public int mineValue = 10;
    public TMP_Text DrillCostText;
    public TMP_Text FuelCapacityText;
    public TMP_Text XRayStatusText;

    private TMP_Text globalMoneyText;
    public TMP_Text DrillLevelText;

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

    public void UpgradeDrillPower()
    {
        int cost = drillPowerLevel * 100;
        if (SpendMoney(cost))
        {
            drillPowerLevel++;
            UpdateDrillUI();
        }
    }

    public void ToggleXRayVision(bool activate)
    {
        hasXRayVision = activate;
    }

    public void UpgradeMaxFuelCapacity(float newCapacity)
    {
        if (newCapacity > maxFuelCapacity)
        {
            maxFuelCapacity = newCapacity;
            UpdateFuelUI();
        }
    }

    public void UpdateGlobalMoneyUI()
    {
        if (globalMoneyText != null)
        {
            globalMoneyText.text = currentMoney.ToString();
        }
    }

    public void UpdateDrillUI()
    {
        if (DrillLevelText != null)
            DrillLevelText.text = "DRILL LEVEL " + drillPowerLevel;

        if (DrillCostText != null)
            DrillCostText.text = "Upgrade Cost: " + (drillPowerLevel * 100) + "$";
    }

    public void UpdateFuelUI()
    {
        if (FuelCapacityText != null)
            FuelCapacityText.text = "MAX FUEL: " + maxFuelCapacity + "L";
    }

    public void UpdateXRayUI()
    {
        if (XRayStatusText != null)
            XRayStatusText.text = hasXRayVision ? "X-RAY: ON" : "X-RAY: OFF";
    }
}
