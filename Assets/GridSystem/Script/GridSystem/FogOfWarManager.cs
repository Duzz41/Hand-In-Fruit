using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    private class XRayTile
    {
        public HexTile tile;
        public float delay;

        public XRayTile(HexTile tile, float delay)
        {
            this.tile = tile;
            this.delay = delay;
        }
    }

    private Transform player;

    [Header("Normal Vision")]
    public float revealRadius = 5f;
    public float hideRadius = 7f;

    [Header("X-Ray Skill")]
    public float xrayRadius = 10f;
    public float xrayAngle = 30f;
    public float xrayDuration = 2f;
    public float xrayCooldown = 10f;

    [Range(0.01f, 0.2f)]
    public float waveSpeed = 0.05f; // Dalga hızı (mesafe başına gecikme)

    private bool xrayOnCooldown = false;
    private bool isXrayActive = false;

    [SerializeField]
    private ParticleSystem xrayEffect;

    private HashSet<HexTile> currentlyVisibleTiles = new HashSet<HexTile>();
    private HashSet<HexTile> revealedTiles = new HashSet<HexTile>();
    private HashSet<HexTile> xrayActiveTiles = new HashSet<HexTile>();

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null)
            return;

        // X-Ray aktifken normal vision'ı durdur
        if (isXrayActive)
            return;

        HashSet<HexTile> newVisibleTiles = new HashSet<HexTile>();
        Collider[] nearby = Physics.OverlapSphere(player.position, revealRadius);

        foreach (var col in nearby)
        {
            if (col == null)
                continue;

            HexTile tile = col.GetComponentInParent<HexTile>();

            if (tile != null && !IsDestroyed(tile))
            {
                newVisibleTiles.Add(tile);

                // Eğer tile daha önce görünür değilse, reveal et
                if (!currentlyVisibleTiles.Contains(tile))
                {
                    tile.RevealTile();
                    revealedTiles.Add(tile);
                }
                else
                {
                    // Eğer tile zaten görünürse ve X-Ray modundaysa, normal materyali geri yükle
                    if (tile.IsInXRayMode)
                    {
                        // Normal görünüm alanına girdiğinde X-Ray modunu kapat
                        float distance = Vector3.Distance(player.position, tile.transform.position);
                        if (distance < revealRadius)
                        {
                            tile.SetXRayMode(false); // X-Ray modunu kapat
                        }
                    }
                }
            }
        }

        HashSet<HexTile> tilesToProcess = new HashSet<HexTile>(currentlyVisibleTiles);
        foreach (var tile in tilesToProcess)
        {
            if (IsDestroyed(tile))
                continue;

            if (!newVisibleTiles.Contains(tile))
            {
                float distance = Vector3.Distance(player.position, tile.transform.position);
                if (distance > hideRadius)
                {
                    tile.HideTile();
                }
                else
                {
                    newVisibleTiles.Add(tile);
                }
            }
        }

        currentlyVisibleTiles = newVisibleTiles;
    }

    public void StartXRay()
    {
        if (xrayOnCooldown)
            return;
        StartCoroutine(PerformXRayWaveScan());
    }

    private IEnumerator PerformXRayWaveScan()
    {
        xrayOnCooldown = true;
        isXrayActive = true;
        if (xrayEffect != null)
            xrayEffect.Play();

        List<XRayTile> tileList = new List<XRayTile>();

        // X-Ray menzilindeki tüm tile'ları bul
        Collider[] hits = Physics.OverlapSphere(player.position, xrayRadius);
        foreach (var hit in hits)
        {
            HexTile tile = hit.GetComponentInParent<HexTile>();
            if (tile != null && !IsDestroyed(tile)) // Sadece geçerli tile'lar
            {
                float distance = Vector3.Distance(player.position, tile.transform.position);
                float delay = distance * waveSpeed; // Dalga hızına göre gecikme

                if (tile.hasValuableResource) // Eğer değerli kaynak varsa
                {
                    tileList.Add(new XRayTile(tile, delay));
                }
                else // Değerli kaynak yoksa hemen gizle
                {
                    StartCoroutine(DeactivateXRayWithDelay(tile, 0f)); // Hemen gizle
                }
            }
        }

        // Açılma için delay küçükten büyüğe sırala (dalgalı açılım)
        tileList.Sort((a, b) => a.delay.CompareTo(b.delay));

        Debug.Log($"X-Ray dalga taraması başlıyor - {tileList.Count} değerli tile bulundu");

        // X-Ray açılma dalgası
        foreach (XRayTile xrayTile in tileList)
        {
            StartCoroutine(ActivateXRayWithDelay(xrayTile.tile, xrayTile.delay));
        }

        // X-Ray süresince bekle
        float totalOpenDuration = tileList.Count > 0 ? tileList[^1].delay + 0.1f : 0.1f;
        yield return new WaitForSeconds(totalOpenDuration + xrayDuration);

        Debug.Log("X-Ray kapanma dalgası başlıyor");

        // X-Ray kapanma dalgası: değerli kaynaklar için
        foreach (XRayTile xrayTile in tileList)
        {
            StartCoroutine(DeactivateXRayWithDelay(xrayTile.tile, xrayDuration)); // Değerli kaynaklar için belirli bir süre açık kal
        }

        // Kapanma süresince bekle
        yield return new WaitForSeconds(xrayDuration);

        isXrayActive = false;
        xrayActiveTiles.Clear();
        if (xrayEffect != null)
            xrayEffect.Stop();
        Debug.Log("X-Ray taraması tamamlandı");

        // Kalan cooldown süresini bekle
        float remainingCooldown = xrayCooldown - totalOpenDuration - xrayDuration;
        if (remainingCooldown > 0)
            yield return new WaitForSeconds(remainingCooldown);

        xrayOnCooldown = false;
    }

    private IEnumerator ActivateXRayWithDelay(HexTile tile, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (tile != null && !IsDestroyed(tile))
        {
            // X-Ray materyalini uygula
            tile.SetXRayMode(true);
            xrayActiveTiles.Add(tile);

            // DoTween ile yukarı doğru hafif hareket ettir
            float moveAmount = 0.2f; // Yukarı doğru hareket miktarı
            float moveDuration = 0.5f; // Hareket süresi

            // Objenin mevcut pozisyonunu al
            Vector3 originalPosition = tile.transform.position;

            // Yukarı doğru hareket ettir
            tile.transform.DOMoveY(originalPosition.y + moveAmount, moveDuration)
                .SetEase(Ease.OutSine) // Hareketin daha doğal görünmesi için easing
                .OnComplete(() =>
                {
                    // Hareket tamamlandığında objeyi eski pozisyona döndür
                    tile.transform.DOMoveY(originalPosition.y, moveDuration).SetEase(Ease.InSine);
                });
        }
    }

    private IEnumerator DeactivateXRayWithDelay(HexTile tile, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (tile != null && !IsDestroyed(tile))
        {
            tile.SetXRayMode(false);
            xrayActiveTiles.Remove(tile);

            // Eğer değerli kaynak değilse hemen gizle
            if (!tile.hasValuableResource)
            {
                float distance = Vector3.Distance(player.position, tile.transform.position);
                if (distance > revealRadius)
                {
                    tile.HideTile();
                }
            }
        }
    }

    private IEnumerator RevealWithDelay(HexTile tile, float delay)
    {
        yield return new WaitForSeconds(delay);
        tile.RevealTile();
    }

    private IEnumerator HideWithDelay(HexTile tile, float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector3 toTile = tile.transform.position - player.position;
        float distance = toTile.magnitude;
        float angle = Vector3.Angle(player.forward, toTile.normalized);

        bool inNormalVision = distance < revealRadius;
        bool inXrayVision = isXrayActive && distance < xrayRadius && angle < xrayAngle * 0.5f;

        if (!inNormalVision && !inXrayVision)
        {
            tile.HideTile();
        }
    }

    private void CleanupDestroyedTiles()
    {
        currentlyVisibleTiles.RemoveWhere(tile => IsDestroyed(tile));
        revealedTiles.RemoveWhere(tile => IsDestroyed(tile));
        xrayActiveTiles.RemoveWhere(tile => IsDestroyed(tile));
    }

    private bool IsDestroyed(HexTile tile)
    {
        return tile == null || tile.gameObject == null;
    }

    public void ClearAllTileReferences()
    {
        currentlyVisibleTiles.Clear();
        revealedTiles.Clear();
        xrayActiveTiles.Clear();
    }

    public void LogTileStatus()
    {
        Debug.Log($"Currently Visible Tiles: {currentlyVisibleTiles.Count}");
        Debug.Log($"Total Revealed Tiles: {revealedTiles.Count}");
        Debug.Log($"X-Ray Active Tiles: {xrayActiveTiles.Count}");

        int destroyedVisible = currentlyVisibleTiles.Count(tile => IsDestroyed(tile));
        int destroyedRevealed = revealedTiles.Count(tile => IsDestroyed(tile));
        int destroyedXRay = xrayActiveTiles.Count(tile => IsDestroyed(tile));

        if (destroyedVisible > 0 || destroyedRevealed > 0 || destroyedXRay > 0)
        {
            Debug.LogWarning(
                $"Destroyed: {destroyedVisible} visible, {destroyedRevealed} revealed, {destroyedXRay} xray tiles"
            );
        }
    }

    public bool IsXRayOnCooldown => xrayOnCooldown;
    public bool IsXRayActive => isXrayActive;

    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, revealRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, hideRadius);

            // X-Ray menzilini çiz
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, xrayRadius);

            // X-Ray açısını çiz
            Vector3 forward = player.forward;
            Vector3 right = Quaternion.AngleAxis(xrayAngle * 0.5f, Vector3.up) * forward;
            Vector3 left = Quaternion.AngleAxis(-xrayAngle * 0.5f, Vector3.up) * forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(player.position, right * xrayRadius);
            Gizmos.DrawRay(player.position, left * xrayRadius);
        }
    }
}
