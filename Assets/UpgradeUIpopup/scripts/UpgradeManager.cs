using UnityEngine;
using UnityEngine.UI; // Image sýnýfý için gerekli
using TMPro; // TextMeshPro için gerekli

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
    public float currentFuel = 100f; // float kullandým çünkü benzin ondalýklý olabilir

    [Tooltip("Aracýn maksimum benzin kapasitesi.")]
    public float maxFuelCapacity = 100f; // Aracýn maksimum benzin kapasitesi

    [Tooltip("Benzin tüketim hýzý (birim zamanda ne kadar benzin harcandýðý).")]
    public float fuelConsumptionRate = 1f; // Örnek: Saniyede 1 birim benzin

    [Header("Mine & Inventory Settings")]
    [Tooltip("Oyuncunun envanterindeki toplanmýþ maden sayýsý.")]
    public int collectedMinesCount = 0;

    [Tooltip("Her bir madenin satýþ deðeri.")]
    public int mineValue = 10;

    [Header("UI References")]
    [Tooltip("Oyundaki global para göstergesi TextMeshPro objesi. Yoksa boþ býrakýlabilir.")]
    public TMP_Text globalMoneyText;

    [Tooltip("Benzin doldurma çubuðunun dolu kýsmý (Image bileþeni).")]
    public Image fuelBarFillImage;

    [Tooltip("Benzin miktarýný sayýsal olarak gösteren TextMeshPro objesi.")]
    public TMP_Text fuelAmountText;

    void Awake()
    {
        // Singleton uygulamasýnýn temel mantýðý
        if (Instance == null)
        {
            Instance = this;
            // Bu objeyi sahneler arasý koru. Genellikle GameManager veya ana sistemler için kullanýlýr.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Eðer zaten bir instance varsa, bu yeni objeyi yok et ve yok etmeden önce hata mesajý ver.
            Debug.LogWarning("Sahneye birden fazla UpgradeManager eklenmeye çalýþýldý! Mevcut olan korunacak, yeni olan yok ediliyor.", this);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Oyun baþladýðýnda UI'larý güncelle
        UpdateGlobalMoneyUI();
        UpdateFuelUI();
    }

    // UPDATE METODU: Benzin tüketimi için (örnek)
    void Update()
    {
        // Sadece oyun oynanýrken (oyun duraklatýlmamýþsa, araç hareket ediyorsa vb.) benzin tüketimi
        // if (Time.timeScale > 0 && IsVehicleMoving()) // IsVehicleMoving kendi aracýnýzýn hareketini kontrol eden bir metot olabilir
        // {
        ConsumeFuel(fuelConsumptionRate * Time.deltaTime);
        // }
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
        int earned = collectedMinesCount * mineValue;
        AddMoney(earned); // Kazanýlan parayý oyuncunun bakiyesine ekle
        collectedMinesCount = 0; // Madenleri sýfýrla
        Debug.Log($"Tüm madenler satýldý. Kazanýlan: {earned} para.");
        return earned;
    }

    // UPGRADE METOTLARI
    /// <summary>
    /// Delme gücü seviyesini yükseltir.
    /// </summary>
    public void UpgradeDrillPower()
    {
        // Örnek: Maliyet seviyeye göre artar
        int cost = drillPowerLevel * 100; // Her seviye için maliyet artýþý
        if (SpendMoney(cost))
        {
            drillPowerLevel++;
            Debug.Log($"Delme Gücü Seviye {drillPowerLevel}'e yükseltildi.");
            // Burada Upgrade UI'ýný da güncelleyecek bir metot çaðrýlabilir.
            // Örneðin: UIManager.Instance.UpdateUpgradeScreen();
        }
    }

    /// <summary>
    /// X-Ray görüþ özelliðini açar veya kapatýr.
    /// </summary>
    /// <param name="activate">X-Ray görüþünün aktif olup olmayacaðý.</param>
    public void ToggleXRayVision(bool activate)
    {
        // X-Ray görüþünün maliyeti veya bir kilit sistemi olabilir.
        // if (SpendMoney(xrayCost) && !hasXRayVision)
        // {
        hasXRayVision = activate;
        Debug.Log($"X-Ray Görüþ: {(hasXRayVision ? "Aktif" : "Deaktif")}");
        // Burada X-Ray görsel efektini açýp kapatan veya güncelleyen kodu çaðýrabilirsiniz.
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
            Debug.LogWarning("Benzin bitti! Araç durdu veya hasar alýyor.");
            // Burada aracýn durmasý, hasar almasý veya oyunu bitirme gibi
            // benzin bitince olacak olaylarý tetikleyebilirsiniz.
        }
        UpdateFuelUI(); // UI güncellemesini çaðýr
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
        UpdateFuelUI(); // UI güncellemesini çaðýr
        Debug.Log($"Benzin Eklendi: {amount}. Güncel Benzin: {currentFuel}");
    }

    /// <summary>
    /// Maksimum benzin kapasitesini yükseltir (upgrade için).
    /// </summary>
    /// <param name="newCapacity">Yeni maksimum benzin kapasitesi.</param>
    public void UpgradeMaxFuelCapacity(float newCapacity)
    {
        // Örnek: Upgrade maliyetini kontrol et
        // int upgradeCost = CalculateFuelCapacityUpgradeCost();
        // if (SpendMoney(upgradeCost)) {
        maxFuelCapacity = newCapacity;
        // Eðer currentFuel yeni kapasitenin üstündeyse, yeni kapasiteye eþitle
        if (currentFuel > maxFuelCapacity)
        {
            currentFuel = maxFuelCapacity;
        }
        UpdateFuelUI(); // UI güncellemesini çaðýr
        Debug.Log($"Maksimum Benzin Kapasitesi {maxFuelCapacity}'e yükseltildi.");
        // }
    }


    // UI GÜNCELLEME METOTLARI
    /// <summary>
    /// Global para göstergesi UI TextMeshPro'sunu günceller.
    /// </summary>
    void UpdateGlobalMoneyUI()
    {
        if (globalMoneyText != null)
        {
            globalMoneyText.text = "Para: " + currentMoney.ToString();
        }
    }

    /// <summary>
    /// Hem benzin çubuðunu hem de sayýsal benzin metnini günceller.
    /// </summary>
    void UpdateFuelUI()
    {
        float fuelPercentage = currentFuel / maxFuelCapacity;

        if (fuelBarFillImage != null)
        {
            fuelBarFillImage.fillAmount = fuelPercentage;
        }

        if (fuelAmountText != null)
        {
            // Sayýsal deðeri göster (örn: 75/100)
            fuelAmountText.text = $"Benzin: {currentFuel:F0}/{maxFuelCapacity:F0}"; // F0: Ondalýk basamak yok

            // Veya sadece yüzde olarak:
            // fuelAmountText.text = $"Benzin: {fuelPercentage * 100:F0}%";
        }
    }
}