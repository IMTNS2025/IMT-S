using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/DecontaminationItemSO")]
public class DecontaminationItemSO : ItemSO
{
    public ToolTypes[] acceptedTypes;
    public Color dirty;
    public Color clean;
}
