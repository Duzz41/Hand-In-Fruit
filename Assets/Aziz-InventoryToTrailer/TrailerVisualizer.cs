using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ResourceVisual
{
    public string resourceType;
    public GameObject resourcePrefab;
}

[RequireComponent(typeof(BoxCollider))]
public class TrailerVisualizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The BoxCollider (set to 'Is Trigger') that defines the volume for stacking.")]
    [SerializeField]
    private BoxCollider stackingVolume;

    [Header("Stacking Configuration")]
    [Tooltip("Spacing to leave between each box on all axes.")]
    public Vector3 spacing = Vector3.zero;

    [Header("Collider Height Control")]
    [Tooltip("Additional height to add to the collider from the top edge (Y axis).")]
    [SerializeField]
    [Range(0f, 10f)]
    public float additionalHeight = 0f;

    [Header("Resource Mapping")]
    public List<ResourceVisual> resourceVisuals;

    private Dictionary<string, GameObject> _resourcePrefabDict;
    private List<GameObject> _instantiatedBoxes = new List<GameObject>();
    private Vector3 _nextPosition;
    private float _currentLayerMaxHeight = 0f;
    private float _currentRowMaxDepth = 0f;
    private bool _isInitialized = false;
    private bool _isFull = false;
    private Vector3 _originalSize;
    private Vector3 _originalCenter;

    void Awake()
    {
        if (stackingVolume == null)
        {
            stackingVolume = GetComponent<BoxCollider>();
        }
        stackingVolume.isTrigger = true;

        // Store original collider dimensions
        _originalSize = stackingVolume.size;
        _originalCenter = stackingVolume.center;

        _resourcePrefabDict = new Dictionary<string, GameObject>();
        foreach (var visual in resourceVisuals)
        {
            if (!_resourcePrefabDict.ContainsKey(visual.resourceType))
            {
                _resourcePrefabDict.Add(visual.resourceType, visual.resourcePrefab);
            }
        }
    }

    void Start()
    {
        UpdateColliderHeight();
    }

    void OnValidate()
    {
        // Update collider height when the value changes in the inspector
        if (stackingVolume != null)
        {
            UpdateColliderHeight();
        }
    }

    void OnEnable()
    {
        InventoryManager.OnResourceCollected += HandleResourceCollected;
        InventoryManager.OnResourceUsed += HandleResourceUsed;
        InventoryManager.OnInventoryCleared += HandleInventoryCleared;
    }

    void OnDisable()
    {
        InventoryManager.OnResourceCollected -= HandleResourceCollected;
        InventoryManager.OnResourceUsed -= HandleResourceUsed;
        InventoryManager.OnInventoryCleared -= HandleInventoryCleared;
    }

    private void UpdateColliderHeight()
    {
        if (stackingVolume == null) return;

        // Calculate new size with additional height
        Vector3 newSize = _originalSize;
        newSize.y += additionalHeight;

        // Calculate new center to keep the bottom edge in the same position
        // When we increase height, we need to move the center up by half the added height
        Vector3 newCenter = _originalCenter;
        newCenter.y += additionalHeight * 0.5f;

        stackingVolume.size = newSize;
        stackingVolume.center = newCenter;
    }

    void InitializePosition()
    {
        _nextPosition = stackingVolume.center - stackingVolume.size / 2;
        _isInitialized = true;
    }

    private void HandleResourceCollected(string resourceType, int amount)
    {
        if (_isFull) return;

        if (_resourcePrefabDict.TryGetValue(resourceType, out GameObject prefabToSpawn))
        {
            // Loop 'amount' times to place a box for each unit.
            for (int i = 0; i < amount; i++)
            {
                if (_isFull) break; // Stop if the trailer becomes full during the loop.
                PlaceResourceBox(prefabToSpawn);
            }
        }
        else
        {
            Debug.LogWarning($"[TrailerVisualizer] No prefab found for resource type: {resourceType}");
        }
    }

    private void HandleResourceUsed(string resourceType, int amount)
    {
        if (_resourcePrefabDict.TryGetValue(resourceType, out GameObject prefabType))
        {
            // Remove visual boxes from the end of the list (LIFO - Last In, First Out)
            int boxesToRemove = Mathf.Min(amount, _instantiatedBoxes.Count);

            for (int i = 0; i < boxesToRemove; i++)
            {
                if (_instantiatedBoxes.Count > 0)
                {
                    GameObject lastBox = _instantiatedBoxes[_instantiatedBoxes.Count - 1];

                    // Check if this box matches the resource type being used
                    // This is a simple check - you might want to store resource type info with each box for better accuracy
                    if (lastBox != null)
                    {
                        _instantiatedBoxes.RemoveAt(_instantiatedBoxes.Count - 1);

                        if (Application.isPlaying)
                            Destroy(lastBox);
                        else
                            DestroyImmediate(lastBox);
                    }
                }
            }

            // If we removed boxes, trailer is no longer full and we might need to recalculate positions
            if (boxesToRemove > 0)
            {
                _isFull = false;
                RecalculateNextPosition();
            }

            Debug.Log($"[TrailerVisualizer] Removed {boxesToRemove} visual boxes for {resourceType}");
        }
    }

    private void HandleInventoryCleared()
    {
        ClearVisuals();
        Debug.Log("[TrailerVisualizer] Trailer visuals cleared due to inventory clear.");
    }

    private void RecalculateNextPosition()
    {
        if (_instantiatedBoxes.Count == 0)
        {
            _isInitialized = false;
            _currentLayerMaxHeight = 0f;
            _currentRowMaxDepth = 0f;
            return;
        }

        // Simple approach: reset to initial position and "simulate" placing all existing boxes
        // This recalculates where the next box should go
        _nextPosition = stackingVolume.center - stackingVolume.size / 2;
        _currentLayerMaxHeight = 0f;
        _currentRowMaxDepth = 0f;

        foreach (var box in _instantiatedBoxes)
        {
            if (box != null)
            {
                MeshFilter meshFilter = box.GetComponentInChildren<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Vector3 localBoxSize = Vector3.Scale(meshFilter.sharedMesh.bounds.size, box.transform.localScale);

                    // Update position tracking (simplified version of PlaceResourceBox logic)
                    if (_nextPosition.x + localBoxSize.x > stackingVolume.center.x + stackingVolume.size.x / 2)
                    {
                        _nextPosition.x = stackingVolume.center.x - stackingVolume.size.x / 2;
                        _nextPosition.z += _currentRowMaxDepth + spacing.z;
                        _currentRowMaxDepth = 0f;
                    }

                    if (_nextPosition.z + localBoxSize.z > stackingVolume.center.z + stackingVolume.size.z / 2)
                    {
                        _nextPosition.x = stackingVolume.center.x - stackingVolume.size.x / 2;
                        _nextPosition.z = stackingVolume.center.z - stackingVolume.size.z / 2;
                        _nextPosition.y += _currentLayerMaxHeight + spacing.y;
                        _currentLayerMaxHeight = 0f;
                    }

                    _currentRowMaxDepth = Mathf.Max(_currentRowMaxDepth, localBoxSize.z);
                    _currentLayerMaxHeight = Mathf.Max(_currentLayerMaxHeight, localBoxSize.y);
                    _nextPosition.x += localBoxSize.x + spacing.x;
                }
            }
        }
    }

    private void PlaceResourceBox(GameObject prefab)
    {
        if (!_isInitialized)
        {
            InitializePosition();
        }

        GameObject newBox = Instantiate(prefab, this.transform);

        // --- FIXED: Reliable Size Calculation ---
        // Using MeshFilter is much more reliable than Renderer.bounds, as it is not affected by world rotation.
        MeshFilter meshFilter = newBox.GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("Resource prefab needs a MeshFilter with a mesh.", newBox);
            Destroy(newBox);
            return;
        }

        // Calculate the actual size of the box by scaling the mesh's base size.
        Vector3 localBoxSize = Vector3.Scale(meshFilter.sharedMesh.bounds.size, newBox.transform.localScale);

        // --- Stacking Logic ---
        // NOTE: This logic will still create gaps if your resource prefabs have different depths or heights.
        // The next row/layer is positioned based on the largest object in the previous row/layer.

        if (_nextPosition.x + localBoxSize.x > stackingVolume.center.x + stackingVolume.size.x / 2)
        {
            _nextPosition.x = stackingVolume.center.x - stackingVolume.size.x / 2;
            _nextPosition.z += _currentRowMaxDepth + spacing.z;
            _currentRowMaxDepth = 0f;
        }

        if (_nextPosition.z + localBoxSize.z > stackingVolume.center.z + stackingVolume.size.z / 2)
        {
            _nextPosition.x = stackingVolume.center.x - stackingVolume.size.x / 2;
            _nextPosition.z = stackingVolume.center.z - stackingVolume.size.z / 2;
            _nextPosition.y += _currentLayerMaxHeight + spacing.y;
            _currentLayerMaxHeight = 0f;
        }

        if (_nextPosition.y + localBoxSize.y > stackingVolume.center.y + stackingVolume.size.y / 2)
        {
            Debug.Log("Trailer is full!");
            _isFull = true;
            Destroy(newBox);
            return;
        }

        Vector3 boxCenterLocalPosition = _nextPosition + new Vector3(localBoxSize.x / 2, localBoxSize.y / 2, localBoxSize.z / 2);
        newBox.transform.position = transform.TransformPoint(boxCenterLocalPosition);
        newBox.transform.rotation = transform.rotation;

        _currentRowMaxDepth = Mathf.Max(_currentRowMaxDepth, localBoxSize.z);
        _currentLayerMaxHeight = Mathf.Max(_currentLayerMaxHeight, localBoxSize.y);
        _nextPosition.x += localBoxSize.x + spacing.x;

        _instantiatedBoxes.Add(newBox);
    }

    [ContextMenu("Clear Trailer Visuals")]
    public void ClearVisuals()
    {
        foreach (var box in _instantiatedBoxes)
        {
            if (box != null)
            {
                // Use Destroy in play mode, DestroyImmediate in editor.
                if (Application.isPlaying) Destroy(box);
                else DestroyImmediate(box);
            }
        }
        _instantiatedBoxes.Clear();
        _isInitialized = false;
        _isFull = false;
        _currentLayerMaxHeight = 0f;
        _currentRowMaxDepth = 0f;
        Debug.Log("Trailer visuals cleared.");
    }

    [ContextMenu("Reset Collider Height")]
    public void ResetColliderHeight()
    {
        additionalHeight = 0f;
        UpdateColliderHeight();
    }
}