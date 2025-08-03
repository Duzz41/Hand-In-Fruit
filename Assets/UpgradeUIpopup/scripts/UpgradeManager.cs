using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("Player Stats")]
    // Oyuncunun para miktarýný 0'dan baþlatýr
    public int currentMoney = 0;
    public int drillPowerLevel = 1;
    public bool hasXRayVision = false;

    [Header("Vehicle Fuel Settings")]
    public float currentFuel = 100f;
    public float maxFuelCapacity = 100f;
    public float fuelConsumptionRate = 1f;

    [Header("Resource Values")]
    [Tooltip("Farklý maden türlerinin satýþ deðerleri.")]
    public Dictionary<string, int> resourceValues = new Dictionary<string, int>();

    // UI referanslarý UIManager tarafýndan atanacak
    private TMP_Text globalMoneyText;
    private Image fuelBarFillImage;
    private TMP_Text fuelAmountText;
    private TMP_Text drillLevelText; // Yeni eklenen referans
    private TMP_Text drillCostText; // Sondaj yükseltme pop-up'ýndaki metin
    private TMP_Text nextDrillPowerText; // Sondaj yükseltme pop-up'ýndaki metin

    void Awake()
    {
        // Singleton deseni
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Maden deðerlerini tanýmlar
        resourceValues.Add("Stone", 50);
        resourceValues.Add("Iron", 100);
        resourceValues.Add("Gold", 200);
    }

    // UIManager'dan tüm UI referanslarýný alýr
    public void SetUIRefs(TMP_Text moneyText, Image fuelBarImage, TMP_Text fuelText, TMP_Text drillText, TMP_Text drillCost, TMP_Text nextDrillPower)
    {
        globalMoneyText = moneyText;
        fuelBarFillImage = fuelBarImage;
        fuelAmountText = fuelText;
        drillLevelText = drillText;
        drillCostText = drillCost;
        nextDrillPowerText = nextDrillPower;

        // Baþlangýçta tüm UI'larý günceller
        UpdateGlobalMoneyUI();
        UpdateFuelUI();
        UpdateDrillLevelUI();
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateGlobalMoneyUI();
    }

    // Harcama iþlemi yapar ve paranýn yeterli olup olmadýðýný kontrol eder
    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateGlobalMoneyUI();
            return true;
        }
        Debug.Log("Yeterli para yok!");
        return false;
    }

    // Envanterdeki tüm madenleri satar ve para kazanýr
    public int SellAllResources()
    {
        int earnedMoney = 0;

        if (InventoryManager.Instance != null)
        {
            Dictionary<string, int> currentInventory = InventoryManager.Instance.GetAllResources();

            foreach (var resource in currentInventory)
            {
                string resourceType = resource.Key;
                int resourceCount = resource.Value;

                if (resourceValues.ContainsKey(resourceType))
                {
                    earnedMoney += resourceCount * resourceValues[resourceType];
                }
            }

            AddMoney(earnedMoney);
            InventoryManager.Instance.ClearInventory();
            Debug.Log($"Tüm madenler satýldý. Kazanýlan: {earnedMoney} para.");
        }
        else
        {
            Debug.LogError("InventoryManager bulunamadý. Satýþ iþlemi yapýlamýyor.");
        }

        return earnedMoney;
    }

    // Delme gücünü yükseltme metodu
    public void UpgradeDrillPower()
    {
        int cost = 300;
        if (SpendMoney(cost))
        {
            drillPowerLevel++;
            UpdateDrillLevelUI();
            Debug.Log($"Delme Gücü Seviye {drillPowerLevel}'e yükseltildi.");
        }
    }

    // X-Ray görüþü açma/kapama metodu
    public void ToggleXRayVision(bool activate)
    {
        int cost = 300;
        if (!hasXRayVision && SpendMoney(cost))
        {
            hasXRayVision = activate;
            Debug.Log($"X-Ray Görüþ: {(hasXRayVision ? "Aktif" : "Deaktif")}");
        }
    }

    // Yakýt kapasitesini yükseltme metodu
    public void UpgradeMaxFuelCapacity()
    {
        int cost = 300;
        if (SpendMoney(cost))
        {
            maxFuelCapacity += 25f;
            if (currentFuel > maxFuelCapacity)
            {
                currentFuel = maxFuelCapacity;
            }
            UpdateFuelUI();
            Debug.Log($"Maksimum Benzin Kapasitesi {maxFuelCapacity}'e yükseltildi.");
        }
    }

    // Yakýt tüketimi ve eklenmesi
    public void ConsumeFuel(float amount)
    {
        currentFuel -= amount;
        if (currentFuel <= 0)
        {
            currentFuel = 0;
            Debug.LogWarning("Benzin bitti! Araç durdu.");
        }
        UpdateFuelUI();
    }

    public void AddFuel(float amount)
    {
        currentFuel += amount;
        if (currentFuel > maxFuelCapacity)
        {
            currentFuel = maxFuelCapacity;
        }
        UpdateFuelUI();
        Debug.Log($"Benzin Eklendi: {amount}. Güncel Benzin: {currentFuel}");
    }

    // Para miktarýný UI'da günceller
    public void UpdateGlobalMoneyUI()
    {
        if (globalMoneyText != null)
        {
            globalMoneyText.text = currentMoney.ToString();
        }
    }

    // Sondaj seviyesini UI'da günceller
    public void UpdateDrillLevelUI()
    {
        if (drillLevelText != null)
        {
            drillLevelText.text = $"DRILL LEVEL {drillPowerLevel}";
        }
    }

    // Yakýt çubuðunu ve metnini günceller
    public void UpdateFuelUI()
    {
        float fuelPercentage = currentFuel / maxFuelCapacity;
        if (fuelBarFillImage != null)
        {
            fuelBarFillImage.fillAmount = fuelPercentage;
        }
        if (fuelAmountText != null)
        {
            fuelAmountText.text = $"Benzin: {currentFuel:F0}/{maxFuelCapacity:F0}";
        }
    }
}