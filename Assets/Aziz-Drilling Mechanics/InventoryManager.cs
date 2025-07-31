using System;
using System.Collections.Generic;
using UnityEngine;

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
    public bool showCollectionSphere=true;

    public static event Action<string, int> OnResourceCollected;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            OnResourceCollected = null;
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

    // Method to get current inventory count
    public int GetResourceCount(string resourceType)
    {
        return inventory.ContainsKey(resourceType) ? inventory[resourceType] : 0;
    }

    // Method to use/consume resources
    public bool UseResource(string resourceType, int amount)
    {
        if (GetResourceCount(resourceType) >= amount)
        {
            inventory[resourceType] -= amount;
            if (inventory[resourceType] <= 0)
            {
                inventory.Remove(resourceType);
            }
            Debug.Log($"Used {amount} {resourceType}! Remaining: {GetResourceCount(resourceType)}");
            return true;
        }
        else
        {
            Debug.Log($"Not enough {resourceType}! Need {amount}, have {GetResourceCount(resourceType)}");
            return false;
        }
    }

    // Method to display inventory in console
    [ContextMenu("Show Inventory")]
    public void ShowInventory()
    {
        Debug.Log("=== INVENTORY ===");
        if (inventory.Count == 0)
        {
            Debug.Log("Inventory is empty");
            return;
        }

        foreach (var item in inventory)
        {
            Debug.Log($"{item.Key}: {item.Value}");
        }
    }

    // Method to clear all inventory
    [ContextMenu("Clear Inventory")]
    public void ClearInventory()
    {
        inventory.Clear();
        Debug.Log("Inventory cleared!");
    }

    // Get all resources as a copy of the dictionary
    public Dictionary<string, int> GetAllResources()
    {
        return new Dictionary<string, int>(inventory);
    }

    // Visualize collection radius in scene view
    void OnDrawGizmosSelected()
    {
        if (showCollectionSphere)
        {
            float collectionRadius = 3f; // This should match the collection radius used in destructible objects
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collectionRadius);
        }
    }
}