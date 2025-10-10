using UnityEngine;
using UnityEngine.UI;

public class DragDropImageChanger : MonoBehaviour
{
    [SerializeField] private Sprite originalSprite;
    [SerializeField] private Sprite dropSprite;

    private void OnEnable()
    {
        EventManager.OnItemDragStart.AddListener((item) =>
        {
            DragDropImageChanger info = item.gameObject.GetComponentInChildren<DragDropImageChanger>();
            info.GetComponent<RawImage>().texture = originalSprite.texture;
        });

        EventManager.OnItemDragEnd.AddListener((item) =>
        {
            DragDropImageChanger info = item.gameObject.GetComponentInChildren<DragDropImageChanger>();
            info.GetComponent<RawImage>().texture = dropSprite.texture;

        });
    }
    private void OnDisable()
    {
        EventManager.OnItemDragStart.RemoveListener((item) =>
        {
            DragDropImageChanger info = item.gameObject.GetComponentInChildren<DragDropImageChanger>();
            info.GetComponent<RawImage>().texture = originalSprite.texture;
        });
        EventManager.OnItemDragEnd.RemoveListener((item) =>
        {
            DragDropImageChanger info = item.gameObject.GetComponentInChildren<DragDropImageChanger>();
            info.GetComponent<RawImage>().texture = dropSprite.texture;
        });
    }
}
