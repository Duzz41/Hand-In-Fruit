using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class RunIntroManager1 : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform door;
    public Transform mountain;
    public MapChunkSpawner chunkSpawner;
    public PlayerController playerController;
    public CinemachineBrain virtualCamera; // CinemachineVirtualCamera yerine CinemachineCamera

    [SerializeField]
    GameObject MountainModel;

    [Header("UI")]
    public Canvas mainMenuCanvas; // Ana menü UI canvas
    public Canvas gameUICanvas; // Oyun içi joystickler ve diğer UI canvas
    public Button startButton;

    [Header("Konumlar")]
    public Transform doorStartPos;
    public Transform doorOpenPos;

    public Transform mountainUpPos;
    public Transform mountainDownPos;

    public CinemachineFollow cameraFollowTarget; // Kameranın takip ettiği hedef

    [Header("Kamera Offset Ayarları")]
    public Vector3 startCameraOffset = new Vector3(0, 8, -12); // Başlangıç kamera pozisyonu
    public Vector3 gameCameraOffset = new Vector3(0, 5, -10); // Oyun içi kamera pozisyonu

    [Header("Kamera Rotasyon Ayarları")]
    public Vector3 startCameraRotation = new Vector3(20, 0, 0); // Başlangıç kamera rotasyonu
    public Vector3 gameCameraRotation = new Vector3(15, 0, 0); // Oyun içi kamera rotasyonu

    [Header("Süreler")]
    public float doorMoveDuration = 1.5f;
    public float mountainMoveDuration = 1.5f;
    public float cameraMoveDuration = 2f;

    void Start()
    {
        // Kamera başlangıç ayarları
        // Başlangıç kamera offset ve rotasyon ayarları
        cameraFollowTarget.FollowOffset = startCameraOffset;
        cameraFollowTarget.transform.eulerAngles = startCameraRotation;

        mainMenuCanvas.gameObject.SetActive(true);
        gameUICanvas.gameObject.SetActive(false);
    }

    public void OnStartButtonClicked()
    {
        startButton.interactable = false;
        mainMenuCanvas.gameObject.SetActive(false);
        gameUICanvas.gameObject.SetActive(true);

        // Kamera hedefini oyun içi objeye yumuşakça geçirelim
        StartCoroutine(SmoothCameraFollowChange());
    }

    IEnumerator SmoothCameraFollowChange()
    {
        // Başlangıç ve hedef değerleri
        Vector3 startOffset = startCameraOffset;
        Vector3 targetOffset = gameCameraOffset;

        Vector3 startRotation = startCameraRotation;
        Vector3 targetRotation = gameCameraRotation;

        float elapsed = 0f;
        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraMoveDuration;

            // Offset'i yumuşakça değiştir
            cameraFollowTarget.FollowOffset = Vector3.Lerp(startOffset, targetOffset, t);

            // Rotasyonu yumuşakça değiştir
            Vector3 currentRotation = Vector3.Lerp(startRotation, targetRotation, t);
            cameraFollowTarget.transform.eulerAngles = currentRotation;

            yield return null;
        }

        // Son değerleri garantilemek için
        cameraFollowTarget.FollowOffset = targetOffset;
        cameraFollowTarget.transform.eulerAngles = targetRotation;

        // Intro başlat
        StartCoroutine(IntroSequence());
    }

    // Bu metod artık kullanılmıyor ama geriye dönük uyumluluk için bırakıldı

    public void PlayIntro()
    {
        StartCoroutine(IntroSequence());
    }

    IEnumerator IntroSequence()
    {
        MountainModel.SetActive(true);

        door.DOMove(doorOpenPos.position, doorMoveDuration);
        yield return new WaitForSeconds(doorMoveDuration);

        mountain.DOMove(mountainDownPos.position, mountainMoveDuration);
        yield return new WaitForSeconds(mountainMoveDuration);

        chunkSpawner.SpawnChunks();

        mountain.DOMove(mountainUpPos.position, mountainMoveDuration);
        yield return new WaitForSeconds(mountainMoveDuration);

        door.DOMove(doorStartPos.position, doorMoveDuration);
        yield return new WaitForSeconds(doorMoveDuration);
        // playerController.SetMovementEnabled(false);
        //playerController.ForceResetPhysics(); // Physics değerlerini sıfırla

        Debug.Log("Intro bitti, oyun başladı - Movement ENABLED!");
    }
}
