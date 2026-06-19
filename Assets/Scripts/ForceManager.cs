using System.Runtime.CompilerServices;
using UnityEngine;


/// <summary>
/// хранит силы
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PhysicsManager))]
public class ForceManager : MonoBehaviour
{
    [SerializeField] private float _gravityScale;
    [SerializeField] private float _impulseDecayVelocity = 5f; // скорость угасания импульсов
    // с прыжком всё по-другому тк контроль прыжка
    [SerializeField] private float _jumpImpulseDecayVelocity = 5f;
    [SerializeField] private float _fastJumpImpulseDecayVelocity = 15f;

    public Vector2 totalForce => _force + _impulse + _jumpImpulse + _gravityDirection * _gravityScalar;

    private Vector2 _force;
    private Vector2 _impulse;
    private Vector2 _jumpImpulse; // для игрока
    private Vector2 _gravityDirection = Vector2.down;
    private float _gravityScalar; // сделано не так как другие силы чтобы было удобнее вручную управлять направлением

    private Rigidbody2D rb;
    private PhysicsManager physicsManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        physicsManager = GetComponent<PhysicsManager>();
    }

    private void FixedUpdate()
    {
        ForcesHandle();

        Debug.Log(_gravityScalar);
    }

    public void ApplyForce(Vector2 applicableForce, ForceMode2D forceMode)
    {
        if (forceMode == ForceMode2D.Force) { _force += applicableForce * Time.deltaTime; }

        else if (forceMode == ForceMode2D.Impulse) { _impulse += applicableForce; }
    }

    public void ZeroForces() /// обнуляет <see cref="_force"/>
    {
        _force = Vector2.zero;
    }

    public void ApplyJumpImpulse(Vector2 applicableForce)
    {
        _jumpImpulse += applicableForce;
    }

    public void MultiplyJumpImpulse(float multiplier)
    {
        _jumpImpulse *= multiplier;
    }

    private void ForcesHandle()
    {
        // плавное ускорение вниз
        if (rb.linearVelocityY < -1f && !physicsManager.IsGrounded())
        {
            _gravityScalar += _gravityScale * Time.deltaTime;
        }
        else
        {
            _gravityScalar = _gravityScale;
        }

        // для отскока от потолка
        if (physicsManager.IsCeiling())
        {
            _impulse.y = 0f;
            _jumpImpulse.y = 0f;
        }

        _impulse = Vector2.Lerp(_impulse, Vector2.zero, _impulseDecayVelocity * Time.deltaTime); // угасание

        // угасание прыжка
        if (_jumpImpulse.y > _gravityScalar)
        {
            _jumpImpulse = Vector2.Lerp(_jumpImpulse, Vector2.zero, _jumpImpulseDecayVelocity * Time.deltaTime);
        }
        else
        {
            _jumpImpulse = Vector2.Lerp(_jumpImpulse, Vector2.zero, _fastJumpImpulseDecayVelocity * Time.deltaTime);
        }
    }
}
