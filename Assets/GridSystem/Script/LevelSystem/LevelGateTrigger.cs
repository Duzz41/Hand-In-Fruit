using System.Collections;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelGateTrigger : MonoBehaviour
{
    public Transform moveTargetPoint;
    public float moveDuration = 1.5f;

    [Header("UI Ayarları")]
    public GameObject levelCompletePanel; // UI’da "Bölüm Geçildi" paneli

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            StartCoroutine(HandleTransition(player));
        }
    }

    private IEnumerator HandleTransition(PlayerController player)
    {
        triggered = true;

        Debug.Log("[LevelGateTrigger] Oyuncu geçiş alanına girdi.");
        player.SetMovementEnabled(false);

        Vector3 startPos = player.transform.position;

        // Oyuncuyu ileri hareket ettir
        yield return player
            .transform.DOMove(moveTargetPoint.position, moveDuration)
            .WaitForCompletion();

        // UI panelini aç
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        // UI’nın bir süre açık kalmasını sağla
        yield return new WaitForSeconds(2f);

        // UI panelini kapat
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        // Oyuncuyu geri getir
        yield return new WaitForSeconds(0.5f);
        yield return player.transform.DOMove(startPos, moveDuration).WaitForCompletion();

        // Yeni seviyeyi spawn et
        MapChunkSpawner spawner = FindObjectOfType<MapChunkSpawner>();
        if (spawner != null)
        {
            spawner.currentLevel += 1;
            spawner.SpawnChunks();
        }

        // Kontrolü aç
        player.SetMovementEnabled(true);
        Debug.Log("[LevelGateTrigger] Geçiş tamamlandı.");

        // Yeniden tetiklenebilir hale getir (istersen bunu kaldırabilirsin tek seferlik için)
        triggered = false;
    }
}
