using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public Sprite originalImage;
    public Sprite imageDrag;
    public new string name;
    public int orderIndex;
    [TextArea(2,5)] public string description;
}
