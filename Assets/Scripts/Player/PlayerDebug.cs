using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ForceManager))]
public class PlayerDebug : MonoBehaviour
{
    [SerializeField] private Vector2 kaboomVector;
    [SerializeField] private float kaboomScale;
    [SerializeField] private bool wind = false; // будет ли ветер
    [SerializeField] private Vector2 windVector;
    [SerializeField] private float windScale = 0;

    private InputSystem inp;
    private ForceManager forceManager;

    private void Awake()
    {
        forceManager = GetComponent<ForceManager>();

        inp = new InputSystem();
    }

    private void FixedUpdate()
    {
        Wind();
    }

    private void OnEnable()
    {
        inp.Player.Enable();

        inp.Player.Crouch.started += OnKaboom;
    }

    private void OnDisable()
    {
        inp.Player.Disable();

        inp.Player.Crouch.started -= OnKaboom;
    }

    private void OnKaboom(InputAction.CallbackContext context)
    {
        forceManager.ApplyForce(kaboomVector * kaboomScale, ForceMode2D.Impulse);
        // Debug.Log("kaboom");
    }

    private void Wind()
    {
        forceManager.ApplyForce(windVector * windScale * Convert.ToInt32(wind), ForceMode2D.Force);
        // Debug.Log("wind");
    }
}
