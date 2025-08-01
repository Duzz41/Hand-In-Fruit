using UnityEngine;
using UnityEngine.Events; // UnityEvent için gerekli

// Bu script'i, oyuncunun yaklaþtýðýnda bir UI panelini tetikleyecek bölgeler için kullanacaðýz.
[RequireComponent(typeof(Collider))] // Bu script'in çalýþmasý için bir Collider olmasý þart
public class UpgradeTrigger : MonoBehaviour
{
    [Tooltip("Bu tetikleyici alanýn hangi yükseltme panelini açacaðýný belirler.")]
    public GameObject targetUpgradePanel;

    [Tooltip("Bu tetikleyiciyi aktif edecek GameObject'in Tag'i (genellikle 'PlayerVehicle').")]
    public string playerTag = "Player";

    // Oyuncu yaklaþtýðýnda çaðrýlacak metot (örneðin UIManager.Instance.OpenDrillUpgradePopup)
    public UnityEvent onPlayerEnter;

    // Oyuncu uzaklaþtýðýnda çaðrýlacak metot (örneðin UIManager.Instance.CloseDrillUpgradePopup)
    public UnityEvent onPlayerExit;

    // Collider'ýn tetikleyici olduðundan emin ol
    void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("UpgradeTrigger script'ini kullanan objenin Collider'ý 'Is Trigger' olarak ayarlanmalý.", this);
        }
    }

    /// <summary>
    /// Baþka bir Collider bu tetikleyiciye girdiðinde çaðrýlýr.
    /// </summary>
    /// <param name="other">Tetikleyiciye giren Collider bileþeni.</param>
    void OnTriggerEnter(Collider other)
    {
        // Giren objenin etiketini kontrol et
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Oyuncu Aracý ({other.gameObject.name}) {gameObject.name} bölgesine girdi. Yükseltme paneli açýlýyor.", this);
            onPlayerEnter.Invoke();
        }
    }

    /// <summary>
    /// Baþka bir Collider bu tetikleyiciden çýktýðýnda çaðrýlýr.
    /// </summary>
    /// <param name="other">Tetikleyiciden çýkan Collider bileþeni.</param>
    void OnTriggerExit(Collider other)
    {
        // Çýkan objenin etiketini kontrol et
        if (other.CompareTag(playerTag))
        {
            Debug.Log($"Oyuncu Aracý ({other.gameObject.name}) {gameObject.name} bölgesinden çýktý. Yükseltme paneli kapatýlýyor.", this);
            onPlayerExit.Invoke();
        }
    }
}