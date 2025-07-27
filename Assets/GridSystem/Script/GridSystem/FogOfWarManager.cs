using System.Collections.Generic;
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

        // Mevcut görünür tile'ları kontrol et
        HashSet<HexTile> newVisibleTiles = new HashSet<HexTile>();

        Collider[] nearby = Physics.OverlapSphere(player.position, revealRadius);
        foreach (var col in nearby)
        {
            HexTile tile = col.GetComponent<HexTile>();
            if (tile != null)
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
        foreach (var tile in currentlyVisibleTiles)
        {
            if (!newVisibleTiles.Contains(tile))
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
        }

        currentlyVisibleTiles = newVisibleTiles;
    }
}
