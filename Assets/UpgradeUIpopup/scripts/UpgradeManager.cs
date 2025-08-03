using UnityEngine;
using UnityEngine.UI; // Image bileþeni için gerekli
using TMPro; // TextMeshPro bileþeni için gerekli

public class UpgradeManager : MonoBehaviour
{
    // Singleton deseni: UpgradeManager'a oyunun herhangi bir yerinden kolayca eriþim saðlar.
    // Sadece bir instance olmasýný garanti eder.
    public static UpgradeManager Instance { get; private set; }

    [Header("Player Stats")]
    [Tooltip("Oyuncunun þu anki para miktarý.")]
    public int currentMoney = 0;

    [Tooltip("Aracýn delme gücü seviyesi.")]
    public int drillPowerLevel = 1;

    [Tooltip("Aracýn X-Ray görüþ özelliðinin aktif olup olmadýðý.")]
    public bool hasXRayVision = false;

    [Header("Vehicle Fuel Settings")]
    [Tooltip("Aracýn þu anki benzin miktarý.")]
    public float currentFuel = 100f;

    [Tooltip("Aracýn maksimum benzin kapasitesi.")]
    public float maxFuelCapacity = 100f;

    [Tooltip("Benzin tüketim hýzý (birim zamanda ne kadar benzin harcandýðý).")]
    public float fuelConsumptionRate = 1f;

    [Header("Mine & Inventory Settings")]
    [Tooltip("Oyuncunun envanterindeki toplanmýþ maden sayýsý.")]
    public int collectedMinesCount = 0;

    [Tooltip("Her bir madenin satýþ deðeri.")]
    public int mineValue = 10;

    // UI referanslarý artýk UIManager tarafýndan atanacak ve private olarak tutulacak
    private TMP_Text globalMoneyText;
    private Image fuelBarFillImage;
    private TMP_Text fuelAmountText;
    public TMP_Text DrillLevelText;
    void Awake()
    {
        // Singleton uygulamasýnýn temel mantýðý
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Sahneye birden fazla UpgradeManager eklenmeye çalýþýldý! Mevcut olan korunacak, yeni olan yok ediliyor.", this);
            Destroy(gameObject);
        }
    }

    // === YENÝ METOT: UIManager'ýn UI referanslarýný atamasý için ===
    /// <summary>
    /// UIManager tarafýndan çaðrýlarak global UI referanslarýnýn UpgradeManager'a atanmasýný saðlar.
    /// </summary>
    public void SetUIRefs(TMP_Text moneyText, Image fuelBarImage, TMP_Text fuelText)
    {
        globalMoneyText = moneyText;
        fuelBarFillImage = fuelBarImage;
        fuelAmountText = fuelText;
    }

    void Update()
    {
        // Benzin tüketimi devam ediyor
        ConsumeFuel(fuelConsumptionRate * Time.deltaTime);
    }

    /// <summary>
    /// Oyuncuya para ekler ve UI'ý günceller.
    /// </summary>
    /// <param name="amount">Eklenecek para miktarý.</param>
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateGlobalMoneyUI();
        Debug.Log($"Para Eklendi: {amount}. Toplam Para: {currentMoney}");
    }

    /// <summary>
    /// Oyuncudan para düþer. Yeterli para yoksa false döndürür.
    /// </summary>
    /// <param name="amount">Harcanacak para miktarý.</param>
    /// <returns>Para harcanabildiyse true, aksi takdirde false.</returns>
    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateGlobalMoneyUI();
            Debug.Log($"Para Harcandý: {amount}. Kalan Para: {currentMoney}");
            return true;
        }
        Debug.Log("Yeterli para yok!");
        return false;
    }

    /// <summary>
    /// Oyuncunun envanterine maden ekler.
    /// </summary>
    /// <param name="count">Eklenecek maden sayýsý (varsayýlan: 1).</param>
    public void CollectMine(int count = 1)
    {
        collectedMinesCount += count;
        Debug.Log($"Maden Toplandý. Toplam Maden: {collectedMinesCount}");
    }

    /// <summary>
    /// Oyuncunun tüm madenlerini satar ve kazanýlan parayý döndürür.
    /// </summary>
    /// <returns>Maden satýþýndan kazanýlan toplam para.</returns>
    public int SellAllMines()
    {
        int stonenvalue = InventoryManager.Instance.GetResourceCount("Stone") * 50;
        int ýronvalue = InventoryManager.Instance.GetResourceCount("Iron") * 100;
        //int ýronvalue = InventoryManager.Instance.GetResourceCount("Gold") * 5;

        int earned = stonenvalue + ýronvalue;
        AddMoney(earned);
        collectedMinesCount = 0;
        Debug.Log($"Tüm madenler satýldý. Kazanýlan: {earned} para.");
        return earned;
    }

    // UPGRADE METOTLARI
    /// <summary>
    /// Delme gücü seviyesini yükseltir.
    /// </summary>
    public void UpgradeDrillPower()
    {
        int cost = drillPowerLevel * 100;
        if (SpendMoney(cost))
        {
            drillPowerLevel++;
            DrillLevelText.text = "DRIL LEVEL" + drillPowerLevel;
            Debug.Log($"Delme Gücü Seviye {drillPowerLevel}'e yükseltildi.");
            // UIManager'daki UI güncelleme metodunu çaðýr (varsayýmsal)
            //UIManager.Instance?.UpdateDrillUpgradeUI(); 
        }
    }

    /// <summary>
    /// X-Ray görüþ özelliðini açar veya kapatýr.
    /// </summary>
    /// <param name="activate">X-Ray görüþünün aktif olup olmayacaðý.</param>
    public void ToggleXRayVision(bool activate)
    {
        // Maliyet ve kontrol eklenebilir
        // if (!hasXRayVision && SpendMoney(xrayUpgradeCost)) {
        hasXRayVision = activate;
        Debug.Log($"X-Ray Görüþ: {(hasXRayVision ? "Aktif" : "Deaktif")}");
        // UIManager.Instance?.UpdateXRayUpgradeUI();
        // }
    }

    /// <summary>
    /// Belirtilen miktarda benzin tüketir.
    /// </summary>
    /// <param name="amount">Tüketilecek benzin miktarý.</param>
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

    /// <summary>
    /// Belirtilen miktarda benzin ekler (depo kapasitesini aþmaz).
    /// </summary>
    /// <param name="amount">Eklenecek benzin miktarý.</param>
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

    /// <summary>
    /// Maksimum benzin kapasitesini yükseltir (upgrade için).
    /// </summary>
    /// <param name="newCapacity">Yeni maksimum benzin kapasitesi.</param>
    public void UpgradeMaxFuelCapacity(float newCapacity)
    {
        // Maliyet ve kontrol eklenebilir
        // int upgradeCost = CalculateFuelCapacityUpgradeCost();
        // if (SpendMoney(upgradeCost)) {
        maxFuelCapacity = newCapacity;
        if (currentFuel > maxFuelCapacity)
        {
            currentFuel = maxFuelCapacity;
        }
        UpdateFuelUI();
        Debug.Log($"Maksimum Benzin Kapasitesi {maxFuelCapacity}'e yükseltildi.");
        // UIManager.Instance?.UpdateFuelUpgradeUI();
        // }
    }


    // UI GÜNCELLEME METOTLARI
    // Bu metotlar private referanslarý kullanarak UI'ý günceller.
    public void UpdateGlobalMoneyUI()
    {
        if (globalMoneyText != null)
        {
            globalMoneyText.text = currentMoney.ToString();
        }
    }

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