using UnityEngine;
using UnityEngine.UI;

public class FuelUI : MonoBehaviour
{
    [SerializeField]
    private FuelSystem fuelSystem; // FuelSystem scriptine referans

    [SerializeField]
    private Image fuelBarImage; // Fill bar UI Image

    void Start()
    {
        if (fuelSystem != null)
        {
            fuelSystem.OnFuelChanged += UpdateFuelUI;
        }
        else
        {
            Debug.LogError("[FuelUI] FuelSystem referansı atanmadı!");
        }
    }

    void UpdateFuelUI(float currentFuel, float maxFuel)
    {
        if (fuelBarImage != null)
        {
            fuelBarImage.fillAmount = currentFuel / maxFuel;
        }
    }

    void OnDestroy()
    {
        if (fuelSystem != null)
        {
            fuelSystem.OnFuelChanged -= UpdateFuelUI;
        }
    }
}
