using UnityEngine;
using System; // Action için gerekli
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // Singleton deseni için Instance
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
                    GameObject inventoryObject = new GameObject("InventoryManager");
                    _instance = inventoryObject.AddComponent<InventoryManager>();
                }
            }
            return _instance;
        }
    }

    // Envanteri tutan sözlük
    public Dictionary<string, int> inventory = new Dictionary<string, int>();

    // Kaynak toplandýðýnda dýþarýdan dinlenebilecek olay (event)
    public static event Action<string, int> OnResourceCollected;
    public static event Action<string, int> OnResourceUsed;
    public static event Action OnInventoryCleared;

    void Awake()
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

        // Kaynak toplandýðýnda OnResourceCollected event'ini çaðýr
        OnResourceCollected?.Invoke(resourceType, amount);

        Debug.Log($"Collected {resourceType}! Total: {inventory[resourceType]}");
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

    public int GetResourceCount(string resourceType)
    {
        return inventory.ContainsKey(resourceType) ? inventory[resourceType] : 0;
    }

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

    // Konsolda envanteri göstermek için metot
    [ContextMenu("Show Inventory")]
    public void ShowInventory()
    {
        Debug.Log("=== INVENTORY ===");
        if (inventory.Count == 0)
        {
            Debug.Log("Inventory is empty!");
            return;
        }
        foreach (var item in inventory)
        {
            Debug.Log($"{item.Key}: {item.Value}");
        }
    }
}
