using UnityEngine;

public class GoesToLockerSpot : MonoBehaviour
{
    public GoesToLockerSpotType goesToLockerSpotType;
}

public enum GoesToLockerSpotType
{
    Null,
    Trash,
    Inventory,
    Locker,
};