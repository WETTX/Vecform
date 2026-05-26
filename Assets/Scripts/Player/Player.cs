using System.Collections;
using NUnit.Framework;
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
    [SerializeField] private float coyoteTime = 0.5f;

    /// <summary>
    /// показания с A, D
    /// </summary>
    private Vector2 moveVector;
    /// <summary>
    /// для <see cref="GroundedHandler"/>
    /// </summary>
    private float checkGroundedDistance = 0.01f;
    /// <summary>
    /// стоит ли на земле
    /// </summary>
    private bool isGrounded;
    /// <summary>
    /// как <see cref="isGrounded"/> но с учётом койот-тайма
    /// </summary>
    private bool isCanJump;
    /// <summary>
    /// костыль, флаг, чтобы не было дабл-прыжка
    /// </summary>
    private bool isJustJump;
    private float coyoteTimeCounter;

    private Bounds colliderBounds;
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
        GroundedHandler();
    }

    private void FixedUpdate()
    {
        MoveHandler();
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
        // передвижение
        inp.Player.Disable();

        // прыжок
        inp.Player.Jump.started -= OnJump;
        inp.Player.Jump.canceled -= OnJump;

    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && isCanJump)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            coyoteTimeCounter = 0f;
            isJustJump = true;
        }
        else if (context.canceled && rb.linearVelocityY > 0)
        {
            rb.linearVelocityY *= canceledJumpSpeedMultiplier;
            isJustJump = false;
        }
    }

    /// <summary>
    /// обновляет <see cref="isGrounded"/> и <see cref="isCanJump"/>
    /// </summary>
    private void GroundedHandler()
    {
        colliderBounds = col.bounds;
        rayOrigin = new Vector2(colliderBounds.min.x, colliderBounds.min.y - 0.01f);
        RaycastHit2D hit1 = Physics2D.Raycast(rayOrigin, Vector2.down, checkGroundedDistance);

        colliderBounds = col.bounds;
        rayOrigin = new Vector2(colliderBounds.max.x, colliderBounds.min.y - 0.01f);
        RaycastHit2D hit2 = Physics2D.Raycast(rayOrigin, Vector2.down, checkGroundedDistance);

        if (hit1.collider != null || hit2.collider != null)
        {
            isGrounded = true;

            if (!isJustJump) 
            {
                coyoteTimeCounter = coyoteTime;
            }
        }
        else
        {
            isGrounded = false;
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (isGrounded || coyoteTimeCounter > 0f)
        {
            isCanJump = true;
        }
        else
        {
            isCanJump = false;
        }
    }

    private IEnumerator CoyoteRoutine()
    {
        yield return new WaitForSeconds(coyoteTime);

        isGrounded = false;
    }

    private void MoveHandler()
    {
        moveVector = inp.Player.Move.ReadValue<Vector2>();
        rb.linearVelocityX = moveVector.x * moveSpeed;
    }
}
