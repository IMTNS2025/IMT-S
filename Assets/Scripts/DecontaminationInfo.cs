using UnityEngine;

public class DecontaminationInfo : MonoBehaviour
{
    public ToolTypes[] acceptedTypes;
    public ToolTypes toolType;
    public ContaminationSpot[] contaminationSpots;
}

[System.Serializable]
public struct ContaminationSpot
{
    public bool visible;
    public Vector2 pos;
    public Texture2D texture;
    public float intensity;

    public ContaminationSpot(Vector2 pos, Texture2D texture, float intensity, bool visible)
    {
        this.pos = pos;
        this.texture = texture;
        this.intensity = intensity;
        this.visible = visible;
    }
}