using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapChunkSpawner))]
public class MapChunkSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapChunkSpawner spawner = (MapChunkSpawner)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Clear Chunks"))
        {
            spawner.ClearChunks();
        }

        if (GUILayout.Button("Spawn Chunks"))
        {
            spawner.SpawnChunks();
        }
    }
}
