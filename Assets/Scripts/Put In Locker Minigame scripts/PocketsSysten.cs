using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PocketsSysten : MonoBehaviour
{
    [SerializeField] Image[] itemsInPocket;

    private float pocketSizeX, pocketSizeY;

    [SerializeField] private List<GameObject> itemInPocket;

    public List<GameObject> getItemInPocket()
    {
        return itemInPocket;
    }

    private void OnEnable()
    {
        EventManager.OnItemReturnedToPocket.AddListener((item) =>
        {
            itemInPocket.Add(item.gameObject);
        });

        EventManager.OnItemRemovedFromPocket.AddListener((item) =>
        {
            itemInPocket.Remove(item.gameObject);
        });
    }
    private void OnDisable()
    {
        EventManager.OnItemReturnedToPocket.RemoveAllListeners();
        EventManager.OnItemRemovedFromPocket.RemoveAllListeners();
    }

    private void Start()
    {
        if (itemsInPocket.Length == 0)
            return;

        pocketSizeX = GetComponent<RectTransform>().rect.width;
        pocketSizeY = GetComponent<RectTransform>().rect.height;

        Bounds pocketBounds = new(transform.position, new Vector3(pocketSizeX, pocketSizeY, 0));

        for (int i = 0; i < itemsInPocket.Length; i++)
        {
            Vector2 randomPosition = new Vector2(
                Random.Range(pocketBounds.min.x + itemsInPocket[i].rectTransform.rect.width / 2, pocketBounds.max.x - itemsInPocket[i].rectTransform.rect.width / 2),
                Random.Range(pocketBounds.min.y + itemsInPocket[i].rectTransform.rect.height / 2, pocketBounds.max.y - itemsInPocket[i].rectTransform.rect.height / 2)
            );

            var go = Instantiate(itemsInPocket[i], randomPosition, Quaternion.identity, transform);
            go.name = itemsInPocket[i].name;
            itemInPocket.Add(go.gameObject);
        }
    }
}
