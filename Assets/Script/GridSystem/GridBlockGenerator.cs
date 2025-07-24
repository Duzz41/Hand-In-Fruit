using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BlockType
{
    public string blockName;
    public GameObject prefab;
    [Range(0f, 100f)]
    public float spawnPercentage;
    public Color gizmoColor = Color.white;
}

[System.Serializable]
public class ExclusionZone
{
    public string zoneName;
    public Transform zoneTransform;
    public float radius = 5f;
    public bool useBoxBounds = false;
    public Vector3 boxSize = Vector3.one * 5f;
    public Color gizmoColor = Color.red;
}

public class GridBlockGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public int maxGridWidth = 50;
    public int maxGridDepth = 50;
    public float blockSize = 1f;
    public Vector3 gridOffset = Vector3.zero;
    public float fixedHeight = 0f; // Tek katman yüksekliği
    
    [Header("Mountain Detection")]
    public LayerMask mountainLayerMask = -1;
    public float raycastDistance = 100f;
    public bool useRaycastFromAbove = true; // Yukarıdan aşağı raycast
    public float raycastHeightOffset = 50f; // Raycast başlangıç yüksekliği
    
    [Header("Block Types")]
    public List<BlockType> blockTypes = new List<BlockType>();
    
    [Header("Exclusion Zones")]
    public List<ExclusionZone> exclusionZones = new List<ExclusionZone>();
    
    [Header("Generation Settings")]
    public bool generateOnStart = true;
    public bool showGridGizmos = true;
    public bool showExclusionZones = true;
    public Transform parentTransform;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showRaycastDebug = false;
    
    // Grid referansı
    private GameObject[,,] gridArray;
    private Dictionary<Vector3Int, GameObject> blockDictionary;
    private HashSet<Vector3Int> validPositions;
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateGrid();
        }
    }
    
    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        ClearExistingGrid();
        InitializeGrid();
        CalculateValidPositions();
        PopulateGrid();
        
        if (showDebugInfo)
        {
            ShowGenerationStats();
        }
    }
    
    [ContextMenu("Clear Grid")]
    public void ClearExistingGrid()
    {
        if (parentTransform != null)
        {
            for (int i = parentTransform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                    Destroy(parentTransform.GetChild(i).gameObject);
                else
                    DestroyImmediate(parentTransform.GetChild(i).gameObject);
            }
        }
        
        gridArray = null;
        blockDictionary?.Clear();
        validPositions?.Clear();
    }
    
    void InitializeGrid()
    {
        gridArray = new GameObject[maxGridWidth, 1, maxGridDepth]; // Y ekseni sadece 1
        blockDictionary = new Dictionary<Vector3Int, GameObject>();
        validPositions = new HashSet<Vector3Int>();
        
        if (parentTransform == null)
        {
            GameObject parent = new GameObject("Generated Mountain Blocks");
            parent.transform.SetParent(transform);
            parentTransform = parent.transform;
        }
    }
    
    void CalculateValidPositions()
    {
        for (int x = 0; x < maxGridWidth; x++)
        {
            for (int z = 0; z < maxGridDepth; z++)
            {
                Vector3 blockPosition = GetWorldPosition(x, 0, z);
                
                // Exclusion zone kontrolü
                if (IsInExclusionZone(blockPosition))
                    continue;
                
                // Dağ sınırları içinde mi kontrol et
                if (IsInsideMountain(blockPosition))
                {
                    validPositions.Add(new Vector3Int(x, 0, z));
                }
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Valid positions calculated: {validPositions.Count}");
        }
    }
    
    bool IsInsideMountain(Vector3 position)
    {
        if (!useRaycastFromAbove)
            return true; // Raycast kullanmıyorsak tüm pozisyonlar geçerli
        
        // Yukarıdan aşağı raycast at
        Vector3 rayStart = new Vector3(position.x, position.y + raycastHeightOffset, position.z);
        Ray ray = new Ray(rayStart, Vector3.down);
        
        bool hitMountain = Physics.Raycast(ray, raycastDistance, mountainLayerMask);
        
        if (showRaycastDebug && hitMountain)
        {
            Debug.DrawRay(rayStart, Vector3.down * raycastDistance, Color.green, 2f);
        }
        else if (showRaycastDebug)
        {
            Debug.DrawRay(rayStart, Vector3.down * raycastDistance, Color.red, 2f);
        }
        
        return hitMountain;
    }
    
    bool IsInExclusionZone(Vector3 position)
    {
        foreach (var zone in exclusionZones)
        {
            if (zone.zoneTransform == null) continue;
            
            Vector3 zoneCenter = zone.zoneTransform.position;
            
            if (zone.useBoxBounds)
            {
                // Box bounds kontrolü
                Vector3 localPos = zone.zoneTransform.InverseTransformPoint(position);
                Vector3 halfSize = zone.boxSize * 0.5f;
                
                if (Mathf.Abs(localPos.x) <= halfSize.x &&
                    Mathf.Abs(localPos.y) <= halfSize.y &&
                    Mathf.Abs(localPos.z) <= halfSize.z)
                {
                    return true;
                }
            }
            else
            {
                // Sphere bounds kontrolü
                if (Vector3.Distance(position, zoneCenter) <= zone.radius)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    void PopulateGrid()
    {
        NormalizePercentages();
        
        foreach (var gridPos in validPositions)
        {
            Vector3 worldPosition = GetWorldPosition(gridPos.x, 0, gridPos.z);
            worldPosition.y = fixedHeight; // Sabit yükseklik
            
            // Final exclusion zone check
            if (IsInExclusionZone(worldPosition))
                continue;
            
            BlockType selectedBlockType = SelectBlockType();
            
            if (selectedBlockType != null && selectedBlockType.prefab != null)
            {
                GameObject newBlock = Instantiate(selectedBlockType.prefab, worldPosition, Quaternion.identity, parentTransform);
                newBlock.name = $"{selectedBlockType.blockName}_{gridPos.x}_0_{gridPos.z}";
                
                gridArray[gridPos.x, 0, gridPos.z] = newBlock;
                blockDictionary[gridPos] = newBlock;
                
                BlockInfo blockInfo = newBlock.GetComponent<BlockInfo>();
                if (blockInfo == null)
                {
                    blockInfo = newBlock.AddComponent<BlockInfo>();
                }
                blockInfo.gridPosition = gridPos;
                blockInfo.blockType = selectedBlockType.blockName;
            }
        }
    }
    
    void NormalizePercentages()
    {
        float totalPercentage = 0f;
        foreach (var blockType in blockTypes)
        {
            totalPercentage += blockType.spawnPercentage;
        }
        
        if (totalPercentage > 100f)
        {
            float normalizationFactor = 100f / totalPercentage;
            foreach (var blockType in blockTypes)
            {
                blockType.spawnPercentage *= normalizationFactor;
            }
        }
    }
    
    BlockType SelectBlockType()
    {
        float randomValue = Random.Range(0f, 100f);
        float cumulativePercentage = 0f;
        
        foreach (var blockType in blockTypes)
        {
            cumulativePercentage += blockType.spawnPercentage;
            if (randomValue <= cumulativePercentage)
            {
                return blockType;
            }
        }
        
        return blockTypes.Count > 0 ? blockTypes[0] : null;
    }
    
    Vector3 GetWorldPosition(int x, int y, int z)
    {
        return transform.position + gridOffset + new Vector3(x * blockSize, fixedHeight, z * blockSize);
    }
    
    public Vector3Int GetGridPosition(Vector3 worldPosition)
    {
        Vector3 localPosition = worldPosition - transform.position - gridOffset;
        return new Vector3Int(
            Mathf.FloorToInt(localPosition.x / blockSize),
            0, // Y her zaman 0
            Mathf.FloorToInt(localPosition.z / blockSize)
        );
    }
    
    public GameObject GetBlockAtPosition(Vector3Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition))
        {
            return gridArray[gridPosition.x, gridPosition.y, gridPosition.z];
        }
        return null;
    }
    
    public bool RemoveBlock(Vector3Int gridPosition)
    {
        if (IsValidGridPosition(gridPosition) && gridArray[gridPosition.x, gridPosition.y, gridPosition.z] != null)
        {
            GameObject blockToRemove = gridArray[gridPosition.x, gridPosition.y, gridPosition.z];
            gridArray[gridPosition.x, gridPosition.y, gridPosition.z] = null;
            blockDictionary.Remove(gridPosition);
            
            if (Application.isPlaying)
                Destroy(blockToRemove);
            else
                DestroyImmediate(blockToRemove);
            
            return true;
        }
        return false;
    }
    
    bool IsValidGridPosition(Vector3Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < maxGridWidth &&
               gridPosition.y == 0 && // Y her zaman 0 olmalı
               gridPosition.z >= 0 && gridPosition.z < maxGridDepth;
    }
    
    void ShowGenerationStats()
    {
        Dictionary<string, int> blockCounts = new Dictionary<string, int>();
        
        foreach (var block in blockDictionary.Values)
        {
            if (block != null)
            {
                BlockInfo info = block.GetComponent<BlockInfo>();
                if (info != null)
                {
                    if (blockCounts.ContainsKey(info.blockType))
                        blockCounts[info.blockType]++;
                    else
                        blockCounts[info.blockType] = 1;
                }
            }
        }
        
        Debug.Log("=== Mountain Grid Generation Stats ===");
        Debug.Log($"Valid positions: {validPositions.Count}");
        Debug.Log($"Generated blocks: {blockDictionary.Count}");
        
        foreach (var kvp in blockCounts)
        {
            float percentage = (kvp.Value / (float)blockDictionary.Count) * 100f;
            Debug.Log($"{kvp.Key}: {kvp.Value} blocks ({percentage:F1}%)");
        }
    }
    
    void OnDrawGizmos()
    {
        if (showGridGizmos)
        {
            Gizmos.color = Color.white;
            Vector3 gridSize = new Vector3(maxGridWidth * blockSize, 1f, maxGridDepth * blockSize);
            Vector3 center = transform.position + gridOffset + new Vector3(gridSize.x * 0.5f, fixedHeight, gridSize.z * 0.5f);
            Gizmos.DrawWireCube(center, gridSize);
        }
        
        // Exclusion zones'ları çiz
        if (showExclusionZones)
        {
            foreach (var zone in exclusionZones)
            {
                if (zone.zoneTransform == null) continue;
                
                Gizmos.color = zone.gizmoColor;
                
                if (zone.useBoxBounds)
                {
                    Gizmos.matrix = zone.zoneTransform.localToWorldMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, zone.boxSize);
                    Gizmos.matrix = Matrix4x4.identity;
                }
                else
                {
                    Gizmos.DrawWireSphere(zone.zoneTransform.position, zone.radius);
                }
            }
        }
        
        // Valid positions'ları göster (2D düzlemde)
        if (Application.isEditor && UnityEditor.Selection.activeGameObject == gameObject && validPositions != null)
        {
            Gizmos.color = Color.green;
            foreach (var pos in validPositions)
            {
                Vector3 worldPos = GetWorldPosition(pos.x, 0, pos.z);
                worldPos.y = fixedHeight;
                Gizmos.DrawWireCube(worldPos, Vector3.one * blockSize * 0.8f);
            }
        }
    }
}

public class BlockInfo : MonoBehaviour
{
    public Vector3Int gridPosition;
    public string blockType;
    public int hardness = 1;
    public int resourceValue = 1;
}