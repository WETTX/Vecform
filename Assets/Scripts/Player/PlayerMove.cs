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
    [SerializeField] private float _moveSpeed = 10f;
    [SerializeField] private float _jumpScale = 10f;
    [SerializeField] private float _canceledJumpSpeedMultiplier = 0.7f; // какая часть скорости останется при отпускании кнопки прыжка
    [SerializeField] private float _coyoteTime = 0.5f;
    [Tooltip("NOT 0")]
    [SerializeField] private float _jumpBufferTime = 0.15f; // NOT 0

    [Space(5)]
    [Header("Physics")]
    [SerializeField] private float _externalForceDecayVelocity = 5f; // скорость угасания внешних импульсов
    [SerializeField] private float _jumpForceDecayVelocity = 5f; // скорость угасания импульса прыжка
    [SerializeField] private float _fastJumpForceDecayVelocity = 10f; // используется когда началось падение чтобы не было эффекта плавного падения после прыжка
    [SerializeField] private float _gravityScale = 1f;

    // силы
    private Vector2 _internalForce;
    private Vector2 _jumpForce; // отдельно чтобы был красивый контроль прыжка
    private Vector2 _externalForce;
    private Vector2 _gravityVector = Vector2.down;
    private float _gravityForce; // сделано не так как другие силы чтобы было удобнее вручную управлять направлением
    private Vector2 totalForce => _internalForce + _externalForce + _jumpForce + _gravityVector * _gravityForce;

    private float _coyoteTimeCounter;
    private float _jumpBufferTimeCounter;
    private float _totalGravityScale => _gravityScale * 9.8f; /// для удобства


    private Rigidbody2D rb;
    private BoxCollider2D col;
    private InputSystem inp;


    public bool IsCanJump { get { return IsGrounded() || _coyoteTimeCounter > 0f; } }

    public bool isJustJump { get; private set; } // костыль, флаг, чтобы не было дабл-прыжка

    private void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        inp = new InputSystem();
    }

    private void OnEnable()
    {
        //передвижение
        inp.Player.Enable();

        //прыжок
        inp.Player.Jump.started += OnJumpBuffer;
        inp.Player.Jump.canceled += OnJumpCanceled;
    }

    private void Start() { }

    private void Update()
    {
        CoyotTimeCounterHandle();
    }

    private void FixedUpdate()
    {
        ForcesHandle();
        JumpHandle();
    }

    private void OnDisable()
    {
        // передвижение
        inp.Player.Disable();

        // прыжок
        inp.Player.Jump.started -= OnJumpBuffer;
        inp.Player.Jump.canceled -= OnJumpCanceled;
    }

    public void ApplyForce(Vector2 force, ForceMode2D forceMode)
    {
        if (forceMode == ForceMode2D.Force) { _externalForce += force * Time.deltaTime; }

        else if (forceMode == ForceMode2D.Impulse) { _externalForce += force; }
    }

    private void OnJumpBuffer(InputAction.CallbackContext context) // начинает таймер буфера
    {
        _jumpBufferTimeCounter = _jumpBufferTime;
    }

    private void JumpHandle()
    {
        if (_jumpBufferTimeCounter > 0 && IsCanJump)
        {
            _jumpForce.y += _jumpScale;
            _coyoteTimeCounter = 0f;
            _jumpBufferTimeCounter = 0f;
            isJustJump = true;
            // Debug.Log("jump");
        }

        _jumpBufferTimeCounter -= Time.deltaTime;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context) // отвечает за контроль прыжка
    {
        if (rb.linearVelocityY > 1f)
        {
            _jumpForce.y *= _canceledJumpSpeedMultiplier;
            isJustJump = false;
        }
    }

    private void CoyotTimeCounterHandle()
    {
        if (IsGrounded() && !isJustJump)
        {
            _coyoteTimeCounter = _coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private bool IsGrounded()
    {
        Bounds colliderBounds;
        Vector2 rayOrigin;
        float checkGroundedDistance = 0.01f;

        colliderBounds = col.bounds;
        rayOrigin = new Vector2(colliderBounds.min.x, colliderBounds.min.y - 0.01f); // 0.01 чтобы не срабатывало на игрока
        RaycastHit2D hit1 = Physics2D.Raycast(rayOrigin, Vector2.down, checkGroundedDistance);

        colliderBounds = col.bounds;
        rayOrigin = new Vector2(colliderBounds.max.x, colliderBounds.min.y - 0.01f); // 0.01 чтобы не срабатывало на игрока
        RaycastHit2D hit2 = Physics2D.Raycast(rayOrigin, Vector2.down, checkGroundedDistance);

        return hit1.collider != null || hit2.collider != null;
    }

    private bool IsCeiling() // касается ли потолка
    {
        Bounds colliderBounds;
        Vector2 rayOrigin;
        float checkGroundedDistance = 0.01f;

        colliderBounds = col.bounds;
        rayOrigin = new Vector2(colliderBounds.min.x, colliderBounds.max.y + 0.01f); // 0.01 чтобы не срабатывало на игрока
        RaycastHit2D hit1 = Physics2D.Raycast(rayOrigin, Vector2.up, checkGroundedDistance);

        colliderBounds = col.bounds;
        rayOrigin = new Vector2(colliderBounds.max.x, colliderBounds.max.y + 0.01f); // 0.01 чтобы не срабатывало на игрока
        RaycastHit2D hit2 = Physics2D.Raycast(rayOrigin, Vector2.up, checkGroundedDistance);

        return hit1.collider != null || hit2.collider != null;
    }

    private void ForcesHandle() // обрабатывает силы и движение игрока
    {
        // плавное ускорение
        if (rb.linearVelocityY > -1f)
        {
            _gravityForce = _totalGravityScale;
        }
        else
        {
            _gravityForce += _totalGravityScale * Time.deltaTime;
        }

        // внутренняя скорость
        Vector2 moveVector = inp.Player.Move.ReadValue<Vector2>(); // показания с A, D
        Vector2 internalForce = moveVector * _moveSpeed;

        // применение общей скорости с учётом удара о потолок
        rb.linearVelocity = totalForce;

        // угасание для Impulse
        _externalForce = Vector2.Lerp(_externalForce, Vector2.zero, _externalForceDecayVelocity * Time.deltaTime);

        // угасание для прыжка
        if (_jumpForce.y > _gravityForce)
        {
            _jumpForce = Vector2.Lerp(_jumpForce, Vector2.zero, _jumpForceDecayVelocity * Time.deltaTime);
        }
        else
        {
            _jumpForce = Vector2.Lerp(_jumpForce, Vector2.zero, _fastJumpForceDecayVelocity * Time.deltaTime);
        }

        // обнуление малой скорости
        if (Mathf.Abs(rb.linearVelocityX) < 0.1f)
        {
            rb.linearVelocityX = 0;
        }
        if (Mathf.Abs(rb.linearVelocityY) < 0.1f)
        {
            rb.linearVelocityY = 0;
        }
    }

    // private void ResetForces()

    private enum Axis { x, y }

    private enum Forces
    {
        initial
    }
}