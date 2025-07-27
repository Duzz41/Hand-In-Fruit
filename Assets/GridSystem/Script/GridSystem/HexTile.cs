using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HexTile : MonoBehaviour
{
    public HexTileGenerationSettings settings;
    public HexTileGenerationSettings.TileType tileType;

    public GameObject tile;

    private Material originalMaterial; // prefabın kendi materyali
    public Vector2Int offsetCoordinates;

    // Fog of War için yeni değişkenler
    private bool isRevealed = false; // Bir kere açıldı mı?
    private bool isCurrentlyVisible = false; // Şu anda görünür mü?
    private MeshRenderer tileRenderer;

    private void Start()
    {
        Debug.Log($"HexTile Start called on {name}");

        // Eğer tile zaten varsa (runtime'da oluşturulmuşsa)
        if (tile != null)
        {
            InitializeTileRenderer();
        }
        else
        {
            Debug.Log($"No tile object found on {name} during Start - will be created later");
        }
    }

    private void InitializeTileRenderer()
    {
        tileRenderer = tile.GetComponentInChildren<MeshRenderer>();

        if (tileRenderer != null)
        {
            // Original material'ı kaydet (eğer henüz kaydedilmemişse)
            if (originalMaterial == null)
            {
                originalMaterial = tileRenderer.sharedMaterial;
                Debug.Log(
                    $"[InitializeTileRenderer] Original material saved: {originalMaterial?.name} for {name}"
                );
            }

            // Başlangıçta gizli material ile göster
            if (settings != null && settings.defaultHiddenMaterial != null)
            {
                Debug.Log($"Setting initial hidden material on {name}");
                tileRenderer.material = settings.defaultHiddenMaterial;
            }
            else
            {
                Debug.LogWarning(
                    $"Cannot set initial hidden material on {name}. settings: {settings != null}, hiddenMaterial: {settings?.defaultHiddenMaterial != null}"
                );
            }
        }
        else
        {
            Debug.LogError($"No MeshRenderer found in children of tile {name}");
        }
    }

    // Fog of War için yeni method
    public void RevealTile()
    {
        Debug.Log(
            $"RevealTile called on {name}. isRevealed: {isRevealed}, isCurrentlyVisible: {isCurrentlyVisible}"
        );

        // Eğer tile henüz oluşturulmamışsa, önce oluştur
        if (tile == null)
        {
            Debug.Log($"Tile is null, calling AddTile for {name}");
            AddTile();
        }

        // Tile renderer yoksa tekrar initialize et
        if (tileRenderer == null || originalMaterial == null)
        {
            Debug.Log($"TileRenderer or originalMaterial is null, reinitializing for {name}");
            InitializeTileRenderer();
        }

        if (!isRevealed)
        {
            isRevealed = true;
            Debug.Log($"Tile revealed for first time: {name}");
        }

        if (!isCurrentlyVisible)
        {
            isCurrentlyVisible = true;
            Debug.Log($"Setting tile {name} to visible with original material");
            ShowWithOriginalMaterial();
        }
    }

    // Tile'ı player'dan uzaklaştığında gizle (ama sadece daha önce açılmışsa)
    public void HideTile()
    {
        if (isRevealed && isCurrentlyVisible)
        {
            isCurrentlyVisible = false;
            ShowWithHiddenMaterial();
        }
    }

    private void ShowWithOriginalMaterial()
    {
        Debug.Log(
            $"ShowWithOriginalMaterial called. tile: {tile != null}, tileRenderer: {tileRenderer != null}, originalMaterial: {originalMaterial != null}"
        );

        if (tile != null && tileRenderer != null && originalMaterial != null)
        {
            Debug.Log(
                $"Changing material from {tileRenderer.material.name} to {originalMaterial.name}"
            );
            tileRenderer.material = originalMaterial;
        }
        else
        {
            Debug.LogError($"Cannot show original material. Missing components on {name}");
        }
    }

    private void ShowWithHiddenMaterial()
    {
        Debug.Log(
            $"ShowWithHiddenMaterial called. tile: {tile != null}, tileRenderer: {tileRenderer != null}, settings: {settings != null}"
        );

        if (tile != null && tileRenderer != null && settings != null)
        {
            Debug.Log(
                $"Changing material from {tileRenderer.material.name} to {settings.defaultHiddenMaterial.name}"
            );
            tileRenderer.material = settings.defaultHiddenMaterial;
        }
        else
        {
            Debug.LogError($"Cannot show hidden material. Missing components on {name}");
        }
    }

    // Eski SetVisible method'unu kaldırıyoruz ve yeni logic ekliyoruz
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

    private bool isDirty = false;

    public void RollTileType()
    {
        tileType = (HexTileGenerationSettings.TileType)Random.Range(0, 3);
    }

    public void AddTile()
    {
        tile = GameObject.Instantiate(settings.GetTile(tileType));
        tile.transform.SetParent(this.transform);
        tile.transform.localPosition = Vector3.zero;

        // Tile renderer'ı initialize et
        InitializeTileRenderer();

        // Collider ekle
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
