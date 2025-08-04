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
    public Transform mountain;
    public Transform mountainUpPos;
    public GameObject player;

    [Header("Kontroller")]
    public PlayerController playerController;
    public MapChunkSpawner chunkSpawner;

    [Header("Ayarlar")]
    public float cameraMoveDuration = 2f;
    public float mountainMoveDuration = 3f;
    public bool firstIntro = false;
    private bool introStarted = false;

    [Header("Kamera Ayarları")]
    public Vector3 playerViewOffset; // Kameranın oyuncuya göre ofseti
    public Vector3 playerViewEuler; // Kameranın oyuncuya bakış rotasyonu (Inspector'dan ayarlanır)

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
        if (!firstIntro)
        {
            // Kapıya geç
            yield return RotateAndOffsetToTarget(
                door.position + new Vector3(0, 2, -5),
                door.position,
                cameraMoveDuration
            );

            // Dağa geç
            yield return RotateAndOffsetToTarget(
                mountain.position + new Vector3(0, 3, -6),
                mountain.position,
                cameraMoveDuration
            );
        }

        // Dağ yukarı çıkar
        bool mountainMoveDone = false;
        mountain
            .DOMove(mountainUpPos.position, mountainMoveDuration)
            .OnComplete(() => mountainMoveDone = true);
        yield return new WaitUntil(() => mountainMoveDone);

        if (!firstIntro)
        {
            // Oyuncuya geç ama offset ve rotation inspector’dan
            yield return MoveToPlayerPreset(cameraMoveDuration);
            firstIntro = true;
        }

        gameplayUI.SetActive(true);
        chunkSpawner.SpawnChunks();
        playerController.enabled = true;
    }

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

    private IEnumerator MoveToPlayerPreset(float duration)
    {
        Vector3 startOffset = cinemachineFollow.FollowOffset;
        Vector3 targetOffset = playerViewOffset;

        Quaternion startRotation = followCamera.transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(playerViewEuler);

        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            cinemachineFollow.FollowOffset = Vector3.Lerp(startOffset, targetOffset, t);
            followCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        cinemachineFollow.FollowOffset = targetOffset;
        followCamera.transform.rotation = targetRotation;
        firstIntro = true;
    }
}
