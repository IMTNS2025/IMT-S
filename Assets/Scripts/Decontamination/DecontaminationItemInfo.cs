using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DecontaminationItemInfo : MonoBehaviour
{
    //TODOALEX save instance of SO on object instead of this?
    [HideInInspector] public List<ContaminationSpot> contaminationSpots; //[HideInInspector] 
    [HideInInspector] public int maxBagLevels;
    [HideInInspector] public int currentBagLevels = 0;
    [HideInInspector] public Vector2 originalSize;
    [HideInInspector] public float scaleContainer = 1f;
    [HideInInspector] public float scaleDragged = 0.75f;
    [HideInInspector] public float scaleWorkplate;
    [HideInInspector] public float firstBagSizeMulitplier;
    [HideInInspector] public float otherBagsSizeMulitplier;
}