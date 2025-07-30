using System.Collections;
using DG.Tweening; // DOTween varsa
using UnityEngine;

public class RunIntroManager : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform door;
    public Transform mountain;
    public MapChunkSpawner chunkSpawner;

    [Header("Konumlar")]
    public Transform doorStartPos;
    public Transform doorOpenPos;

    public Transform mountainUpPos;
    public Transform mountainDownPos;

    [Header("Süreler")]
    public float doorMoveDuration = 1.5f;
    public float mountainMoveDuration = 1.5f;

    void Start()
    {
        StartCoroutine(IntroSequence());
    }

    public void PlayIntro()
    {
        Debug.Log("[RunIntroManager] Başlangıç sekansı başlatıldı!");
        StartCoroutine(IntroSequence());
        // Burada animasyon oynatma, UI, fade vs yapılabilir
    }

    IEnumerator IntroSequence()
    {
        // Kapı yukarı çıkar
        door.DOMove(doorOpenPos.position, doorMoveDuration);
        yield return new WaitForSeconds(doorMoveDuration);

        // Dağ aşağı iner
        mountain.DOMove(mountainDownPos.position, mountainMoveDuration);
        yield return new WaitForSeconds(mountainMoveDuration);

        // Chunk’lar spawn olur
        chunkSpawner.SpawnChunks();

        // Dağ yukarı çıkar
        mountain.DOMove(mountainUpPos.position, mountainMoveDuration);
        yield return new WaitForSeconds(mountainMoveDuration);

        // Kapı aşağı iner
        door.DOMove(doorStartPos.position, doorMoveDuration);
        yield return new WaitForSeconds(doorMoveDuration);

        Debug.Log("Intro bitti, oyun başlıyor.");
    }
}
