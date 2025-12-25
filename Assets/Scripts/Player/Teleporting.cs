using UnityEngine;

public class Teleporting : MonoBehaviour, IInteractable
{
    [SerializeField] private Transform teleportLocation;
    [SerializeField] private Transform player;
    [SerializeField] bool teleported = false;
    Vector2 originalPosition;

    public void Interact()
    {
        if (!teleported)
        {
            originalPosition = player.transform.position;
            player.transform.position = teleportLocation.position;
        }
        else
        {
            player.transform.position = originalPosition;
        }
        teleported = !teleported;
    }
}
