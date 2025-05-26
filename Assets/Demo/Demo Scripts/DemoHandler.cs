using System;
using JetCreative.Console;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Demo.Demo_Scripts
{
    [RequireComponent(typeof(PlayerInput))]
    public class DemoHandler: MonoBehaviour
    {
        //[FormerlySerializedAs("openConsoleAction")] [SerializeField] private InputActionReference consoleActions;
        [SerializeField] private InputAction openAction, completePrediction, enterCommand, lastCommand;

        private void Awake()
        {
            openAction.performed += context => JCCommandConsole.Instance.EnableConsole(true);
            completePrediction.performed += context => ConsoleUI.Instance.AcceptPrediction();
            enterCommand.performed += context => ConsoleUI.Instance.OnSubmitCommand();
        }


        private void OnEnable()
        {
            //consoleActions.action.Enable();
            openAction.Enable();
            completePrediction.Enable();
            enterCommand.Enable();
            lastCommand.Enable();
        }

        private void OnDisable()
        {
            //consoleActions.action.Disable();
            openAction.Disable();
            completePrediction.Disable();
            enterCommand.Disable();
            lastCommand.Disable();
        }

        [Command]public float setthisfloat;

    }
}