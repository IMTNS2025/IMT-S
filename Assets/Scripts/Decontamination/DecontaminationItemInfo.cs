using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecontaminationItemInfo : MonoBehaviour
{
    public List<ContaminationSpot> contaminationSpots; //[HideInInspector] 
    public int maxBagLevels;
    public int currentBagLevels = 0;
    public Vector2 originalSize;
    public float scaleContainer = 1f;
    public float scaleDragged = 0.75f;
    public float scaleWorkplate;
    public float firstBagSizeMulitplier;
    public float otherBagsSizeMulitplier;
    public string itemName;
}