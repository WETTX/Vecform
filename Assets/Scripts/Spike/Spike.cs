using UnityEngine;

public class Spike : MonoBehaviour
{
    private const string PLAYER = "Player";

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag(PLAYER))
        {
            PlayerMove.Instance.Die();
        }
    }
}
