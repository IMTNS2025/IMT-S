using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PocketsSysten : MonoBehaviour
{
    private float pocketSizeX, pocketSizeY;

    [SerializeField] private List<GameObject> itemInPocket = new List<GameObject>();
    private Dictionary<GameObject, GameObject> instantiatedToOriginal = new Dictionary<GameObject, GameObject>();
    
    [Header("Scale Settings")]
    [SerializeField] private float scalePocket = 1f;
    [SerializeField] private float scaleDragged = 0.75f;
    [SerializeField] private float scaleLocker = 0.5f;
    
    // Track original sizes for scaling
    private Dictionary<GameObject, Vector2> originalSizes = new Dictionary<GameObject, Vector2>();

    public List<GameObject> getItemInPocket()
    {
        return itemInPocket;
    }

    private void OnEnable()
    {
        EventManager.OnItemReturnedToPocket.AddListener(OnItemReturnedToPocket);
        EventManager.OnItemRemovedFromPocket.AddListener(OnItemRemovedFromPocket);
        EventManager.OnItemDragStart.AddListener(OnItemDragStart);
        EventManager.OnDragSuccessed.AddListener(OnDragSucceeded);
        EventManager.OnDragFailed.AddListener(OnDragFailed);
    }
        
    private void OnDisable()
    {
        EventManager.OnItemReturnedToPocket.RemoveListener(OnItemReturnedToPocket);
        EventManager.OnItemRemovedFromPocket.RemoveListener(OnItemRemovedFromPocket);
        EventManager.OnItemDragStart.RemoveListener(OnItemDragStart);
        EventManager.OnDragSuccessed.RemoveListener(OnDragSucceeded);
        EventManager.OnDragFailed.RemoveListener(OnDragFailed);
    }
    
    private void OnItemDragStart(DragAndDrop item)
    {
        if (item == null || item.gameObject == null) return;
        if (!originalSizes.TryGetValue(item.gameObject, out Vector2 originalSize)) return;
        
        RectTransform rect = item.gameObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = originalSize * scaleDragged;
        }
    }
    
    private void OnDragSucceeded(GameObject item)
    {
        if (item == null) return;
        if (!originalSizes.TryGetValue(item, out Vector2 originalSize)) return;
        
        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = originalSize * scaleLocker;
        }
    }
    
    private void OnDragFailed(GameObject item)
    {
        if (item == null) return;
        if (!originalSizes.TryGetValue(item, out Vector2 originalSize)) return;
        
        RectTransform rect = item.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = originalSize * scalePocket;
        }
    }

    private void OnItemReturnedToPocket(DragAndDrop item)
    {
        if (!itemInPocket.Contains(item.gameObject))
        {
            itemInPocket.Add(item.gameObject);
        }

        // Re-add the original item to InventoryManager so it triggers OnInventoryItemAdded
        if (InventoryManager.Instance == null) return;

        // Use the dictionary to find the original inventory item
        if (instantiatedToOriginal.TryGetValue(item.gameObject, out GameObject originalItem))
        {
            InventoryManager.Instance.AddItem(originalItem);
        }
    }

    private void OnItemRemovedFromPocket(DragAndDrop item)
    {
        if (item == null || item.gameObject == null) return;

        itemInPocket.Remove(item.gameObject);

        // Remove the original item from InventoryManager
        if (InventoryManager.Instance == null) return;

        // Primary method: use ScriptableObject reference which is reliable across instantiation
        var itemInfo = item.gameObject.GetComponent<DecontaminationItemInfo>();
        if (itemInfo != null && itemInfo.decontaminationItemSO != null)
        {
            InventoryManager.Instance.RemoveItemBySO(itemInfo.decontaminationItemSO);
        }
        else
        {
            // Fallback: try dictionary reference
            if (instantiatedToOriginal.TryGetValue(item.gameObject, out GameObject originalItem))
            {
                InventoryManager.Instance.RemoveItem(originalItem);
            }
            else
            {
                // Last resort: try by name
                InventoryManager.Instance.RemoveItemByName(item.gameObject.name);
            }
        }

        // Keep the dictionary mapping so we can restore the item if it's returned to pocket
    }

    private void Start()
    {
        if (InventoryManager.Instance == null)
            return;

        var inventoryItems = InventoryManager.Instance.GetInventoryItems();
        if (inventoryItems == null || inventoryItems.Count == 0)
            return;

        pocketSizeX = GetComponent<RectTransform>().rect.width;
        pocketSizeY = GetComponent<RectTransform>().rect.height;

        Bounds pocketBounds = new(transform.position, new Vector3(pocketSizeX, pocketSizeY, 0));

        for (int i = 0; i < inventoryItems.Count; i++)
        {
            var sourceRect = inventoryItems[i].GetComponent<RectTransform>();
            if (sourceRect == null)
                continue;

            // Get the original size from the source
            Vector2 originalSize = sourceRect.sizeDelta;
            
            // Use scaled size for positioning
            Vector2 scaledSize = originalSize * scalePocket;

            Vector2 randomPosition = new Vector2(
                Random.Range(pocketBounds.min.x + scaledSize.x / 2, pocketBounds.max.x - scaledSize.x / 2),
                Random.Range(pocketBounds.min.y + scaledSize.y / 2, pocketBounds.max.y - scaledSize.y / 2)
            );

            // Instantiate the item directly
            var go = Instantiate(inventoryItems[i], transform);
            go.name = inventoryItems[i].name;
            go.transform.position = randomPosition;

            // Add DragAndDrop if not present
            var dragAndDrop = go.GetComponent<DragAndDrop>();
            if (dragAndDrop == null)
            {
                dragAndDrop = go.AddComponent<DragAndDrop>();
            }
            dragAndDrop.ResizeWithTarget = true;
            dragAndDrop.MakeInvisible = false;

            // Ensure the graphic has raycast target enabled
            var graphic = go.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }

            // Store original size and apply pocket scale
            var goRect = go.GetComponent<RectTransform>();
            if (goRect != null)
            {
                originalSizes[go] = originalSize;
                goRect.sizeDelta = scaledSize;
                
                if (go.TryGetComponent<RawImage>(out RawImage img))
                {
                    img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);
                }
            }

            itemInPocket.Add(go);
            instantiatedToOriginal[go] = inventoryItems[i];

            dragAndDrop.Initialize();
        }
    }
}
