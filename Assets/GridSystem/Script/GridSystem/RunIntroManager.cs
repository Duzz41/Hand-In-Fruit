using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;

public class RunIntroManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject startButtonUI;
    public GameObject mainMenuUI;
    public GameObject gameplayUI;

    [Header("Kamera Sistemi")]
    public GameObject followCamera; // İçinde CinemachineFollow var
    private CinemachineFollow cinemachineFollow;

    [Header("Sahne Objeleri")]
    public Transform door;
    public Transform doorDownPos; // Kapının aşağı pozisyonu
    public Transform doorUpPos; // Kapının yukarı pozisyonu
    public Transform mountain;
    public Transform mountainUpPos;
    public Transform mountainDownPos; // Dağın aşağı pozisyonu
    public GameObject player;

    [Header("Kontroller")]
    public PlayerController playerController;
    public MapChunkSpawner chunkSpawner;

    [Header("Ayarlar")]
    public float cameraMoveDuration = 2f;
    public float mountainMoveDuration = 3f;
    public float doorMoveDuration = 2f; // Kapı hareket süresi
    public bool firstIntro = false;
    private bool introStarted = false;

    [Header("Kamera Pozisyon Referansları")]
    public Transform doorCameraPosition; // Kapıya bakacak kamera pozisyonu
    public Transform mountainCameraPosition; // Dağa bakacak kamera pozisyonu
    public Transform playerCameraPosition; // Oyuncuya bakacak kamera pozisyonu

    [Header("Final Ayarlar")]
    public GameObject finalCameraObject; // İntro bitince aktif edilecek kamera objesi

    void Start()
    {
        gameplayUI.SetActive(false);
        playerController.enabled = false;
        cinemachineFollow = followCamera.GetComponent<CinemachineFollow>();

        // Start butonu sahne başında aktif kalsın
        startButtonUI.SetActive(true);
        mainMenuUI.SetActive(true);
    }

    public void StartButtonClicked()
    {
        if (introStarted)
            return;
        startButtonUI.SetActive(false);

        StartCoroutine(IntroSequence());
    }

    public void StartIntro()
    {
        if (introStarted)
            return;
        if (firstIntro == false)
            return;
        introStarted = true;

        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // İlk başlangıçta kapı aşağı insin
        if (!firstIntro)
        {
            bool doorMoveDone = false;
            door.DOMove(doorDownPos.position, doorMoveDuration)
                .OnComplete(() => doorMoveDone = true);
            yield return new WaitUntil(() => doorMoveDone);
        }

        if (!firstIntro)
        {
            // Kapıya geç - Transform referansını kullan
            if (doorCameraPosition != null)
            {
                yield return MoveToTransformPosition(doorCameraPosition, door, cameraMoveDuration);
            }

            // Dağa geç - Transform referansını kullan
            if (mountainCameraPosition != null)
            {
                yield return MoveToTransformPosition(
                    mountainCameraPosition,
                    mountain,
                    cameraMoveDuration
                );
            }
        }

        // Dağ yukarı çıkar
        bool mountainMoveDone = false;
        mountain
            .DOMove(mountainUpPos.position, mountainMoveDuration)
            .OnComplete(() => mountainMoveDone = true);
        yield return new WaitUntil(() => mountainMoveDone);

        if (!firstIntro)
        {
            // Oyuncuya geç - Transform referansını kullan
            if (playerCameraPosition != null)
            {
                yield return MoveToTransformPositionAndRotation(
                    playerCameraPosition,
                    cameraMoveDuration
                );
            }
            firstIntro = true;
        }

        // Final kamera objesini aktif et
        if (finalCameraObject != null)
        {
            finalCameraObject.SetActive(true);
        }

        gameplayUI.SetActive(true);
        chunkSpawner.SpawnChunks();
        playerController.enabled = true;
    }

    private IEnumerator MoveToTransformPosition(
        Transform cameraPositionTarget,
        Transform lookAtTarget,
        float duration
    )
    {
        Vector3 startOffset = cinemachineFollow.FollowOffset;
        Vector3 targetOffset = cameraPositionTarget.position - player.transform.position;

        Quaternion startRotation = followCamera.transform.rotation;

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Offset'i lerp et
            cinemachineFollow.FollowOffset = Vector3.Lerp(startOffset, targetOffset, t);

            // Kamera pozisyonunu Transform'dan al ve LookAt yap
            Vector3 currentCameraPos = player.transform.position + cinemachineFollow.FollowOffset;
            Vector3 directionToTarget = lookAtTarget.position - currentCameraPos;

            if (directionToTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                followCamera.transform.rotation = Quaternion.Slerp(
                    startRotation,
                    targetRotation,
                    t
                );
            }

            yield return null;
        }

        // Final değerleri ayarla
        cinemachineFollow.FollowOffset = targetOffset;
        Vector3 finalCameraPos = player.transform.position + cinemachineFollow.FollowOffset;
        Vector3 finalDirection = lookAtTarget.position - finalCameraPos;
        if (finalDirection != Vector3.zero)
        {
            followCamera.transform.rotation = Quaternion.LookRotation(finalDirection);
        }
    }

    private IEnumerator MoveToTransformPositionAndRotation(
        Transform cameraPositionTarget,
        float duration
    )
    {
        Vector3 startOffset = cinemachineFollow.FollowOffset;
        Vector3 targetOffset = cameraPositionTarget.position - player.transform.position;

        Quaternion startRotation = followCamera.transform.rotation;
        Quaternion targetRotation = cameraPositionTarget.rotation;

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Offset'i lerp et
            cinemachineFollow.FollowOffset = Vector3.Lerp(startOffset, targetOffset, t);

            // Rotasyonu Transform'dan al
            followCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Final değerleri ayarla
        cinemachineFollow.FollowOffset = targetOffset;
        followCamera.transform.rotation = targetRotation;
    }

    // Oyun devam ederken çağrılacak metod - MapChunkSpawner'dan çağrılabilir
    public void SpawnChunks()
    {
        if (introStarted == true)
            return;
        StartCoroutine(SpawnChunksSequence());
    }

    private IEnumerator SpawnChunksSequence()
    {
        // 1. Kapı yukarı çık
        bool doorUpDone = false;
        door.DOMove(doorUpPos.position, doorMoveDuration).OnComplete(() => doorUpDone = true);
        yield return new WaitUntil(() => doorUpDone);

        // 2. Dağ aşağı in
        bool mountainDownDone = false;
        mountain
            .DOMove(mountainDownPos.position, mountainMoveDuration)
            .OnComplete(() => mountainDownDone = true);
        yield return new WaitUntil(() => mountainDownDone);

        // 3. Chunk spawn et
        chunkSpawner.SpawnChunks();

        // 4. Dağ yukarı çık
        bool mountainUpDone = false;
        mountain
            .DOMove(mountainUpPos.position, mountainMoveDuration)
            .OnComplete(() => mountainUpDone = true);
        yield return new WaitUntil(() => mountainUpDone);

        // 5. Kapı tekrar aşağı in
        bool doorDownDone = false;
        door.DOMove(doorDownPos.position, doorMoveDuration).OnComplete(() => doorDownDone = true);
        yield return new WaitUntil(() => doorDownDone);
    }

    // Eski RotateAndOffsetToTarget metodu - geriye dönük uyumluluk için
    private IEnumerator RotateAndOffsetToTarget(
        Vector3 offsetTarget,
        Vector3 lookAtTarget,
        float duration
    )
    {
        Vector3 startOffset = cinemachineFollow.FollowOffset;
        Vector3 targetOffset = offsetTarget - player.transform.position;

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            cinemachineFollow.FollowOffset = Vector3.Lerp(startOffset, targetOffset, t);

            Quaternion targetRotation = Quaternion.LookRotation(
                lookAtTarget - followCamera.transform.position
            );
            followCamera.transform.rotation = Quaternion.Slerp(
                followCamera.transform.rotation,
                targetRotation,
                t
            );

            yield return null;
        }

        cinemachineFollow.FollowOffset = targetOffset;
        followCamera.transform.rotation = Quaternion.LookRotation(
            lookAtTarget - followCamera.transform.position
        );
    }

    // Eski MoveToPlayerPreset metodu kaldırıldı - artık RotateAndOffsetToTransform kullanılıyor
}
