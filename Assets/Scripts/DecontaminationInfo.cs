using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecontaminationInfo : MonoBehaviour
{
    public ToolTypes toolType;
    public List<ContaminationSpot> contaminationSpots;
}

[System.Serializable]
public struct ContaminationSpot
{
    public bool visible;
    public bool needsAlcohol;
    public bool isSoaked;
    public Vector3 pos;
    public RawImage image;
    public float intensity;

    public ContaminationSpot(Vector3 pos, RawImage image, float intensity, bool visible, bool needsAlcohol)
    {
        this.pos = pos;
        this.image = image;
        this.intensity = intensity;
        this.visible = visible;
        this.isSoaked = false;
        this.needsAlcohol = needsAlcohol;
    }
}