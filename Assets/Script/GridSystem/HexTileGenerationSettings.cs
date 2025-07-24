using UnityEngine;

[CreateAssetMenu(menuName = "TileGen/GenerationSettings")]
public class HexTileGenerationSettings : ScriptableObject
{
    public enum TileType
    {
        Standard,
        Water,
        Cliff
    }
    
    public GameObject Standard;
    public GameObject Water;
    public GameObject Cliff;
    
    public GameObject GetTile(TileType tileType)
    {
        switch (tileType)
        {
            case TileType.Standard:
                return Standard;
            case TileType.Water:
                return Water;
            case TileType.Cliff:
                return Cliff;
        }
        return null;
    }
}