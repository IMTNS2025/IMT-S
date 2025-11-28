using UnityEngine;
using UnityEngine.UI;

public class DragDropImageChanger : MonoBehaviour
{
    [SerializeField] private Sprite originalSprite;
    [SerializeField] private Sprite dropSprite;

    [SerializeField] private DragAndDrop myDrag;

    private bool alreadyPlaced;
    private RawImage image;
    private bool isMyDragActive;

    private void ErrorHandler<T>(T r, string context)
    {
        if (r == null)
        {
            Debug.LogError($"[DragDropImageChanger] Null reference detected in '{context}' (Type: {typeof(T)})");
            return;
        }
    }


    private void OnEnable()
    {
        References();

        var c = image.color;
        if (!alreadyPlaced)
        {
            image.color = new Color(c.r, c.g, c.b, 0f);
        }
        else
        {
            image.color = new Color(c.r, c.g, c.b, 1f);

        }

        EventManager.OnItemDragStart.AddListener((item) =>
        {
            if (item != myDrag) return;
            isMyDragActive = true;

            if (originalSprite != null)
            {
                image.texture = originalSprite.texture;
                image.rectTransform.sizeDelta = new Vector2(originalSprite.rect.width / 2f, originalSprite.rect.height / 2f);
            }

            var col = image.color;
            image.color = new Color(col.r, col.g, col.b, 1f);
        });

        EventManager.OnItemDragEnd.AddListener((item) =>
        {
            if (item != myDrag) return;
            isMyDragActive = false;
        });

        EventManager.OnDragSuccessed.AddListener(() =>
        {
            if (!isMyDragActive) return;
            if (dropSprite != null)
                image.texture = dropSprite.texture;
            alreadyPlaced = true;
        });

        EventManager.OnDragFailed.AddListener(() =>
        {
            if (!isMyDragActive) return;
            var col = image.color;
            image.color = new Color(col.r, col.g, col.b, 0f);

            alreadyPlaced = false;
        });
    }

    private void References()
    {
        if (myDrag == null) myDrag = GetComponentInParent<DragAndDrop>();
        ErrorHandler(myDrag, nameof(myDrag));

        image = GetComponent<RawImage>();
        ErrorHandler(image, nameof(image));
    }


    private void OnDisable()
    {
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, 1f);

        // Follow the same pattern you use elsewhere
        EventManager.OnItemDragStart.RemoveAllListeners();
        EventManager.OnItemDragEnd.RemoveAllListeners();
        EventManager.OnDragSuccessed.RemoveAllListeners();
        EventManager.OnDragFailed.RemoveAllListeners();
    }
}
