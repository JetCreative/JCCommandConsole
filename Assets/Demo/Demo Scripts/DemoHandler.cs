using System;
using JetCreative.Console;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo.Demo_Scripts
{
    [RequireComponent(typeof(PlayerInput))]
    public class DemoHandler: MonoBehaviour
    {
        [SerializeField] private InputActionReference openConsoleAction;

        private void Start()
        {
            openConsoleAction.action.performed += ctx => JetCreative.Console.JCCommandConsole.Instance.EnableConsole(true);
        }

        private void OnEnable()
        {
            openConsoleAction.action.Enable();
        }

        private void OnDisable()
        {
            openConsoleAction.action.Disable();
        }

        [Command]public float setthisfloat;

    }
}