using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class EventManager
{
    //INFO: 

    //This is the Event Manager class. Here we will manage all the events in the game.
    //It's static manager so we can access it from anywhere in the project.
    //We dont need to create new events everywhere, we can just call the event from here.


    //With this, we dont care where the event is called from, we just care about what to do when the event is called
    //and we avoid reference issues and coupling.

    public static UnityEvent<List<Vector3>> OnPathCalculated = new();

    public static UnityEvent<DragAndDrop> OnItemReturnedToPocket = new();
    public static UnityEvent<DragAndDrop> OnItemRemovedFromPocket = new();

    public static UnityEvent<DragAndDrop> OnItemDragStart = new();
    public static UnityEvent<DragAndDrop> OnItemDragEnd = new();
}
