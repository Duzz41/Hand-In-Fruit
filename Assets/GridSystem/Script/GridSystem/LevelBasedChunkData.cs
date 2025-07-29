using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelChunkData", menuName = "Chunks/LevelBasedChunkData")]
public class LevelBasedChunkData : ScriptableObject
{
    public int level;
    public GameObject[] chunkPrefabs;
}
