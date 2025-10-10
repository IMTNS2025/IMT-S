using UnityEngine;
using UnityEngine.UI;

public class DragDropImageChanger : MonoBehaviour
{
    [SerializeField] private Sprite originalSprite;
    [SerializeField] private Sprite dropSprite;

    private void OnEnable()
    {
        var color = this.GetComponent<RawImage>().color;
        color = new Color(color.r, color.g, color.b, 0f);

        EventManager.OnItemDragStart.AddListener((item) =>
        {
            this.GetComponent<RawImage>().texture = originalSprite.texture;
            this.GetComponent<RawImage>().color = new Color(color.r, color.g, color.b, 1f);
        });

        EventManager.OnItemDragEnd.AddListener((item) =>
        {
            this.GetComponent<RawImage>().texture = dropSprite.texture;
        });
    }
    private void OnDisable()
    {
        EventManager.OnItemDragStart.RemoveListener((item) =>
        {
            this.GetComponent<RawImage>().texture = originalSprite.texture;
        });
        EventManager.OnItemDragEnd.RemoveListener((item) =>
        {
            this.GetComponent<RawImage>().texture = dropSprite.texture;
        });
    }
}
