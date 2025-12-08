using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private Button interactionButton;
    private bool isInteracting = false;
    private UnityAction cachedInteraction;
    private IInteractable currentInteractable;
    [SerializeField] private GameObject currentInteractableObject;

    private void Update()
    {
        if (interactionButton == null)
        {
            Debug.LogError("Interaction Button is not assigned in the InteractionController.");
            return;
        }

        interactionButton.interactable = isInteracting;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable i))
        {
            currentInteractable = i;
            currentInteractableObject = collision.gameObject;

            cachedInteraction = () =>
            {
                if (currentInteractable != null && currentInteractableObject != null)
                {
                    currentInteractable.Interact();
                }
                else
                {
                    ClearCurrentInteraction();
                }
            };

            interactionButton.onClick.AddListener(cachedInteraction);
            isInteracting = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out IInteractable i) && i == currentInteractable)
        {
            ClearCurrentInteraction();
        }
    }

    private void ClearCurrentInteraction()
    {
        if (interactionButton != null && cachedInteraction != null)
        {
            interactionButton.onClick.RemoveListener(cachedInteraction);
        }

        cachedInteraction = null;
        currentInteractable = null;
        currentInteractableObject = null;
        isInteracting = false;
    }

    private void OnDestroy()
    {
        ClearCurrentInteraction();
    }
}