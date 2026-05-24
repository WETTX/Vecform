using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpForce = 10f;
    /// <summary>
    /// какая часть скорости останется при отпускании кнопки прыжка
    /// </summary>
    [SerializeField] private float canceledJumpSpeedMultiplier = 0.7f; 

    /// <summary>
    /// показания с A, D
    /// </summary>
    private float moveX;
    /// <summary>
    /// для IsGrounded
    /// </summary>
    private float checkGroundedDistance = 0.01f;
    private Bounds colBounds;
    private Vector2 rayOrigin;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private InputSystem inp;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        inp = new InputSystem();
    }

    private void Update()
    {
        //передвижение
        moveX = inp.Player.Move.ReadValue<Vector2>().x;
    }

    private void FixedUpdate()
    {
        //передвижение
        rb.linearVelocityX = moveX * moveSpeed;
    }

    private void OnEnable()
    {
        //передвижение
        inp.Player.Enable();

        //прыжок
        inp.Player.Jump.started += OnJump;
        inp.Player.Jump.canceled += OnJump;
    }

    private void OnDisable()
    {
        //передвижение
        inp.Player.Disable();

        //прыжок
        inp.Player.Jump.started -= OnJump;
        inp.Player.Jump.canceled -= OnJump;

    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && IsGrounded())
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        else if (context.canceled && rb.linearVelocityY > 0)
        {
            rb.linearVelocityY *= canceledJumpSpeedMultiplier;
        }
    }

    private bool IsGrounded()
    {
        colBounds = col.bounds;
        rayOrigin = new Vector2(colBounds.center.x, colBounds.min.y - 0.01f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, checkGroundedDistance);

        return hit.collider != null;
    }
}
