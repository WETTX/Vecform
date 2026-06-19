using UnityEngine;

/// <summary>
/// применяет силы, содержит методы и свойства связанные с физикой
/// </summary>
[RequireComponent(typeof(ForceManager))]
[RequireComponent(typeof(Rigidbody2D))]
public class PhysicsManager : MonoBehaviour
{
    private BoxCollider2D col;
    private ForceManager forceManager;
    private Rigidbody2D rb;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        forceManager = GetComponent<ForceManager>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        PhysicsHandle();
    }

    public bool IsGrounded()
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

    public bool IsCeiling() // касается ли потолка
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

    private void PhysicsHandle()
    {
        rb.linearVelocity = forceManager.totalForce;

        forceManager.ZeroForces(); // чтобы не накапливалась и не было инерции

        // обнуление при малых скоростях
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
