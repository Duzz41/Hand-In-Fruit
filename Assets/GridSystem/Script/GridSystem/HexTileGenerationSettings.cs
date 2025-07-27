using UnityEngine;

[CreateAssetMenu(fileName = "New HexTileSettings", menuName = "Hex/Tile Settings")]
public class HexTileGenerationSettings : ScriptableObject
{
    public Material defaultVisibleMaterial;
    public Material defaultHiddenMaterial;

    public enum TileType
    {
        Stone,
        Iron,
        Cliff,
    }

    public GameObject stonePrefab;
    public GameObject ironPrefab;
    public GameObject cliffPrefab;

    public GameObject GetTile(TileType type)
    {
        switch (type)
        {
            case TileType.Stone:
                return stonePrefab;
            case TileType.Iron:
                return ironPrefab;
            case TileType.Cliff:
                return cliffPrefab;
        }
        return null;
    }
}
