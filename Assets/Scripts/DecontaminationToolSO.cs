using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/DecontaminationToolSO")]
public class DecontaminationToolSO : ItemSO
{
    public ToolTypes toolType;
}

public enum ToolTypes
{
    Null,
    Wipes,
    Bag,
    Alcohol,
};
