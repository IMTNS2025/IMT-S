using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public Texture imageOnCharacter;
    public Sprite imageOnDragging;
    public new string name;
    public int orderIndex;
    [TextArea(2,5)] public string description;
}
