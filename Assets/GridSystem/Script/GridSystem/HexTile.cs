using System.Collections;
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

    [SerializeField]
    private bool isInXRayMode = false;

    [Header("Valuable Resource")]
    public bool hasValuableResource = false; // Inspector'dan ayarlanabilir

    [Range(0f, 1f)]
    public float valuableResourceChance = 0.1f; // %10 şans ile değerli kaynak

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

        // Valuable resource'u otomatik belirle
        DetermineValuableResource();

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

        // DestructibleObject dahil altındaki tüm MeshRenderer'ları bul
        MeshRenderer[] renderers = tile.GetComponentsInChildren<MeshRenderer>(true); // true: inactive objeler dahil

        foreach (MeshRenderer renderer in renderers)
        {
            pieceRenderers.Add(renderer);
            originalMaterials.Add(renderer.sharedMaterial);
        }

        Debug.Log($"[{gameObject.name}] Cached {pieceRenderers.Count} renderers.");
    }

    // Yeni metod - recursive olarak tüm alt nesneleri tara
    private void CacheDestructibleRenderers(Transform parent)
    {
        // Bu nesnenin kendi renderer'ını kontrol et
        MeshRenderer renderer = parent.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            pieceRenderers.Add(renderer);
            originalMaterials.Add(renderer.sharedMaterial);
        }

        // Tüm çocukları recursive olarak kontrol et
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            CacheDestructibleRenderers(child);
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
        Debug.Log($"[{gameObject.name}] RevealTile CALLED!");
        isCurrentlyVisible = true;
        isRevealed = true;

        // X-Ray modunu kapat
        if (isInXRayMode)
        {
            SetXRayMode(false);
        }

        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null && originalMaterials[i] != null)
            {
                pieceRenderers[i].sharedMaterial = originalMaterials[i];
            }
        }
    }

    public void HideTile()
    {
        if (Application.isPlaying)
        {
            if (!isRevealed)
            {
                isCurrentlyVisible = false;
                HidePieces();
            }
            else if (!hasValuableResource) // Eğer değerli kaynak yoksa gizle
            {
                RestoreOriginalMaterials(); // Normal materyali geri yükle
            }
        }
    }

    // X-RAY METHODS
    public void SetXRayMode(bool enable)
    {
        isInXRayMode = enable;

        if (enable)
        {
            // Sadece hiç reveal edilmemiş (hidden) tile'lara X-Ray uygula
            if (!isRevealed)
            {
                ApplyXRayMaterial();
            }
        }
        else
        {
            // X-Ray bitince: eğer hiç reveal edilmemişse tekrar hidden yap
            if (!isRevealed)
            {
                HidePieces();
            }
            else
            {
                // Eğer reveal edilmişse, normal materyali geri yükle
                RestoreOriginalMaterials();
            }
        }
    }

    private void RestoreOriginalMaterials()
    {
        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null && originalMaterials[i] != null)
            {
                pieceRenderers[i].materials = new Material[] { originalMaterials[i] };
            }
        }
    }

    private void ApplyXRayMaterial()
    {
        if (settings == null)
            return;

        Material xrayMat = hasValuableResource
            ? settings.xrayValuableMaterial
            : settings.xrayNormalMaterial;

        if (xrayMat == null)
            return;

        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null)
            {
                pieceRenderers[i].enabled = true;
                Material[] materials = new Material[pieceRenderers[i].materials.Length];
                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = xrayMat;
                }
                pieceRenderers[i].materials = materials;
            }
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
        {
            Debug.LogWarning($"No renderers cached for {gameObject.name}");
            return;
        }

        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null)
            {
                pieceRenderers[i].enabled = true;
            }
        }

        // Zorla material güncellemesi yap
        ForceMaterialUpdate();
    }

    private void HidePieces()
    {
        if (pieceRenderers.Count == 0)
            return;

        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null)
            {
                if (settings == null || settings.defaultHiddenMaterial == null)
                {
                    pieceRenderers[i].enabled = false;
                }
                else
                {
                    pieceRenderers[i].enabled = true;
                }
            }
        }

        // Zorla material güncellemesi yap
        if (settings != null && settings.defaultHiddenMaterial != null)
        {
            ForceMaterialUpdate();
        }
    }

    // Material değişimini zorla uygula
    private void ForceMaterialUpdate()
    {
        StartCoroutine(ForceUpdateCoroutine());
    }

    private IEnumerator ForceUpdateCoroutine()
    {
        // Bir frame bekle
        yield return null;

        // Material'ları tekrar uygula
        if (isCurrentlyVisible)
        {
            ApplyMaterialsDirectly(false); // Original materials
        }
        else
        {
            ApplyMaterialsDirectly(true); // Hidden material
        }
    }

    private void ApplyMaterialsDirectly(bool useHiddenMaterial)
    {
        for (int i = 0; i < pieceRenderers.Count; i++)
        {
            if (pieceRenderers[i] != null)
            {
                // Occlusion culling durumunu kontrol et
                if (!pieceRenderers[i].isVisible)
                {
                    // Görünmezse zorla görünür yap
                    pieceRenderers[i].forceRenderingOff = false;
                }

                if (useHiddenMaterial && settings != null && settings.defaultHiddenMaterial != null)
                {
                    // Material array'i kullanarak zorla değiştir
                    Material[] materials = new Material[pieceRenderers[i].materials.Length];
                    for (int j = 0; j < materials.Length; j++)
                    {
                        materials[j] = settings.defaultHiddenMaterial;
                    }
                    pieceRenderers[i].materials = materials;
                }
                else if (!useHiddenMaterial && i < originalMaterials.Count)
                {
                    // Orijinal material'ı geri yükle
                    Material[] materials = new Material[pieceRenderers[i].materials.Length];
                    for (int j = 0; j < materials.Length; j++)
                    {
                        materials[j] = originalMaterials[i];
                    }
                    pieceRenderers[i].materials = materials;
                }
            }
        }
    }

    private void ValidateRenderers()
    {
        // Null renderer'ları temizle
        for (int i = pieceRenderers.Count - 1; i >= 0; i--)
        {
            if (pieceRenderers[i] == null)
            {
                pieceRenderers.RemoveAt(i);
                if (i < originalMaterials.Count)
                    originalMaterials.RemoveAt(i);
            }
        }

        // Liste boşsa yeniden cache'le
        if (pieceRenderers.Count == 0 && tile != null)
        {
            CachePieceRenderers();
        }
    }

    private void ApplyFogState()
    {
        ValidateRenderers();

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
    public bool IsInXRayMode => isInXRayMode;

    public void RollTileType()
    {
        tileType = (HexTileGenerationSettings.TileType)Random.Range(0, 3);

        // Tile type değişince valuable resource'u yeniden hesapla
        if (Application.isPlaying)
        {
            OnTileTypeChanged();
        }
    }

    public static Vector3Int OffsetToCube(Vector2Int offset)
    {
        var q = offset.x - (offset.y - (offset.y % 2)) / 2;
        var r = offset.y;
        return new Vector3Int(q, r, -q - r);
    }

    // VALUABLE RESOURCE METHODS
    private void DetermineValuableResource()
    {
        // Eğer settings'te bu tile type için özel ayar varsa onu kullan
        if (settings != null && settings.HasValuableResourceSettings())
        {
            hasValuableResource = settings.IsValuableResourceType(tileType);
        }
        else
        {
            // Rastgele belirle
            hasValuableResource = Random.value < valuableResourceChance;
        }

        // Debug için
        if (hasValuableResource)
        {
            Debug.Log($"[{gameObject.name}] Valuable resource detected! Type: {tileType}");
        }
    }

    // Manuel olarak valuable resource durumunu değiştir
    public void SetValuableResource(bool isValuable)
    {
        hasValuableResource = isValuable;

        // Eğer şu anda X-Ray modundaysa ve reveal edilmemişse materyali güncelle
        if (isInXRayMode && !isRevealed)
        {
            ApplyXRayMaterial();
        }
    }

    // Tile type değişince valuable resource'u yeniden hesapla
    public void OnTileTypeChanged()
    {
        DetermineValuableResource();
    }
}
