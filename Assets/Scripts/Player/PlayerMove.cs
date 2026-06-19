using System;
using System.Collections;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// обрабатывает кнопки игрока
/// </summary>
[RequireComponent(typeof(PhysicsManager))]
public class PlayerMove : MonoBehaviour
{
    public static PlayerMove Instance { get; private set; }

    [Header("Move")]
    [SerializeField] private float _moveSpeed = 23f;
    [SerializeField] private float _jumpScale = 121f;
    [SerializeField] private float _canceledJumpSpeedMultiplier = 0.3f; // какая часть скорости останется при отпускании кнопки прыжка
    [SerializeField] private float _coyoteTime = 0.07f;
    [Tooltip("NOT 0")]
    [SerializeField] private float _jumpBufferTime = 0.08f; // NOT 0

    [Space(5)]
    [Header("Physics")]
    // [SerializeField] private float _gravityScale = 3.5f;

    private float _coyoteTimeCounter;
    private float _jumpBufferTimeCounter;
    // private float _totalGravityScale => _gravityScale * 9.8f; /// для удобства


    private Rigidbody2D rb;
    // private BoxCollider2D col;
    private InputSystem inp;
    private ForceManager forceManager;
    private PhysicsManager physicsManager;


    public bool IsCanJump { get { return physicsManager.IsGrounded() || _coyoteTimeCounter > 0f; } }

    public bool IsJustJump { get; private set; } // костыль, флаг, чтобы не было дабл-прыжка

    private void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        // col = GetComponent<BoxCollider2D>();
        forceManager = GetComponent<ForceManager>();
        physicsManager = GetComponent<PhysicsManager>();

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

    private void Update()
    {
        CoyotTimeCounterHandle();
    }

    private void FixedUpdate()
    {
        MoveHandle();
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

    public void Die()
    {
        Debug.Log("Die");
    }

    private void OnJumpBuffer(InputAction.CallbackContext context) // начинает таймер буфера
    {
        _jumpBufferTimeCounter = _jumpBufferTime;
    }

    private void JumpHandle()
    {
        if (_jumpBufferTimeCounter > 0 && IsCanJump)
        {
            forceManager.ApplyJumpImpulse(Vector2.up * _jumpScale);
            _coyoteTimeCounter = 0f;
            _jumpBufferTimeCounter = 0f;
            IsJustJump = true;
            // Debug.Log("jump");
        }

        _jumpBufferTimeCounter -= Time.deltaTime;
    }

    private void OnJumpCanceled(InputAction.CallbackContext context) // отвечает за контроль прыжка
    {
        if (rb.linearVelocityY > 1f)
        {
            forceManager.MultiplyJumpImpulse(_canceledJumpSpeedMultiplier);
        }
        IsJustJump = false;
    }

    private void CoyotTimeCounterHandle()
    {
        if (physicsManager.IsGrounded() && !IsJustJump)
        {
            _coyoteTimeCounter = _coyoteTime;
        }
        else
        {
            _coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void MoveHandle() // обрабатывает движение игрока
    {
        Vector2 moveVector = inp.Player.Move.ReadValue<Vector2>(); // показания с A, D

        forceManager.ApplyForce(moveVector * _moveSpeed, ForceMode2D.Force);
    }
}