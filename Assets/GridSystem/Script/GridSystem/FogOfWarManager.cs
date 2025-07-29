using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    private Transform player;
    public float revealRadius = 5f;
    public float hideRadius = 7f; // Gizleme radiusu reveal'den biraz daha büyük

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

        // Null/destroyed tile'ları temizle
        CleanupDestroyedTiles();

        // Mevcut görünür tile'ları kontrol et
        HashSet<HexTile> newVisibleTiles = new HashSet<HexTile>();

        Collider[] nearby = Physics.OverlapSphere(player.position, revealRadius);
        foreach (var col in nearby)
        {
            // Collider'ın kendisi de null olabilir
            if (col == null)
                continue;

            HexTile tile = col.GetComponent<HexTile>();
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

        // Artık görünmeyen tile'ları gizle (ama sadece hide radius dışındaysa)
        HashSet<HexTile> tilesToProcess = new HashSet<HexTile>(currentlyVisibleTiles);

        foreach (var tile in tilesToProcess)
        {
            // Tile destroyed mı kontrol et
            if (IsDestroyed(tile))
            {
                continue; // Skip destroyed tiles, cleanup will handle them
            }

            if (!newVisibleTiles.Contains(tile))
            {
                try
                {
                    float distance = Vector3.Distance(player.position, tile.transform.position);
                    if (distance > hideRadius)
                    {
                        tile.HideTile();
                    }
                    else
                    {
                        newVisibleTiles.Add(tile); // Hala hide radius içinde, görünür tut
                    }
                }
                catch (MissingReferenceException)
                {
                    // Tile destroyed during processing, will be cleaned up
                    Debug.LogWarning($"HexTile was destroyed during processing");
                }
            }
        }

        currentlyVisibleTiles = newVisibleTiles;
    }

    // Destroyed/null tile'ları temizle
    private void CleanupDestroyedTiles()
    {
        // currentlyVisibleTiles'dan destroyed olanları kaldır
        currentlyVisibleTiles.RemoveWhere(tile => IsDestroyed(tile));

        // revealedTiles'dan destroyed olanları kaldır
        revealedTiles.RemoveWhere(tile => IsDestroyed(tile));
    }

    // Tile'ın destroyed olup olmadığını kontrol et
    private bool IsDestroyed(HexTile tile)
    {
        // Unity'nin null check'i destroyed objeler için true döner
        return tile == null || tile.gameObject == null;
    }

    // Chunk'lar yeniden oluşturulduğunda çağrılabilir
    public void ClearAllTileReferences()
    {
        Debug.Log("Clearing all tile references in FogOfWarManager");
        currentlyVisibleTiles.Clear();
        revealedTiles.Clear();
    }

    // Debug için
    public void LogTileStatus()
    {
        Debug.Log($"Currently Visible Tiles: {currentlyVisibleTiles.Count}");
        Debug.Log($"Total Revealed Tiles: {revealedTiles.Count}");

        int destroyedVisible = currentlyVisibleTiles.Count(tile => IsDestroyed(tile));
        int destroyedRevealed = revealedTiles.Count(tile => IsDestroyed(tile));

        if (destroyedVisible > 0 || destroyedRevealed > 0)
        {
            Debug.LogWarning(
                $"Found {destroyedVisible} destroyed visible tiles and {destroyedRevealed} destroyed revealed tiles"
            );
        }
    }

    // Gizmoz çizimi (opsiyonel)
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            // Reveal radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, revealRadius);

            // Hide radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, hideRadius);
        }
    }
}
