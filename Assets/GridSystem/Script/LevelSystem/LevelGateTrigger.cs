using System.Collections;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelGateTrigger : MonoBehaviour
{
    public Transform moveTargetPoint;
    public float moveDuration = 1.5f;

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

        // Oyuncuyu geri getir
        yield return new WaitForSeconds(0.5f);
        yield return player.transform.DOMove(startPos, moveDuration).WaitForCompletion();

        // Seviye atlat ve yeni chunk'ları spawn et
        MapChunkSpawner spawner = FindObjectOfType<MapChunkSpawner>();
        if (spawner != null)
        {
            spawner.currentLevel += 1;
            spawner.SpawnChunks();
        }

        // Kontrolü aç
        player.SetMovementEnabled(true);
        Debug.Log("[LevelGateTrigger] Geçiş tamamlandı.");

        // Geçiş tamamlandıktan sonra yeniden tetiklenebilir hale getir
        triggered = false;
    }
}
