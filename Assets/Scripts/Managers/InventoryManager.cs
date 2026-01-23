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
        if (item != null && !inventoryItems.Contains(item))
        {
            inventoryItems.Add(item);
            EventManager.OnInventoryItemAdded.Invoke(item);
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
        GameObject itemToRemove = null;
        foreach (var item in inventoryItems)
        {
            if (item != null && item.name == itemName)
            {
                itemToRemove = item;
                break;
            }
        }

        if (itemToRemove != null)
        {
            inventoryItems.Remove(itemToRemove);
            EventManager.OnInventoryItemRemoved.Invoke(itemToRemove);
        }
    }

    public void RemoveItemBySO(DecontaminationItemSO itemSO)
    {
        if (itemSO == null) return;

        GameObject itemToRemove = null;
        foreach (var item in inventoryItems)
        {
            if (item == null) continue;
            var info = item.GetComponent<DecontaminationItemInfo>();
            if (info != null && info.decontaminationItemSO == itemSO)
            {
                itemToRemove = item;
                break;
            }
        }

        if (itemToRemove != null)
        {
            inventoryItems.Remove(itemToRemove);
            EventManager.OnInventoryItemRemoved.Invoke(itemToRemove);
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
