using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HexTile : MonoBehaviour
{
    [Header("Tile Settings")]
    public HexTileGenerationSettings settings;
    public HexTileGenerationSettings.TileType tileType;
    public Vector2Int offsetCoordinates;

    [Header("Fog of War")]
    [SerializeField]
    private bool isRevealed = false;

    [SerializeField]
    private bool isCurrentlyVisible = false;

    [Header("Debug & Editor Options")]
    public bool editorForceVisible = false;

    // Private fields
    public GameObject tile;
    private List<MeshRenderer> pieceRenderers = new List<MeshRenderer>();
    private List<Material> originalMaterials = new List<Material>();
    private bool isDirty = false;

    private void Start()
    {
        // Runtime'da fog durumunu uygula
        if (Application.isPlaying && tile != null)
        {
            CachePieceRenderers();
            ApplyFogState();
        }
    }

    private void Update()
    {
        if (isDirty)
        {
            RefreshTile();
            isDirty = false;
        }
    }

    private void OnValidate()
    {
        if (tile != null)
        {
            isDirty = true;
        }
    }

    public void AddTile()
    {
        if (settings == null)
            return;

        tile = GameObject.Instantiate(settings.GetTile(tileType));
        tile.transform.SetParent(this.transform);
        tile.transform.localPosition = Vector3.zero;

        // Piece'lerin layer'ını Debris yap
        SetPiecesLayer("Debris");

        // Collider ekle
        AddMainColliderIfNeeded();

        // Runtime'daysa renderer'ları cache'le
        if (Application.isPlaying)
        {
            CachePieceRenderers();
            ApplyFogState();
        }
    }

    private void RefreshTile()
    {
        if (tile != null)
        {
            if (Application.isPlaying)
                GameObject.Destroy(tile);
            else
                GameObject.DestroyImmediate(tile);
        }

        pieceRenderers.Clear();
        originalMaterials.Clear();
        AddTile();
    }

    private void CachePieceRenderers()
    {
        pieceRenderers.Clear();
        originalMaterials.Clear();

        if (tile == null)
            return;

        // Önce direkt child piece'leri kontrol et (parçalanabilir objeler için)
        for (int i = 0; i < tile.transform.childCount; i++)
        {
            Transform child = tile.transform.GetChild(i);
            MeshRenderer renderer = child.GetComponent<MeshRenderer>();

            if (renderer != null)
            {
                pieceRenderers.Add(renderer);
                originalMaterials.Add(renderer.sharedMaterial);
            }
        }

        // Eğer child piece bulunamazsa, tile'ın kendisinde MeshRenderer var mı bak
        if (pieceRenderers.Count == 0)
        {
            MeshRenderer tileRenderer = tile.GetComponent<MeshRenderer>();
            if (tileRenderer != null)
            {
                pieceRenderers.Add(tileRenderer);
                originalMaterials.Add(tileRenderer.sharedMaterial);
            }
        }
    }

    private void SetPiecesLayer(string layerName)
    {
        if (tile == null)
            return;

        int layer = LayerMask.NameToLayer(layerName);
        if (layer == -1)
            return;

        // Tüm piece'lerin layer'ını ayarla
        for (int i = 0; i < tile.transform.childCount; i++)
        {
            tile.transform.GetChild(i).gameObject.layer = layer;
        }
    }

    private void AddMainColliderIfNeeded()
    {
        if (tile == null)
            return;

        if (tile.GetComponent<DestructibleObject>() == null)
        {
            if (gameObject.GetComponent<MeshCollider>() == null)
            {
                MeshRenderer firstRenderer = tile.GetComponentInChildren<MeshRenderer>();
                if (firstRenderer != null)
                {
                    MeshFilter filter = firstRenderer.GetComponent<MeshFilter>();
                    if (filter != null)
                    {
                        MeshCollider collider = gameObject.AddComponent<MeshCollider>();
                        collider.sharedMesh = filter.sharedMesh;
                    }
                }
            }
        }
    }

    // FOG OF WAR METHODS
    public void RevealTile()
    {
        if (!isRevealed)
        {
            isRevealed = true;
        }

        if (!isCurrentlyVisible)
        {
            isCurrentlyVisible = true;
            ShowPieces();
        }
    }

    public void HideTile()
    {
        if (Application.isPlaying && isRevealed && isCurrentlyVisible)
        {
            isCurrentlyVisible = false;
            HidePieces();
        }
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            RevealTile();
        }
        else if (isRevealed)
        {
            HideTile();
        }
    }

    private void ShowPieces()
    {
        if (pieceRenderers.Count == 0)
            return;

        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null && i < originalMaterials.Count)
            {
                pieceRenderers[i].material = originalMaterials[i];
                pieceRenderers[i].enabled = true;
            }
        }
    }

    private void HidePieces()
    {
        if (pieceRenderers.Count == 0)
            return;

        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null)
            {
                if (settings != null && settings.defaultHiddenMaterial != null)
                {
                    pieceRenderers[i].material = settings.defaultHiddenMaterial;
                }
                else
                {
                    pieceRenderers[i].enabled = false;
                }
            }
        }
    }

    private void ApplyFogState()
    {
        if (isCurrentlyVisible)
        {
            ShowPieces();
        }
        else
        {
            HidePieces();
        }
    }

    // PUBLIC PROPERTIES
    public bool IsRevealed => isRevealed;
    public bool IsCurrentlyVisible => isCurrentlyVisible;

    public void RollTileType()
    {
        tileType = (HexTileGenerationSettings.TileType)Random.Range(0, 3);
    }

    public static Vector3Int OffsetToCube(Vector2Int offset)
    {
        var q = offset.x - (offset.y - (offset.y % 2)) / 2;
        var r = offset.y;
        return new Vector3Int(q, r, -q - r);
    }
}
