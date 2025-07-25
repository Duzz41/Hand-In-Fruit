using UnityEngine;

public class MapChunkSpawner : MonoBehaviour
{
    [Tooltip("Hazır chunk prefablarını buraya sürükle.")]
    public GameObject[] chunkPrefabs;

    public Vector2Int chunkCount = new Vector2Int(3, 3);
    public float chunkSpacing = 0.1f;

    private GameObject[,] spawnedChunks;

    [SerializeField]
    private Vector2 gridCellSize = new Vector2(10f, 10f); // Sabit grid boyutu (x: genişlik, y: derinlik)

    public void SpawnChunks()
    {
        ClearChunks();

        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogError("Chunk prefabları atanmadı!");
            return;
        }

        spawnedChunks = new GameObject[chunkCount.x, chunkCount.y];

        for (int y = 0; y < chunkCount.y; y++)
        {
            for (int x = 0; x < chunkCount.x; x++)
            {
                int randomIndex = Random.Range(0, chunkPrefabs.Length);
                GameObject prefabToSpawn = chunkPrefabs[randomIndex];

                // Sadece sabit grid boyutunu kullan
                float posX = x * (gridCellSize.x + chunkSpacing);
                float posZ = y * (gridCellSize.y + chunkSpacing);

                Vector3 spawnPos = new Vector3(posX, 0, posZ);

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
    }

    public void ClearChunks()
    {
        if (spawnedChunks == null)
            return;

        for (int y = 0; y < spawnedChunks.GetLength(1); y++)
        {
            for (int x = 0; x < spawnedChunks.GetLength(0); x++)
            {
                if (spawnedChunks[x, y] != null)
                {
#if UNITY_EDITOR
                    DestroyImmediate(spawnedChunks[x, y]);
#else
                    Destroy(spawnedChunks[x, y]);
#endif
                }
            }
        }
    }
}
