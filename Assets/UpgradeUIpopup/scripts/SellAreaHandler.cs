using UnityEngine;
using TMPro;

// Bu script'i sarı Sell plane'e ekleyin.
// Collider'ı Is Trigger olarak işaretlemeyi unutmayın.
[RequireComponent(typeof(Collider))]
public class SellAreaHandler : MonoBehaviour
{
    [Tooltip("Bu tetikleyiciyi aktif edecek GameObject'in Tag'i (genellikle 'PlayerVehicle').")]
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        // Giren objenin etiketini ve UpgradeManager instance'ının varlığını kontrol et
        if (other.CompareTag(playerTag) && UpgradeManager.Instance != null)
        {
            Debug.Log($"Oyuncu Aracı ({other.gameObject.name}) Satış Alanına Girdi. Madenler satılıyor...", this);

            // Pop-up açmak yerine direkt satış işlemini yap
            int earnedMoney = UpgradeManager.Instance.SellAllMines();
            InventoryManager.Instance.ClearInventory();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Oyuncu Aracı ({other.gameObject.name}) Satış Alanından Çıktı.", this);
        }
    }
}
