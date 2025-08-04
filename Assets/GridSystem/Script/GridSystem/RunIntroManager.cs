using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class RunIntroManager : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform door;
    public Transform mountain;
    public MapChunkSpawner chunkSpawner;
    public PlayerController playerController;
    public CinemachineBrain cinemachineCamera;
    public GameObject startButton;
    public GameObject MountainModel;

    [Header("Konumlar")]
    public Transform doorStartPos;
    public Transform doorOpenPos;
    public Transform mountainUpPos;
    public Transform mountainDownPos;

    [Header("Kamera Ayarı")]
    public Vector3 cameraIntroPosition;
    public Vector3 cameraIntroRotation;
    public float cameraTransitionDuration = 2f;

    public bool hasStarted = false;

    void Start()
    {
        // Başlangıç pozisyonlarını ayarla
        door.position = doorStartPos.position;
        mountain.position = mountainDownPos.position;

        // Oyuncu hareketini devre dışı bırak
        playerController.SetMovementEnabled(false);

        // Kamerayı başlangıç açısına ayarla
        cinemachineCamera.transform.position = cameraIntroPosition;
        cinemachineCamera.transform.rotation = Quaternion.Euler(cameraIntroRotation);
    }

    public void PlayIntro()
    {
        StartCoroutine(StartIntroSequence());
    }

    private IEnumerator StartIntroSequence()
    {
        if (hasStarted)
            yield break;

        hasStarted = true;
        Debug.Log("▶ Intro Sequence başladı");

        // 1. Start butonunu kapat
        if (startButton != null)
        {
            startButton.SetActive(false);
            Debug.Log("⛔ Start butonu kapatıldı");
        }

        // 2. Kamera hareketi
        Debug.Log("📷 Kamera hareketi başlıyor");
        var cameraMove = cinemachineCamera.transform.DOMove(
            cameraIntroPosition,
            cameraTransitionDuration
        );
        var cameraRotate = cinemachineCamera.transform.DORotate(
            cameraIntroRotation,
            cameraTransitionDuration
        );

        yield return cameraMove.WaitForCompletion();
        yield return cameraRotate.WaitForCompletion();

        // 3. Dağ yukarı çıkıyor
        Debug.Log("⛰️ Dağ yukarı çıkıyor");
        yield return mountain
            .DOMove(mountainUpPos.position, 1f)
            .SetEase(Ease.OutCubic)
            .WaitForCompletion();

        // 4. Kapı açılıyor
        Debug.Log("🚪 Kapı açılıyor");
        yield return door.DOMove(doorOpenPos.position, 1f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        // 5. Harita spawn ediliyor
        Debug.Log("🗺️ Harita spawn ediliyor");
        chunkSpawner.SpawnChunks();

        // 6. Oyuncu kontrolü açılıyor
        yield return new WaitForSeconds(0.5f); // Küçük bir bekleme
        Debug.Log("🎮 Oyuncu kontrolü açıldı");
        playerController.SetMovementEnabled(true);
    }
}
