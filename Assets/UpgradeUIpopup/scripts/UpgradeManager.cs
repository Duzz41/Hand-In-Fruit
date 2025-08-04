using TMPro; // TextMeshPro bile�eni i�in gerekli
using UnityEngine;
using UnityEngine.UI; // Image bile�eni i�in gerekli

public class UpgradeManager : MonoBehaviour
{
    // Singleton deseni: UpgradeManager'a oyunun herhangi bir yerinden kolayca eri�im sa�lar.
    // Sadece bir instance olmas�n� garanti eder.
    public static UpgradeManager Instance { get; private set; }

    [Header("Player Stats")]
    [Tooltip("Oyuncunun �u anki para miktar�.")]
    public int currentMoney = 0;

    [Tooltip("Arac�n delme g�c� seviyesi.")]
    public int drillPowerLevel = 1;

    [Tooltip("Arac�n X-Ray g�r�� �zelli�inin aktif olup olmad���.")]
    public bool hasXRayVision = false;

    [Header("Vehicle Fuel Settings")]
    [Tooltip("Arac�n �u anki benzin miktar�.")]
    public float currentFuel = 100f;

    [Tooltip("Arac�n maksimum benzin kapasitesi.")]
    public float maxFuelCapacity = 100f;

    [Header("Mine & Inventory Settings")]
    [Tooltip("Oyuncunun envanterindeki toplanm�� maden say�s�.")]
    public int collectedMinesCount = 0;

    [Tooltip("Her bir madenin sat�� de�eri.")]
    public int mineValue = 10;

    // UI referanslar� art�k UIManager taraf�ndan atanacak ve private olarak tutulacak
    private TMP_Text globalMoneyText;
    private Image fuelBarFillImage;
    private TMP_Text fuelAmountText;
    public TMP_Text DrillLevelText;

    void Awake()
    {
        // Singleton uygulamas�n�n temel mant���
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning(
                "Sahneye birden fazla UpgradeManager eklenmeye �al���ld�! Mevcut olan korunacak, yeni olan yok ediliyor.",
                this
            );
            Destroy(gameObject);
        }
    }

    // === YEN� METOT: UIManager'�n UI referanslar�n� atamas� i�in ===
    /// <summary>
    /// UIManager taraf�ndan �a�r�larak global UI referanslar�n�n UpgradeManager'a atanmas�n� sa�lar.
    /// </summary>
    public void SetUIRefs(TMP_Text moneyText)
    {
        globalMoneyText = moneyText;
    }

    /// <summary>
    /// Oyuncuya para ekler ve UI'� g�nceller.
    /// </summary>
    /// <param name="amount">Eklenecek para miktar�.</param>
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        UpdateGlobalMoneyUI();
        Debug.Log($"Para Eklendi: {amount}. Toplam Para: {currentMoney}");
    }

    /// <summary>
    /// Oyuncudan para d��er. Yeterli para yoksa false d�nd�r�r.
    /// </summary>
    /// <param name="amount">Harcanacak para miktar�.</param>
    /// <returns>Para harcanabildiyse true, aksi takdirde false.</returns>
    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            UpdateGlobalMoneyUI();
            Debug.Log($"Para Harcand�: {amount}. Kalan Para: {currentMoney}");
            return true;
        }
        Debug.Log("Yeterli para yok!");
        return false;
    }

    /// <summary>
    /// Oyuncunun envanterine maden ekler.
    /// </summary>
    /// <param name="count">Eklenecek maden say�s� (varsay�lan: 1).</param>
    public void CollectMine(int count = 1)
    {
        collectedMinesCount += count;
        Debug.Log($"Maden Topland�. Toplam Maden: {collectedMinesCount}");
    }

    /// <summary>
    /// Oyuncunun t�m madenlerini satar ve kazan�lan paray� d�nd�r�r.
    /// </summary>
    /// <returns>Maden sat���ndan kazan�lan toplam para.</returns>
    public int SellAllMines()
    {
        int stonenvalue = InventoryManager.Instance.GetResourceCount("Stone") * 50;
        int ironvalue = InventoryManager.Instance.GetResourceCount("Iron") * 100;
        //int �ronvalue = InventoryManager.Instance.GetResourceCount("Gold") * 5;

        int earned = stonenvalue + ironvalue;
        AddMoney(earned);
        collectedMinesCount = 0;
        Debug.Log($"T�m madenler sat�ld�. Kazan�lan: {earned} para.");
        return earned;
    }

    // UPGRADE METOTLARI
    /// <summary>
    /// Delme g�c� seviyesini y�kseltir.
    /// </summary>
    public void UpgradeDrillPower()
    {
        int cost = drillPowerLevel * 100;
        if (SpendMoney(cost))
        {
            drillPowerLevel++;
            DrillLevelText.text = "DRIL LEVEL" + drillPowerLevel;
            Debug.Log($"Delme G�c� Seviye {drillPowerLevel}'e y�kseltildi.");
            // UIManager'daki UI g�ncelleme metodunu �a��r (varsay�msal)
            //UIManager.Instance?.UpdateDrillUpgradeUI();
        }
    }

    /// <summary>
    /// X-Ray g�r�� �zelli�ini a�ar veya kapat�r.
    /// </summary>
    /// <param name="activate">X-Ray g�r���n�n aktif olup olmayaca��.</param>
    public void ToggleXRayVision(bool activate)
    {
        // Maliyet ve kontrol eklenebilir
        // if (!hasXRayVision && SpendMoney(xrayUpgradeCost)) {
        hasXRayVision = activate;
        Debug.Log($"X-Ray G�r��: {(hasXRayVision ? "Aktif" : "Deaktif")}");
        // UIManager.Instance?.UpdateXRayUpgradeUI();
        // }
    }

    /// <summary>
    /// Maksimum benzin kapasitesini y�kseltir (upgrade i�in).
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
        Debug.Log($"Maksimum Benzin Kapasitesi {maxFuelCapacity}'e y�kseltildi.");
        // UIManager.Instance?.UpdateFuelUpgradeUI();
        // }
    }

    // UI G�NCELLEME METOTLARI
    // Bu metotlar private referanslar� kullanarak UI'� g�nceller.
    public void UpdateGlobalMoneyUI()
    {
        if (globalMoneyText != null)
        {
            globalMoneyText.text = currentMoney.ToString();
        }
    }
}
