using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject sellPopupPanel;
    public GameObject upgradeScreenPanel;
    public GameObject drillUpgradePopup;
    public GameObject fuelUpgradePopup;
    public GameObject xrayUpgradePopup;
    public GameObject fuelEmptyPanel;
    public GameObject levelCompletePanel;

    [Header("Settings")]
    public GameObject settingsPanel;

    [Header("Global In-Game UI Elements")]
    public TMP_Text globalMoneyText;

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
        if (sellPopupPanel != null)
            sellPopupPanel.SetActive(false);
        if (drillUpgradePopup != null)
            drillUpgradePopup.SetActive(false);
        if (fuelUpgradePopup != null)
            fuelUpgradePopup.SetActive(false);
        if (xrayUpgradePopup != null)
            xrayUpgradePopup.SetActive(false);
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.SetUIRefs(globalMoneyText);
            UpgradeManager.Instance.UpdateGlobalMoneyUI();
        }
        else
        {
            Debug.LogError("UpgradeManager.Instance bulunamadı.");
        }
    }

    public void ShowFuelEmptyThenRestart(float delay = 2f)
    {
        StartCoroutine(FuelEmptyRoutine(delay, true));
    }

    private IEnumerator FuelEmptyRoutine(float delay, bool restart = false)
    {
        fuelEmptyPanel.SetActive(true);
        yield return new WaitForSeconds(delay);
        fuelEmptyPanel.SetActive(false);

        if (restart)
        {
            RunIntroManager intro = FindObjectOfType<RunIntroManager>();
            if (intro != null)
            {
                intro.StartIntro();
            }
        }
    }

    public void ShowFuelEmptyThenDo(System.Action onComplete, float delay = 2.5f)
    {
        StartCoroutine(FuelEmptyRoutine(onComplete, delay));
    }

    private IEnumerator FuelEmptyRoutine(System.Action onComplete, float delay)
    {
        fuelEmptyPanel.SetActive(true);
        yield return new WaitForSeconds(delay);
        fuelEmptyPanel.SetActive(false);

        onComplete?.Invoke();
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void StartNextLevelCycle()
    {
        if (levelCompletePanel != null)
            levelCompletePanel.SetActive(false);

        RunIntroManager intro = FindObjectOfType<RunIntroManager>();
        if (intro != null)
        {
            intro.StartIntro();
        }
    }

    public void OpenSellPopup()
    {
        if (sellPopupPanel != null)
            sellPopupPanel.SetActive(true);
    }

    public void CloseSellPopup()
    {
        if (sellPopupPanel != null)
            sellPopupPanel.SetActive(false);
    }

    public void OpenUpgradeScreen()
    {
        if (upgradeScreenPanel != null)
            upgradeScreenPanel.SetActive(true);
        CloseAllDetailPopups();
    }

    public void CloseUpgradeScreen()
    {
        if (upgradeScreenPanel != null)
            upgradeScreenPanel.SetActive(false);
        CloseAllDetailPopups();
    }

    public void OpenDrillUpgradePopup()
    {
        CloseAllDetailPopups();
        if (drillUpgradePopup != null)
            drillUpgradePopup.SetActive(true);
    }

    public void OpenFuelUpgradePopup()
    {
        CloseAllDetailPopups();
        if (fuelUpgradePopup != null)
            fuelUpgradePopup.SetActive(true);
    }

    public void OpenXRayUpgradePopup()
    {
        CloseAllDetailPopups();
        if (xrayUpgradePopup != null)
            xrayUpgradePopup.SetActive(true);
    }

    void CloseAllDetailPopups()
    {
        if (drillUpgradePopup != null)
            drillUpgradePopup.SetActive(false);
        if (fuelUpgradePopup != null)
            fuelUpgradePopup.SetActive(false);
        if (xrayUpgradePopup != null)
            xrayUpgradePopup.SetActive(false);
    }
}
