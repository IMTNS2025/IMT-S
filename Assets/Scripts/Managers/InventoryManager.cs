using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [SerializeField] private List<GameObject> inventoryItems = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public List<GameObject> GetInventoryItems()
    {
        return inventoryItems;
    }

    public void AddItem(GameObject item)
    {
        Debug.Log($"[InventoryManager] AddItem called: {item?.name}");
        
        if (item != null && !inventoryItems.Contains(item))
        {
            inventoryItems.Add(item);
            Debug.Log($"[InventoryManager] Item {item.name} added. Invoking OnInventoryItemAdded. Total items: {inventoryItems.Count}");
            EventManager.OnInventoryItemAdded.Invoke(item);
        }
        else
        {
            Debug.Log($"[InventoryManager] Item {item?.name} NOT added (null or already exists). Contains: {inventoryItems.Contains(item)}");
        }
    }

    public void RemoveItem(GameObject item)
    {
        if (item != null && inventoryItems.Contains(item))
        {
            inventoryItems.Remove(item);
            EventManager.OnInventoryItemRemoved.Invoke(item);
        }
    }

    public void RemoveItemByName(string itemName)
    {
        Debug.Log($"[InventoryManager] RemoveItemByName called with name: {itemName}");
        Debug.Log($"[InventoryManager] Searching through {inventoryItems.Count} items...");
        
        GameObject itemToRemove = null;
        foreach (var item in inventoryItems)
        {
            if (item != null)
            {
                Debug.Log($"[InventoryManager] Checking item: '{item.name}' vs '{itemName}', match: {item.name == itemName}");
            }
            if (item != null && item.name == itemName)
            {
                itemToRemove = item;
                break;
            }
        }

        if (itemToRemove != null)
        {
            Debug.Log($"[InventoryManager] RemoveItemByName: Removing item: {itemToRemove.name}");
            inventoryItems.Remove(itemToRemove);
            EventManager.OnInventoryItemRemoved.Invoke(itemToRemove);
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] RemoveItemByName: No item found with name: {itemName}");
        }
    }

    public void RemoveItemBySO(DecontaminationItemSO itemSO)
    {
        Debug.Log($"[InventoryManager] RemoveItemBySO called with SO: {itemSO?.name}");
        
        if (itemSO == null)
        {
            Debug.LogWarning("[InventoryManager] RemoveItemBySO: itemSO is NULL!");
            return;
        }

        Debug.Log($"[InventoryManager] Searching through {inventoryItems.Count} items...");
        
        GameObject itemToRemove = null;
        foreach (var item in inventoryItems)
        {
            if (item == null)
            {
                Debug.Log("[InventoryManager] Found NULL item in inventory");
                continue;
            }
            var info = item.GetComponent<DecontaminationItemInfo>();
            Debug.Log($"[InventoryManager] Checking item: {item.name}, has info: {info != null}, SO: {info?.decontaminationItemSO?.name}");
            
            if (info != null && info.decontaminationItemSO == itemSO)
            {
                Debug.Log($"[InventoryManager] MATCH FOUND: {item.name}");
                itemToRemove = item;
                break;
            }
        }

        if (itemToRemove != null)
        {
            Debug.Log($"[InventoryManager] Removing item: {itemToRemove.name}");
            inventoryItems.Remove(itemToRemove);
            EventManager.OnInventoryItemRemoved.Invoke(itemToRemove);
        }
        else
        {
            Debug.LogWarning($"[InventoryManager] No matching item found for SO: {itemSO.name}");
        }
    }

    public bool HasItem(GameObject item)
    {
        return inventoryItems.Contains(item);
    }

    public void ClearInventory()
    {
        inventoryItems.Clear();
        EventManager.OnInventoryCleared.Invoke();
    }

    public int GetItemCount()
    {
        return inventoryItems.Count;
    }
}
