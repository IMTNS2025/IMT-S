using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/DecontaminationItemSO")]
public class DecontaminationItemSO : ItemSO
{
    public int maxBagLevels = 2;

    [Header("Scaling")]
    [Tooltip("Scaling factor while the item is in the container (not dragged, not on the workplate).")]
    public float scaleContainer = 0.25f;
    [Tooltip("Scaling factor while the item is being dragged.")]
    public float scaleDragged = 0.2f;
    [Tooltip("Scaling factor when the item is placed on the workplate.")]
    public float scaleWorkplate = 0.4f;

    public float firstBagSizeMulitplier = 0.75f;
    public float otherBagsSizeMulitplier = 0.25f;
}
