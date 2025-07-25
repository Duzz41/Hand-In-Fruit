using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HexTile : MonoBehaviour
{
    public HexTileGenerationSettings settings;
    public HexTileGenerationSettings.TileType tileType;

    public GameObject tile;

    // public GameObject fow;
    public Vector2Int offsetCoordinates;

    // public Vector3Int cubeCoordinate;
    //  public List<HexTile> neighbours;
    private bool isDirty = false;

    public void RollTileType()
    {
        tileType = (HexTileGenerationSettings.TileType)Random.Range(0, 3);
    }

    public void AddTile()
    {
        tile = GameObject.Instantiate(settings.GetTile(tileType));

        // Bu HexTile objesinin altına yerleştiriyoruz
        tile.transform.SetParent(this.transform);

        // SADECE localPosition sıfırlanıyor
        tile.transform.localPosition = Vector3.zero;

        // Collider kontrolü
        if (gameObject.GetComponent<MeshCollider>() == null)
        {
            MeshFilter filter = GetComponentInChildren<MeshFilter>();
            if (filter != null)
            {
                MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
            }
        }
    }

    private void OnValidate()
    {
        if (tile == null)
            return;
        isDirty = true;
    }

    private void Update()
    {
        if (isDirty)
        {
            if (Application.isPlaying)
            {
                GameObject.Destroy(tile);
            }
            else
            {
                GameObject.DestroyImmediate(tile);
            }
            AddTile();
            isDirty = false;
        }
    }

    public static Vector3Int OffsetToCube(Vector2Int offset)
    {
        var q = offset.x - (offset.y - (offset.y % 2)) / 2;
        var r = offset.y;
        return new Vector3Int(q, r, -q - r);
    }
}
