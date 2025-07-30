using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private bool xrayOnCooldown = false;
    private bool isXrayActive = false;

    private HashSet<HexTile> currentlyVisibleTiles = new HashSet<HexTile>();
    private HashSet<HexTile> revealedTiles = new HashSet<HexTile>();

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null)
            return;

        if (Input.GetKeyDown(KeyCode.Q) && !xrayOnCooldown)
        {
            StartCoroutine(PerformXRayWaveScan());
        }

        CleanupDestroyedTiles();

        // XRay aktifken normal vision'ı durdur
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

                if (!currentlyVisibleTiles.Contains(tile))
                {
                    tile.RevealTile();
                    revealedTiles.Add(tile);
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

        List<XRayTile> tileList = new List<XRayTile>();

        Collider[] hits = Physics.OverlapSphere(player.position, xrayRadius);
        foreach (var hit in hits)
        {
            HexTile tile = hit.GetComponentInParent<HexTile>();
            if (tile != null)
            {
                Vector3 toTile = (tile.transform.position - player.position).normalized;
                float angle = Vector3.Angle(player.forward, toTile);
                if (angle < xrayAngle * 0.5f)
                {
                    float distance = Vector3.Distance(player.position, tile.transform.position);
                    float delay = distance * 0.05f; // Açılma için mesafeye göre delay
                    tileList.Add(new XRayTile(tile, delay));
                }
            }
        }

        // Açılma için delay küçükten büyüğe sırala
        tileList.Sort((a, b) => a.delay.CompareTo(b.delay));

        // Açılma dalgası
        foreach (XRayTile xrayTile in tileList)
        {
            StartCoroutine(RevealWithDelay(xrayTile.tile, xrayTile.delay));
        }

        // Açılma süresi + en son delay kadar bekle
        float totalDuration = tileList.Count > 0 ? tileList[^1].delay + xrayDuration : xrayDuration;
        yield return new WaitForSeconds(totalDuration);

        // Kapanma dalgası: ters sırada, ters gecikme ile başlat
        float closeDelayStep = 0.05f; // Kapanma gecikme aralığı (isteğe göre ayarla)

        for (int i = 0; i < tileList.Count; i++)
        {
            int reverseIndex = tileList.Count - 1 - i;
            XRayTile xrayTile = tileList[reverseIndex];

            float reverseDelay = i * closeDelayStep; // ters sıra ile artan delay

            StartCoroutine(HideWithDelay(xrayTile.tile, reverseDelay));
        }

        // Kapanma süresi (toplam gecikme)
        float closeTotalDuration = tileList.Count * closeDelayStep + xrayDuration;
        yield return new WaitForSeconds(closeTotalDuration);

        // Cooldown bekle, kapanma ve açılma süresi toplamı çıkarıldı
        float cooldownWait = xrayCooldown - totalDuration - closeTotalDuration;
        if (cooldownWait > 0)
            yield return new WaitForSeconds(cooldownWait);

        xrayOnCooldown = false;
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
    }

    private bool IsDestroyed(HexTile tile)
    {
        return tile == null || tile.gameObject == null;
    }

    public void ClearAllTileReferences()
    {
        currentlyVisibleTiles.Clear();
        revealedTiles.Clear();
    }

    public void LogTileStatus()
    {
        Debug.Log($"Currently Visible Tiles: {currentlyVisibleTiles.Count}");
        Debug.Log($"Total Revealed Tiles: {revealedTiles.Count}");

        int destroyedVisible = currentlyVisibleTiles.Count(tile => IsDestroyed(tile));
        int destroyedRevealed = revealedTiles.Count(tile => IsDestroyed(tile));

        if (destroyedVisible > 0 || destroyedRevealed > 0)
        {
            Debug.LogWarning(
                $"Destroyed: {destroyedVisible} visible, {destroyedRevealed} revealed tiles"
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, revealRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, hideRadius);
        }
    }
}
