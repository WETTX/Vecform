using System.Collections;
using NUnit.Framework;
using UnityEngine;
using System;

public class PlayerLife : MonoBehaviour
{
    public static PlayerLife Instance;

    public Transform spawnPoint;

    [SerializeField] private bool _respawn = true;
    [SerializeField] private float _respawnTime = 0.5f;

    public static event Action OnDeath;
    public static event Action OnRespawn;

    public bool isAlive { get; private set; } = true;

    private void Awake()
    {
        Instance = this;
    }

    public void Death()
    {
        isAlive = false;

        OnDeath?.Invoke();


        if (_respawn)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    public void Respawn() // МНГНОВЕННО спавнит (событием)
    {
        isAlive = true;

        OnRespawn?.Invoke();
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnTime);

        Respawn();
    }
}
