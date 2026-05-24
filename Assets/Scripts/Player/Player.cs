using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    /// <summary>
    /// sdh
    /// </summary>
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float canceledJumpSpeedMultiplier = 0.7f; //0 <= x < 1 какая часть скорости останется при отпускании кнопки прыжка

    
    private float moveX; //показания с A, D


    private Rigidbody2D rb;
    private InputSystem inp;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

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
        if (context.started)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        else if (context.canceled && rb.linearVelocityY > 0)
        {
            rb.linearVelocityY *= canceledJumpSpeedMultiplier;
        }
    }

}
