using System;
using System.Collections;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    public static PlayerMove Instance { get; private set; }

    [Header("Move")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpScale = 10f;
    [SerializeField] private float canceledJumpSpeedMultiplier = 0.7f; // какая часть скорости останется при отпускании кнопки прыжка
    [SerializeField] private float coyoteTime = 0.5f;
    [Tooltip("NOT 0")]
    [SerializeField] private float jumpBufferTime = 0.15f; // NOT 0

    [Space(5)]
    [Header("Physics")]
    [SerializeField] private float externalForceDecayVelocity = 5f; // скорость угасания внешних импульсов
    [SerializeField] private float jumpForceDecayVelocity = 5f; // скорость угасания импульса прыжка
    [SerializeField] private float fastJumpForceDecayVelocity = 10f; // используется когда началось падение 
                                                                     // чтобы не было эффекта плавного падения
    [SerializeField] private float gravityScale = 1f;

    private bool isJustJump; // костыль, флаг, чтобы не было дабл-прыжка
    private float coyoteTimeCounter;
    private float jumpBufferTimeCounter;
    private Vector2 jumpForce; // отдельно чтобы был красивый контроль прыжка
    private Vector2 externalForce;
    private Vector2 gravityVector = Vector2.down;
    private float gravityForce; // сделано не так как другие силы чтобы было удобнее управлять вручную направлением
    private float totalGravityScale; /// <see cref="gravityScale"/> * 9.8 для удобства


    private Rigidbody2D rb;
    private BoxCollider2D col;
    private InputSystem inp;



    private void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        inp = new InputSystem();
    }

    private void Start()
    {
        totalGravityScale = gravityScale * 9.8f;
    }

    private void Update()
    {
        CoyotTimeCounterHandle();
    }

    private void FixedUpdate()
    {
        MoveHandle();
        JumpHandle();
    }

    public void ApplyForce(Vector2 force, ForceMode2D forceMode)
    {
        if (forceMode == ForceMode2D.Force) { externalForce += force * Time.deltaTime; }

        else if (forceMode == ForceMode2D.Impulse) { externalForce += force; }
    }

    private void OnEnable()
    {
        //передвижение
        inp.Player.Enable();

        //прыжок
        inp.Player.Jump.started += OnJumpBuffer;
        inp.Player.Jump.canceled += OnJumpCanceled;
    }

    private void OnDisable()
    {
        // передвижение
        inp.Player.Disable();

        // прыжок
        inp.Player.Jump.started -= OnJumpBuffer;
        inp.Player.Jump.canceled -= OnJumpCanceled;
    }

    private void OnJumpBuffer(InputAction.CallbackContext context) // начинает таймер буфера
    {
        jumpBufferTimeCounter = jumpBufferTime;
    }

    private void JumpHandle()
    {
        if (jumpBufferTimeCounter > 0 && IsCanJump())
        {
            jumpForce.y += jumpScale;
            coyoteTimeCounter = 0f;
            jumpBufferTimeCounter = 0f;
            isJustJump = true;
            // Debug.Log("jump");
        }

        jumpBufferTimeCounter -= Time.deltaTime;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context) // отвечает за контроль прыжка
    {
        if (rb.linearVelocityY > 1f)
        {
            jumpForce.y *= canceledJumpSpeedMultiplier;
            isJustJump = false;
        }
    }

    private void CoyotTimeCounterHandle()
    {
        if (IsGrounded() && !isJustJump)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private bool IsGrounded()
    {
        Bounds colliderBounds;
        Vector2 rayOrigin;
        float checkGroundedDistance = 0.01f;

        colliderBounds = col.bounds;
        rayOrigin = new Vector2(colliderBounds.min.x, colliderBounds.min.y - 0.01f);
        RaycastHit2D hit1 = Physics2D.Raycast(rayOrigin, Vector2.down, checkGroundedDistance);

        colliderBounds = col.bounds;
        rayOrigin = new Vector2(colliderBounds.max.x, colliderBounds.min.y - 0.01f);
        RaycastHit2D hit2 = Physics2D.Raycast(rayOrigin, Vector2.down, checkGroundedDistance);

        return hit1.collider != null || hit2.collider != null;
    }

    private bool IsCanJump()
    {
        return IsGrounded() || coyoteTimeCounter > 0f;
    }

    private void CeilingHandle() { }

    private void MoveHandle()
    {
        if (rb.linearVelocityY > -1f)
        {
            gravityForce = totalGravityScale;
        }
        else
        {
            gravityForce += totalGravityScale * Time.deltaTime;
        }

        Vector2 moveVector = inp.Player.Move.ReadValue<Vector2>(); // показания с A, D
        Vector2 internalForce = moveVector * moveSpeed;

        // Debug.Log(moveVector);
        // Debug.Log(internalForce);

        rb.linearVelocity = internalForce + externalForce + jumpForce + gravityVector * gravityForce;

        // Debug.Log(externalForce);

        externalForce = Vector2.Lerp(externalForce, Vector2.zero, externalForceDecayVelocity * Time.deltaTime); // угасание для Impulse

        if (jumpForce.y > gravityForce)
        {
            jumpForce = Vector2.Lerp(jumpForce, Vector2.zero, jumpForceDecayVelocity * Time.deltaTime);
        }
        else
        {
            jumpForce = Vector2.Lerp(jumpForce, Vector2.zero, fastJumpForceDecayVelocity * Time.deltaTime);
        }

        if (Mathf.Abs(rb.linearVelocityX) < 0.1f)
        {
            rb.linearVelocityX = 0;
        }
        if (Mathf.Abs(rb.linearVelocityY) < 0.1f)
        {
            rb.linearVelocityY = 0;
        }
    }
}