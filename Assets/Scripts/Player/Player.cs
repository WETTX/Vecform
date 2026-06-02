using System;
using System.Collections;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float jumpScale = 10f;
    [SerializeField] private float canceledJumpSpeedMultiplier = 0.7f; // какая часть скорости останется при отпускании кнопки прыжка
    [SerializeField] private float coyoteTime = 0.5f;

    [Space(5)]
    [Header("Physics")]
    [SerializeField] private float externalForceDecayVelocity = 5f; // скорость угасания внешних импульсов
    [SerializeField] private float jumpForceDecayVelocity = 5f; // скорость угасания импульса прыжка
                                                                // чтобы было удобнее управлять вручную направлением
    [SerializeField] private float fastJumpForceDecayVelocity = 10f; // используется когда началось падение 
                                                                     // чтобы не было эффекта плавного падения
    [SerializeField] private float gravityScale = 1f;
    // [SerializeField] private float gravityAcceleration = 5f; // как быстро игрок приобретет максимальную скорость во время падения с обрыва

    [Space(5)]
    [Header("Debug")]
    [SerializeField] private Vector2 kaboomVector;
    [SerializeField] private float kaboomVectorMultiplier;
    [SerializeField] private bool wind = false; // будет ли ветер
    [SerializeField] private Vector2 windVector;
    [SerializeField] private float windVectorMultiplier = 0;


    private bool isGrounded; // стоит ли на земле
    private bool isCanJump; /// как <see cref="isGrounded"/> но с учётом койот-тайма
    private bool isJustJump; // костыль, флаг, чтобы не было дабл-прыжка
    private float coyoteTimeCounter;
    private Vector2 jumpForce; // отдельно чтобы был красивый контроль прыжка
    private Vector2 externalForce;
    private Vector2 gravityVector = Vector2.down;

    private float gravityForce; // сделано не так как другие силы
                                // чтобы было удобнее управлять вручную направлением
    private float totalGravityScale; /// <see cref="gravityScale"/> * 9.8 для удобства

    // private Vector2 zeroExternalForce = new Vector2(0f, -9.8f); // 9.8 это гравитация



    private Rigidbody2D rb;
    private BoxCollider2D col;
    private InputSystem inp;



    private void Awake()
    {
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
        GroundedHandler();

        // Debug.Log(jumpForce);
        // Debug.Log(rb.linearVelocityY);
        Debug.Log(gravityForce);

    }

    private void FixedUpdate()
    {
        MoveHandler();

        Wind();
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
        inp.Player.Jump.started += OnJump;
        inp.Player.Jump.canceled += OnJump;

        // дебаг
        inp.Player.Crouch.started += OnKaboom;
        // inp.Player.Sprint.performed += OnWind;
    }

    private void OnDisable()
    {
        // передвижение
        inp.Player.Disable();

        // прыжок
        inp.Player.Jump.started -= OnJump;
        inp.Player.Jump.canceled -= OnJump;

        // дебаг
        inp.Player.Crouch.started -= OnKaboom;
        // inp.Player.Sprint.performed -= OnWind;
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.started && isCanJump)
        {
            jumpForce.y += jumpScale;
            coyoteTimeCounter = 0f;
            isJustJump = true;
            Debug.Log("jump");
        }
        else if (context.canceled && rb.linearVelocityY > 1f)
        {
            jumpForce.y *= canceledJumpSpeedMultiplier;
            isJustJump = false;
        }
    }

    private void OnKaboom(InputAction.CallbackContext context)
    {
        ApplyForce(kaboomVector * kaboomVectorMultiplier, ForceMode2D.Impulse);
        Debug.Log("kaboom");
    }

    private void Wind()
    {
        ApplyForce(windVector * windVectorMultiplier * Convert.ToInt32(wind), ForceMode2D.Force);
        // Debug.Log("wind");
    }

    /// <summary>
    /// обновляет <see cref="isGrounded"/> и <see cref="isCanJump"/>
    /// </summary>
    private void GroundedHandler()
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

    private void MoveHandler()
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

        rb.linearVelocity = internalForce + externalForce + jumpForce + gravityVector * gravityForce;

        // externalForce = Vector2.Lerp(externalForce, zeroExternalForce * gravityScale, externalForceDecayVelocity * Time.deltaTime); // угасание для Impulse
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