using UnityEngine;
using System;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    private static InventoryManager _instance;
    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<InventoryManager>();
                if (_instance == null)
                {
                    GameObject inventoryManagerObject = new GameObject("InventoryManager");
                    _instance = inventoryManagerObject.AddComponent<InventoryManager>();
                }
            }
            return _instance;
        }
    }

    public Dictionary<string, int> inventory = new Dictionary<string, int>();
    public bool showCollectionSphere = true;

    // Events
    public static event Action<string, int> OnResourceCollected;
    public static event Action<string, int> OnResourceUsed;
    public static event Action OnInventoryCleared;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            OnResourceCollected = null;
            OnResourceUsed = null;
            OnInventoryCleared = null;
        }
    }

    public void CollectResource(string resourceType, int amount)
    {
        if (inventory.ContainsKey(resourceType))
        {
            inventory[resourceType] += amount;
        }
        else
        {
            inventory[resourceType] = amount;
        }
        OnResourceCollected?.Invoke(resourceType, amount);
        Debug.Log($"Collected {resourceType}! Total: {inventory[resourceType]}");
    }

    public int GetResourceCount(string resourceType)
    {
        return inventory.ContainsKey(resourceType) ? inventory[resourceType] : 0;
    }

    public bool UseResource(string resourceType, int amount)
    {
        if (GetResourceCount(resourceType) >= amount)
        {
            inventory[resourceType] -= amount;
            if (inventory[resourceType] <= 0)
            {
                inventory.Remove(resourceType);
            }
            OnResourceUsed?.Invoke(resourceType, amount);
            Debug.Log($"Used {amount} {resourceType}! Remaining: {GetResourceCount(resourceType)}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough {resourceType}! Need {amount}, have {GetResourceCount(resourceType)}");
            return false;
        }
    }

    [ContextMenu("Clear Inventory")]
    public void ClearInventory()
    {
        inventory.Clear();
        OnInventoryCleared?.Invoke();
        Debug.Log("Inventory cleared!");
    }

    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>(inventory);
    }

    void OnDrawGizmosSelected()
    {
        if (showCollectionSphere)
        {
            float collectionRadius = 3f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
        }
    }
}