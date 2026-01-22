using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float speed = 1.0f;
    [SerializeField] private float deadzone = 0.1f;

    private Rigidbody2D rb;
    private Animator animator;

    private static readonly int HashMoveX = Animator.StringToHash("MoveX");
    private static readonly int HashMoveY = Animator.StringToHash("MoveY");
    private static readonly int HashSpeed = Animator.StringToHash("Speed");

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable() => EventManager.OnMovementDragStarted.AddListener(PlayerMove);

    private void OnDisable() => EventManager.OnMovementDragStarted.RemoveListener(PlayerMove);

    private void PlayerMove(Vector2 dir)
    {
        rb.linearVelocity = dir * speed;

        float mag = dir.magnitude;
        if (mag > deadzone)
        {
            Vector2 n = dir / mag;
            animator.SetFloat(HashMoveX, n.x);
            animator.SetFloat(HashMoveY, n.y);
            animator.SetFloat(HashSpeed, mag);
        }
        else
        {
            animator.SetFloat(HashSpeed, 0f);
        }

    }
}
