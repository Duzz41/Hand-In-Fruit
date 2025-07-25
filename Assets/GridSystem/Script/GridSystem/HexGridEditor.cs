using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexGrid))]
public class HexGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // Mevcut inspector'ı çiz

        HexGrid hexGrid = (HexGrid)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Layout Grid"))
        {
            hexGrid.LayoutGrid();
        }

        if (GUILayout.Button("Clear Grid"))
        {
            hexGrid.Clear();
        }
    }
}
