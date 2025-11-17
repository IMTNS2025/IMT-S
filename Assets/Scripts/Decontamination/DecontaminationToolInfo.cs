using UnityEngine;

public class DecontaminationToolInfo : MonoBehaviour
{
    [HideInInspector] public ToolTypes toolType; //[HideInInspector] 
    [HideInInspector] public Sprite imageDrag;
    [HideInInspector] public Sprite imageOriginal;
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public float scaleContainer = 1f;
    [HideInInspector] public float scaleDragged = 0.75f;
    [HideInInspector] public float scaleWorkplate = 1.25f;
}