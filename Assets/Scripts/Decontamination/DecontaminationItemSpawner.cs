using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecontaminationItemSpawner : MonoBehaviour
{
    [SerializeField] private DecontaminationFeedbackManager decontaminationPerformanceManager;
    [SerializeField] private GameObject[] itemContainers;
    [SerializeField] private DecontaminationToolSO[] tools;
    [SerializeField] private GameObject[] toolContainers;

    // Track mapping between inventory prefabs and spawned instances
    private Dictionary<GameObject, SpawnedItemData> spawnedItems = new Dictionary<GameObject, SpawnedItemData>();

    private class SpawnedItemData
    {
        public GameObject spawnedInstance;
        public GameObject container;
        public int containerIndex;
    }

    void Awake()
    {
        SetUpItems();
        SetUpTools();
    }

    private void OnEnable()
    {
        Debug.Log("[DecontaminationItemSpawner] OnEnable - Subscribing to events");
        EventManager.OnInventoryItemAdded.AddListener(OnInventoryItemAdded);
        EventManager.OnInventoryItemRemoved.AddListener(OnInventoryItemRemoved);
        EventManager.OnInventoryCleared.AddListener(OnInventoryCleared);
        EventManager.OnItemRemovedFromPocket.AddListener(OnItemRemovedFromPocket);
        EventManager.OnItemReturnedToPocket.AddListener(OnItemReturnedToPocket);
        
        // Sync with current inventory state - spawn any items that should be spawned but aren't
        SyncWithInventory();
    }
    
    private void SyncWithInventory()
    {
        if (InventoryManager.Instance == null) return;
        
        List<GameObject> inventoryItems = InventoryManager.Instance.GetInventoryItems();
        
        foreach (GameObject inventoryItem in inventoryItems)
        {
            if (inventoryItem == null) continue;
            
            DecontaminationItemInfo itemInfo = inventoryItem.GetComponent<DecontaminationItemInfo>();
            if (itemInfo == null) continue;
            
            // Check if this item is already spawned
            if (spawnedItems.ContainsKey(inventoryItem)) continue;
            
            // Check if an item with the same SO is already spawned
            bool alreadySpawned = false;
            foreach (var kvp in spawnedItems)
            {
                if (kvp.Key == null) continue;
                DecontaminationItemInfo existingInfo = kvp.Key.GetComponent<DecontaminationItemInfo>();
                if (existingInfo != null && existingInfo.decontaminationItemSO == itemInfo.decontaminationItemSO)
                {
                    alreadySpawned = true;
                    break;
                }
            }
            
            if (alreadySpawned) continue;
            
            // Find an available container and spawn
            int containerIndex = FindAvailableContainer();
            if (containerIndex < 0)
            {
                Debug.LogWarning($"[DecontaminationItemSpawner] SyncWithInventory: No available container for {inventoryItem.name}");
                continue;
            }
            
            Debug.Log($"[DecontaminationItemSpawner] SyncWithInventory: Spawning {inventoryItem.name} in container {containerIndex}");
            SpawnItem(inventoryItem, containerIndex);
        }
    }

    private void OnDisable()
    {
        EventManager.OnInventoryItemAdded.RemoveListener(OnInventoryItemAdded);
        EventManager.OnInventoryItemRemoved.RemoveListener(OnInventoryItemRemoved);
        EventManager.OnInventoryCleared.RemoveListener(OnInventoryCleared);
        EventManager.OnItemRemovedFromPocket.RemoveListener(OnItemRemovedFromPocket);
        EventManager.OnItemReturnedToPocket.RemoveListener(OnItemReturnedToPocket);
    }

    private void OnItemRemovedFromPocket(DragAndDrop dragAndDrop)
    {
        Debug.Log($"[DecontaminationItemSpawner] OnItemRemovedFromPocket: {dragAndDrop?.gameObject?.name}");
        
        // Find the spawned item by matching the item name
        string itemName = dragAndDrop.gameObject.name;
        
        GameObject inventoryItemToRemove = null;
        foreach (var kvp in spawnedItems)
        {
            if (kvp.Key != null && kvp.Key.name == itemName)
            {
                inventoryItemToRemove = kvp.Key;
                break;
            }
        }

        if (inventoryItemToRemove == null)
        {
            Debug.Log($"[DecontaminationItemSpawner] No spawned item found for {itemName}");
            return;
        }

        SpawnedItemData data = spawnedItems[inventoryItemToRemove];

        // Unregister from feedback manager
        DecontaminationItemInfo itemInfo = data.spawnedInstance.GetComponent<DecontaminationItemInfo>();
        if (itemInfo != null)
        {
            decontaminationPerformanceManager.UnregisterDecontaminationItem(itemInfo);
        }

        // Destroy the spawned instance
        Destroy(data.spawnedInstance);

        // Remove from tracking
        spawnedItems.Remove(inventoryItemToRemove);
        Debug.Log($"[DecontaminationItemSpawner] Removed spawned item: {itemName}");
    }

    private void OnItemReturnedToPocket(DragAndDrop dragAndDrop)
    {
        Debug.Log($"[DecontaminationItemSpawner] OnItemReturnedToPocket: {dragAndDrop?.gameObject?.name}");
        
        // Get the DecontaminationItemInfo from the dragged object
        DecontaminationItemInfo draggedItemInfo = dragAndDrop.gameObject.GetComponent<DecontaminationItemInfo>();
        if (draggedItemInfo == null || draggedItemInfo.decontaminationItemSO == null)
        {
            Debug.Log($"[DecontaminationItemSpawner] No DecontaminationItemInfo/SO on dragged object, skipping");
            return;
        }

        // Check if this item already has a spawned instance (by matching ScriptableObject)
        foreach (var kvp in spawnedItems)
        {
            if (kvp.Key == null) continue;
            
            DecontaminationItemInfo existingInfo = kvp.Key.GetComponent<DecontaminationItemInfo>();
            if (existingInfo != null && existingInfo.decontaminationItemSO == draggedItemInfo.decontaminationItemSO)
            {
                Debug.Log($"[DecontaminationItemSpawner] Already has spawned instance for {draggedItemInfo.decontaminationItemSO.name}, skipping");
                return;
            }
        }

        // Find the inventory item that matches by ScriptableObject
        List<GameObject> inventoryItems = InventoryManager.Instance.GetInventoryItems();
        Debug.Log($"[DecontaminationItemSpawner] Searching {inventoryItems.Count} inventory items for SO: {draggedItemInfo.decontaminationItemSO.name}");
        
        GameObject existingInventoryItem = null;
        
        foreach (GameObject inventoryItem in inventoryItems)
        {
            if (inventoryItem == null) continue;
            
            DecontaminationItemInfo itemInfo = inventoryItem.GetComponent<DecontaminationItemInfo>();
            if (itemInfo != null && itemInfo.decontaminationItemSO == draggedItemInfo.decontaminationItemSO)
            {
                existingInventoryItem = inventoryItem;
                Debug.Log($"[DecontaminationItemSpawner] Found matching inventory item: {inventoryItem.name}");
                break;
            }
        }

        // If the item exists in inventory but not spawned, spawn it now
        if (existingInventoryItem != null && !spawnedItems.ContainsKey(existingInventoryItem))
        {
            int containerIndex = FindAvailableContainer();
            if (containerIndex < 0)
            {
                Debug.LogWarning($"[DecontaminationItemSpawner] No available container for {existingInventoryItem.name}");
                return;
            }

            Debug.Log($"[DecontaminationItemSpawner] Spawning {existingInventoryItem.name} in container {containerIndex}");
            SpawnItem(existingInventoryItem, containerIndex);
        }
        else
        {
            Debug.Log($"[DecontaminationItemSpawner] Cannot spawn: existingInventoryItem={existingInventoryItem?.name}, alreadySpawned={existingInventoryItem != null && spawnedItems.ContainsKey(existingInventoryItem)}");
        }
    }

    private void OnInventoryItemAdded(GameObject inventoryItem)
    {
        Debug.Log($"[DecontaminationItemSpawner] OnInventoryItemAdded: {inventoryItem?.name}");
        
        // Check if item has DecontaminationItemInfo
        DecontaminationItemInfo itemInfo = inventoryItem.GetComponent<DecontaminationItemInfo>();
        if (itemInfo == null)
        {
            Debug.Log($"[DecontaminationItemSpawner] No DecontaminationItemInfo on {inventoryItem?.name}, skipping");
            return;
        }

        // Check if already spawned
        if (spawnedItems.ContainsKey(inventoryItem))
        {
            Debug.Log($"[DecontaminationItemSpawner] {inventoryItem?.name} already in spawnedItems, skipping");
            return;
        }

        // Find an available container
        int containerIndex = FindAvailableContainer();
        if (containerIndex < 0)
        {
            Debug.LogWarning($"[DecontaminationItemSpawner] No available container for {inventoryItem?.name}");
            return;
        }

        Debug.Log($"[DecontaminationItemSpawner] Spawning {inventoryItem?.name} in container {containerIndex}");
        SpawnItem(inventoryItem, containerIndex);
    }

    private void OnInventoryItemRemoved(GameObject inventoryItem)
    {
        if (!spawnedItems.TryGetValue(inventoryItem, out SpawnedItemData data)) return;

        // Unregister from feedback manager
        DecontaminationItemInfo itemInfo = data.spawnedInstance.GetComponent<DecontaminationItemInfo>();
        if (itemInfo != null)
        {
            decontaminationPerformanceManager.UnregisterDecontaminationItem(itemInfo);
        }

        // Destroy the spawned instance
        Destroy(data.spawnedInstance);

        // Remove from tracking
        spawnedItems.Remove(inventoryItem);
    }

    private void OnInventoryCleared()
    {
        // Destroy all spawned instances
        foreach (var kvp in spawnedItems)
        {
            if (kvp.Value.spawnedInstance != null)
            {
                DecontaminationItemInfo itemInfo = kvp.Value.spawnedInstance.GetComponent<DecontaminationItemInfo>();
                if (itemInfo != null)
                {
                    decontaminationPerformanceManager.UnregisterDecontaminationItem(itemInfo);
                }
                Destroy(kvp.Value.spawnedInstance);
            }
        }
        spawnedItems.Clear();
    }

    private int FindAvailableContainer()
    {
        for (int i = 0; i < itemContainers.Length; i++)
        {
            bool isUsed = false;
            foreach (var kvp in spawnedItems)
            {
                if (kvp.Value.containerIndex == i)
                {
                    isUsed = true;
                    break;
                }
            }
            if (!isUsed) return i;
        }
        return -1;
    }

    private void SpawnItem(GameObject inventoryItem, int containerIndex)
    {
        DecontaminationItemInfo sourceInfo = inventoryItem.GetComponent<DecontaminationItemInfo>();
        if (sourceInfo == null || sourceInfo.decontaminationItemSO == null)
        {
            Debug.LogWarning($"DecontaminationItemInfo on {inventoryItem.name} has no DecontaminationItemSO assigned.");
            return;
        }

        // Validate the source has required components before instantiating
        if (inventoryItem.GetComponent<RawImage>() == null)
        {
            Debug.LogWarning($"Inventory item {inventoryItem.name} has no RawImage component, skipping spawn.");
            return;
        }

        if (sourceInfo.contaminationTypeSO == null)
        {
            Debug.LogWarning($"DecontaminationItemInfo on {inventoryItem.name} has no ContaminationTypeSO assigned, skipping spawn.");
            return;
        }

        // Clear any existing children in the container immediately
        for (int j = itemContainers[containerIndex].transform.childCount - 1; j >= 0; j--)
        {
            DestroyImmediate(itemContainers[containerIndex].transform.GetChild(j).gameObject);
        }

        GameObject itemGO = Instantiate(sourceInfo.gameObject, itemContainers[containerIndex].transform);
        DecontaminationItemInfo decontaminationInfo = itemGO.GetComponent<DecontaminationItemInfo>();

        // Track the spawned item
        spawnedItems[inventoryItem] = new SpawnedItemData
        {
            spawnedInstance = itemGO,
            container = itemContainers[containerIndex],
            containerIndex = containerIndex
        };

        // Re-initialize the DragAndDrop to pick up the new child
        DragAndDrop dragAndDrop = itemContainers[containerIndex].GetComponent<DragAndDrop>();
        if (dragAndDrop != null)
        {
            dragAndDrop.Initialize();
        }

        RectTransform containerRect = itemContainers[containerIndex].GetComponent<RectTransform>();
        RectTransform itemRectTransform = itemGO.GetComponent<RectTransform>();
        RawImage image = itemGO.GetComponent<RawImage>();

        // Ensure the image can receive raycasts for drag and drop
        image.raycastTarget = true;

        DecontaminationItemSO itemSO = decontaminationInfo.decontaminationItemSO;

        image.texture = itemSO.originalImage.texture;

        decontaminationInfo.maxBagLevels = itemSO.maxBagLevels;
        decontaminationInfo.scaleContainer = itemSO.scaleContainer;
        decontaminationInfo.scaleDragged = itemSO.scaleDragged;
        decontaminationInfo.scaleWorkplate = itemSO.scaleWorkplate;
        decontaminationInfo.originalSize = new Vector2(image.texture.width, image.texture.height);
        decontaminationInfo.firstBagSizeMulitplier = itemSO.firstBagSizeMulitplier;
        decontaminationInfo.otherBagsSizeMulitplier = itemSO.otherBagsSizeMulitplier;
        decontaminationInfo.itemName = itemSO.name;

        Vector2 scaledSize = decontaminationInfo.originalSize * decontaminationInfo.scaleContainer;
        containerRect.sizeDelta = scaledSize;

        itemRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        itemRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        itemRectTransform.pivot = new Vector2(0.5f, 0.5f);
        itemRectTransform.anchoredPosition = Vector2.zero;
        itemRectTransform.sizeDelta = scaledSize;

        // Initialize/clear the contamination spots list for the new instance
        if (decontaminationInfo.contaminationSpots == null)
        {
            decontaminationInfo.contaminationSpots = new List<ContaminationSpot>();
        }
        else
        {
            decontaminationInfo.contaminationSpots.Clear();
        }

        decontaminationInfo.contaminationTypeSO.spawnContamination(decontaminationInfo);

        decontaminationPerformanceManager.RegisterDecontaminationItem(decontaminationInfo);
    }

    private void SetUpTools()
    {
        
        for (int i = 0; i < toolContainers.Length; i++)
        {
            if (toolContainers[i].transform.childCount <= 0) continue;            
                
            Transform item = toolContainers[i].transform.GetChild(0);
            RawImage image = item.GetComponent<RawImage>();
            DecontaminationToolInfo decontaminationToolInfo = item?.GetComponent<DecontaminationToolInfo>();
            RectTransform containerRect = toolContainers[i].GetComponent<RectTransform>();
            RectTransform itemRectTransform = item.GetComponent<RectTransform>();

            if (item == null || image == null || containerRect == null || itemRectTransform == null || decontaminationToolInfo == null) continue;

            image.texture = tools[i].originalImage.texture;

            decontaminationToolInfo.toolType = tools[i].toolType;
            decontaminationToolInfo.imageDrag = tools[i].imageDrag;
            decontaminationToolInfo.scaleContainer = tools[i].scaleContainer;
            decontaminationToolInfo.scaleDragged = tools[i].scaleDragged;
            decontaminationToolInfo.scaleWorkplate = tools[i].scaleWorkplate;
            decontaminationToolInfo.imageOriginal = tools[i].originalImage;
            decontaminationToolInfo.originalSize = new Vector2(image.texture.width, image.texture.height);

            Vector2 scaledSize = decontaminationToolInfo.originalSize * decontaminationToolInfo.scaleContainer;
            containerRect.sizeDelta = scaledSize;

            itemRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            itemRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            itemRectTransform.pivot = new Vector2(0.5f, 0.5f);
            itemRectTransform.anchoredPosition = Vector2.zero;
            itemRectTransform.sizeDelta = scaledSize;
        }
    }

    private void SetUpItems()
    {
        List<GameObject> inventoryItems = InventoryManager.Instance.GetInventoryItems();
        List<GameObject> decontaminationItems = new List<GameObject>();

        foreach (GameObject inventoryItem in inventoryItems)
        {
            DecontaminationItemInfo itemInfo = inventoryItem.GetComponent<DecontaminationItemInfo>();
            if (itemInfo != null)
            {
                decontaminationItems.Add(inventoryItem);
            }
        }

        Shuffle(itemContainers);

        for (int i = 0; i < decontaminationItems.Count && i < itemContainers.Length; i++)
        {
            SpawnItem(decontaminationItems[i], i);
        }
    }

    private void Shuffle(GameObject[] array)
    {
        if (array == null || array.Length <= 1) return;

        // Fisher–Yates shuffle using UnityEngine.Random
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1); // inclusive lower, exclusive upper; i+1 makes it inclusive of i
            if (i == j) continue;
            GameObject tmp = array[i];
            array[i] = array[j];
            array[j] = tmp;
        }
    }
}
