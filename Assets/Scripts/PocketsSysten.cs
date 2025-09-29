using UnityEngine;
using UnityEngine.UI;

public class PocketsSysten : MonoBehaviour
{
    [SerializeField] Image[] itemsInPocket;

    private float pocketSizeX, pocketSizeY;

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

            Instantiate(itemsInPocket[i], randomPosition, Quaternion.identity, transform);
        }
    }
}
