using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject sellPopupPanel;
    public GameObject upgradeScreenPanel;
    public GameObject drillUpgradePopup;
    public GameObject xrayUpgradePopup;
    public GameObject fuelUpgradePopup;

    [Header("Global In-Game UI Elements")]
    public TMP_Text globalMoneyText;
    public Image fuelBarFillImage;
    public TMP_Text fuelAmountText;

    [Header("Drill Upgrade UI Elements")]
    public TMP_Text drillLevelText; // Yeni eklenen referans
    public TMP_Text drillCostText;
    public TMP_Text nextDrillPowerText;

    // ... Diðer upgrade UI elemanlarý buraya eklenebilir

    void Start()
    {
        // Tüm UI referanslarýný UpgradeManager'a gönder
        UpgradeManager.Instance.SetUIRefs(globalMoneyText, fuelBarFillImage, fuelAmountText, drillLevelText, drillCostText, nextDrillPowerText);
    }

    // Pop-up'larý açma/kapama metotlarý
    public void OpenUpgradePopup(string popupType)
    {
        upgradeScreenPanel.SetActive(true);
        if (popupType == "Drill")
        {
            drillUpgradePopup.SetActive(true);
        }
        else if (popupType == "Xray")
        {
            xrayUpgradePopup.SetActive(true);
        }
        else if (popupType == "Fuel")
        {
            fuelUpgradePopup.SetActive(true);
        }
    }

    public void CloseUpgradePopup()
    {
        upgradeScreenPanel.SetActive(false);
        drillUpgradePopup.SetActive(false);
        xrayUpgradePopup.SetActive(false);
        fuelUpgradePopup.SetActive(false);
    }

    // Gerekirse diðer pop-up ve UI yönetimi metotlarý buraya eklenebilir.
}
