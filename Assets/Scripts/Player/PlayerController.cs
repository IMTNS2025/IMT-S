using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 1.0f;
    private Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        EventManager.OnMovementDragStarted.AddListener(PlayerMove);
    }

    private void OnDisable()
    {
        EventManager.OnMovementDragStarted.AddListener(PlayerMove);
    }

    private void PlayerMove(Vector2 dir)
    {
        rb.linearVelocity = dir * speed;
        Debug.Log(dir);
    }
}
