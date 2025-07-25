using System.Collections.Generic;
using UnityEditor.Playables;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2Int gridSize;
    public float radius = 1f;

    //public GameObject prefab;
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
                hexTile.AddTile();
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
                hexTile.AddTile();
            }
        }
    }
}
