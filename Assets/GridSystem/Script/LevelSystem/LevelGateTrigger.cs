using System.Collections;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelGateTrigger : MonoBehaviour
{
    [Header("Hareket Ayarları")]
    public float moveDuration = 1.5f;

    [Header("Pozisyon Referansları")]
    public Transform playerStartPosition; // Yeni bölümün başlangıç pozisyonu

    [Header("UI Ayarları")]
    public GameObject levelCompletePanel; // UI'da "Bölüm Geçildi" paneli
    public TMPro.TextMeshProUGUI levelText; // Level sayısını gösteren UI metni

    [Header("Sistem Referansları")]
    public MapChunkSpawner mapChunkSpawner;
    public RunIntroManager runIntroManager;

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

        // 1. Oyuncuyu alanın x,z merkezine götür (y pozisyonu sabit kalsın)
        Vector3 centerPosition = new Vector3(
            transform.position.x,
            player.transform.position.y,
            transform.position.z
        );

        yield return player.transform.DOMove(centerPosition, moveDuration).WaitForCompletion();

        // 2. UI panelini aç
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        // 3. UI'nın bir süre açık kalmasını sağla
        yield return new WaitForSeconds(2f);

        // 4. UI panelini kapat
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        // 5. Oyuncuyu Player Start Pozisyonuna ışınla
        if (playerStartPosition != null)
        {
            player.transform.position = playerStartPosition.position;
            Debug.Log("[LevelGateTrigger] Oyuncu start pozisyonuna ışınlandı.");
        }

        // 6. Level'i arttır ve yeni bölümü spawn et
        if (mapChunkSpawner != null)
        {
            mapChunkSpawner.currentLevel += 1; // Önce level'i arttır
        }

        if (runIntroManager != null)
        {
            runIntroManager.SpawnChunks(); // Bu metod dağ sekansını da içeriyor
        }
        else if (mapChunkSpawner != null)
        {
            // Eğer RunIntroManager yoksa direkt spawner kullan
            mapChunkSpawner.SpawnChunks();
        }

        // 6.5. Level UI'sını güncelle
        UpdateLevelUI();

        // 7. Dağ sekansının tamamlanması için bekleme
        yield return new WaitForSeconds(3f);

        // 8. Kontrolü tekrar aç
        player.SetMovementEnabled(true);
        Debug.Log("[LevelGateTrigger] Geçiş tamamlandı.");

        // Yeniden tetiklenebilir hale getir
        triggered = false;
    }

    private void UpdateLevelUI()
    {
        if (levelText != null && mapChunkSpawner != null)
        {
            levelText.text = "Level " + mapChunkSpawner.currentLevel;
        }
    }
}
