using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int gridSize;
    public float radius = 1f;
    public bool isFlatTopped;
    public HexTileGenerationSettings settings;

    public void Clear()
    {
        List<GameObject> children = new List<GameObject>();

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            children.Add(child);
        }

        foreach (GameObject child in children)
        {
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child, true);
        }
    }

    public void LayoutGrid()
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                GameObject tileGO = new GameObject($"Hex C{x},R{y}");
                tileGO.transform.SetParent(this.transform);

                Vector3 pos = GetTilePosition(x, y, radius, isFlatTopped);
                tileGO.transform.position = pos;

                HexTile hexTile = tileGO.AddComponent<HexTile>();
                hexTile.settings = settings;
                hexTile.offsetCoordinates = new Vector2Int(x, y);

                hexTile.RollTileType();
                hexTile.AddTile(); // ESKİ HALİNE GERİ DÖNDÜ
            }
        }
    }

    Vector3 GetTilePosition(int x, int y, float radius, bool isFlatTopped)
    {
        if (isFlatTopped)
        {
            float width = radius * 2f;
            float height = Mathf.Sqrt(3) * radius;
            float horiz = width * 0.75f;
            float vert = height;

            float posX = x * horiz;
            float posZ = y * vert + (x % 2 == 0 ? 0f : vert / 2f);
            return new Vector3(posX, 0f, posZ);
        }
        else
        {
            float width = Mathf.Sqrt(3) * radius;
            float height = radius * 2f;
            float horiz = width;
            float vert = height * 0.75f;

            float posX = x * horiz + (y % 2 == 0 ? 0f : horiz / 2f);
            float posZ = y * vert;
            return new Vector3(posX, 0f, posZ);
        }
    }

    public void RollTiles()
    {
        foreach (Transform child in transform)
        {
            HexTile hexTile = child.GetComponent<HexTile>();
            if (hexTile != null)
            {
                hexTile.RollTileType();
                hexTile.AddTile(); // ESKİ HALİNE GERİ DÖNDÜ
            }
        }
    }

    // FOG OF WAR HELPER METHODS
    public HexTile GetTile(int x, int y)
    {
        string tileName = $"Hex C{x},R{y}";
        Transform tileTransform = transform.Find(tileName);
        if (tileTransform != null)
        {
            return tileTransform.GetComponent<HexTile>();
        }
        return null;
    }

    public void RevealTile(int x, int y)
    {
        HexTile tile = GetTile(x, y);
        if (tile != null)
        {
            tile.RevealTile();
        }
    }

    public void HideTile(int x, int y)
    {
        HexTile tile = GetTile(x, y);
        if (tile != null)
        {
            tile.HideTile();
        }
    }

    public void RevealTilesInRadius(Vector2Int center, int radius)
    {
        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                Vector2Int current = new Vector2Int(x, y);
                if (GetHexDistance(center, current) <= radius)
                {
                    RevealTile(x, y);
                }
            }
        }
    }

    public int GetHexDistance(Vector2Int a, Vector2Int b)
    {
        Vector3Int cubeA = HexTile.OffsetToCube(a);
        Vector3Int cubeB = HexTile.OffsetToCube(b);

        return (
                Mathf.Abs(cubeA.x - cubeB.x)
                + Mathf.Abs(cubeA.y - cubeB.y)
                + Mathf.Abs(cubeA.z - cubeB.z)
            ) / 2;
    }
}
