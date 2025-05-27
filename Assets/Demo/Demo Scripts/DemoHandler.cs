using JetCreative.Console;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Demo.Demo_Scripts
{
    
    public class DemoHandler: MonoBehaviour
    {
        [SerializeField] private InputAction openAction, completePrediction, enterCommand, lastCommand, nextCommand;

        private void Awake()
        {
            openAction.performed += context => JCCommandConsole.Instance.EnableConsole(true);
            completePrediction.performed += context => ConsoleUI.Instance.AcceptPrediction();
            enterCommand.performed += context => ConsoleUI.Instance.OnSubmitCommand();
            lastCommand.performed += context => ConsoleUI.Instance.ReloadLastCommand();
            nextCommand.performed += context => ConsoleUI.Instance.ReloadNextCommand();
        }


        private void OnEnable()
        {
            //consoleActions.action.Enable();
            openAction.Enable();
            completePrediction.Enable();
            enterCommand.Enable();
            lastCommand.Enable();
            nextCommand.Enable();
        }

        private void OnDisable()
        {
            //consoleActions.action.Disable();
            openAction.Disable();
            completePrediction.Disable();
            enterCommand.Disable();
            lastCommand.Disable();
            nextCommand.Disable();
        }

    }
}