using System.Collections.Generic;
using UnityEngine;

public class MapChunkSpawner : MonoBehaviour
{
    [Header("Seviye Bazlı Chunk Listesi")]
    [Tooltip("Her seviye için chunk prefablarını tanımlayın.")]
    public List<LevelBasedChunkData> levelChunks;

    [Header("Aktif Seviye")]
    [Tooltip("Şu an yüklenecek olan seviye.")]
    public int currentLevel = 1;

    [Header("Chunk Yerleşimi")]
    public Vector2Int chunkCount = new Vector2Int(3, 3);
    public float chunkSpacing = 0.1f;

    [SerializeField]
    private Vector2 gridCellSize = new Vector2(10f, 10f); // Chunk boyutları (X: genişlik, Y: derinlik)

    private GameObject[,] spawnedChunks;

    // --- Chunk Spawn ---

    public void SpawnChunks()
    {
        ClearChunks();

        GameObject[] selectedChunks = GetChunksForCurrentLevel();

        if (selectedChunks == null || selectedChunks.Length == 0)
        {
            Debug.LogError($"Seviye {currentLevel} için chunk prefabı bulunamadı!");
            return;
        }

        spawnedChunks = new GameObject[chunkCount.x, chunkCount.y];

        for (int y = 0; y < chunkCount.y; y++)
        {
            for (int x = 0; x < chunkCount.x; x++)
            {
                int randomIndex = Random.Range(0, selectedChunks.Length);
                GameObject prefabToSpawn = selectedChunks[randomIndex];

                float posX = x * (gridCellSize.x + chunkSpacing);
                float posZ = y * (gridCellSize.y + chunkSpacing);
                Vector3 spawnPos = new Vector3(posX, transform.position.y, posZ);

                GameObject chunk = Instantiate(
                    prefabToSpawn,
                    spawnPos,
                    Quaternion.identity,
                    this.transform
                );
                chunk.name = $"Chunk_{x}_{y}";
                spawnedChunks[x, y] = chunk;
            }
        }

        Debug.Log(
            $"Seviye {currentLevel} için {chunkCount.x * chunkCount.y} chunk başarıyla spawn edildi."
        );
    }

    private GameObject[] GetChunksForCurrentLevel()
    {
        foreach (var data in levelChunks)
        {
            if (data.level == currentLevel)
            {
                return data.chunkPrefabs;
            }
        }

        return null;
    }

    // --- Chunk Temizleme ---

    public void ClearChunks()
    {
        Debug.Log("ClearChunks started...");

        // Fog of War ile haberleş
        FogOfWarManager fogManager = FindObjectOfType<FogOfWarManager>();
        if (fogManager != null)
        {
            fogManager.ClearAllTileReferences();
        }

        // Array üzerinden temizle
        if (spawnedChunks != null)
        {
            for (int y = 0; y < spawnedChunks.GetLength(1); y++)
            {
                for (int x = 0; x < spawnedChunks.GetLength(0); x++)
                {
                    if (spawnedChunks[x, y] != null)
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying)
                            Destroy(spawnedChunks[x, y]);
                        else
                            DestroyImmediate(spawnedChunks[x, y]);
#else
                        Destroy(spawnedChunks[x, y]);
#endif
                        spawnedChunks[x, y] = null;
                    }
                }
            }
        }

        // Sahnedeki orphan chunk'ları da sil
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("Chunk_"))
            {
                children.Add(child);
            }
        }

        foreach (Transform child in children)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }

        spawnedChunks = null;

        Debug.Log("ClearChunks completed.");
    }

    // --- Utility ---

    private void OnValidate()
    {
        chunkCount.x = Mathf.Max(1, chunkCount.x);
        chunkCount.y = Mathf.Max(1, chunkCount.y);
        gridCellSize.x = Mathf.Max(0.1f, gridCellSize.x);
        gridCellSize.y = Mathf.Max(0.1f, gridCellSize.y);
    }

    private void Start()
    {
        RegisterExistingChunks();
    }

    private void RegisterExistingChunks()
    {
        int found = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).name.StartsWith("Chunk_"))
            {
                found++;
            }
        }

        if (found > 0)
        {
            Debug.Log($"Sahne içinde {found} adet chunk bulundu.");
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Force Clear All Chunks")]
    public void ForceClearAllChunks()
    {
        ClearChunks();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Spawn New Chunks")]
    public void EditorSpawnChunks()
    {
        SpawnChunks();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
