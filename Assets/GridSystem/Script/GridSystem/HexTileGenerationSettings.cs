using UnityEngine;

[CreateAssetMenu(fileName = "New HexTileSettings", menuName = "Hex/Tile Settings")]
public class HexTileGenerationSettings : ScriptableObject
{
    [Header("Materials")]
    public Material defaultVisibleMaterial;
    public Material defaultHiddenMaterial;
    public Material xrayNormalMaterial;
    public Material xrayValuableMaterial;

    [Header("Valuable Resource Settings")]
    public bool useValuableResourceSettings = false;
    public TileType[] valuableResourceTypes;

    public enum TileType
    {
        Stone,
        Iron,
        Cliff,
        Emerald,
        Gold,
    }

    [Header("Tile Prefabs")]
    public GameObject stonePrefab;
    public GameObject ironPrefab;
    public GameObject cliffPrefab;
    public GameObject EmeraldPrefab;
    public GameObject GoldPrefab;

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
            case TileType.Emerald:
                return EmeraldPrefab;
            case TileType.Gold:
                return GoldPrefab;
        }
        return null;
    }

    public bool HasValuableResourceSettings()
    {
        return useValuableResourceSettings
            && valuableResourceTypes != null
            && valuableResourceTypes.Length > 0;
    }

    public bool IsValuableResourceType(TileType tileType)
    {
        if (!HasValuableResourceSettings())
            return false;

        foreach (TileType valuable in valuableResourceTypes)
        {
            if (valuable == tileType)
                return true;
        }
        return false;
    }
}
