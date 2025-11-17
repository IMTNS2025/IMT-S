using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/DecontaminationToolSO")]
public class DecontaminationToolSO : ItemSO
{
    public ToolTypes toolType;

    [Header("Scaling")]
    [Tooltip("Scaling factor while the item is in the container (not dragged, not on the workplate).")]
    public float scaleContainer = 0.25f;
    [Tooltip("Scaling factor while the item is being dragged.")]
    public float scaleDragged = 0.2f;
    [Tooltip("Scaling factor when the item is placed on the workplate.")]
    public float scaleWorkplate = 0.4f;
}

public enum ToolTypes
{
    Null,
    Wipes,
    Bag,
    Alcohol,
    Lamp,
};
