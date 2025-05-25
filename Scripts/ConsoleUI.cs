using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JetCreative.Console
{
    /// <summary>
    /// Handles the display and logging of messages in the console UI within the Unity environment.
    /// Provides functionality to log messages and errors to the console display.
    /// </summary>
    public class ConsoleUI : MonoBehaviour
    {
        /// <summary>
        /// A private singleton instance of the <see cref="ConsoleUI"/> class.
        /// Used to ensure only one instance of the ConsoleUI exists and provides a global point of access.
        /// </summary>
        private static ConsoleUI _instance;

        /// <summary>
        /// Represents the text element in the console UI that displays output messages and console history.
        /// This variable is used to dynamically update the console display with logs and error messages.
        /// </summary>
        [SerializeField] private TMP_Text outputText;

        /// <summary>
        /// Represents the input field in the console UI where users can type commands.
        /// </summary>
        /// <remarks>
        /// This TMP_InputField is used to capture user input for executing console commands.
        /// When a command is submitted, the text from the input field is processed, executed,
        /// and the field is cleared for the next command.
        /// </remarks>
        [SerializeField] private TMP_InputField inputField;

        /// <summary>
        /// Represents a button in the console user interface that triggers the submission of player commands.
        /// </summary>
        /// <remarks>
        /// When clicked, this button executes the associated logic for processing and submitting console commands entered by the user.
        /// </remarks>
        [SerializeField] private Button submitButton;

        /// <summary>
        /// Represents a button in the console interface that allows the user
        /// to exit or disable the console UI when clicked.
        /// </summary>
        [SerializeField] private Button exitButton;

        /// <summary>
        /// Stores the history of all messages logged to the console within the ConsoleUI class.
        /// This includes regular logs as well as error messages, formatted and appended
        /// to maintain a sequential history of outputs.
        /// </summary>
        private string consoleHistory = "";

        /// Provides a singleton instance of the ConsoleUI class. This property ensures that
        /// there is only one active instance of the ConsoleUI in the scene. If the instance
        /// doesn't exist, it initializes the instance by finding it within the child objects
        /// of the JCCommandConsole.
        public static ConsoleUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = JCCommandConsole.Instance.gameObject.GetComponentInChildren<ConsoleUI>(true);
                }
                return _instance;
            }
        }

        /// <summary>
        /// Called by Unity when the script instance is being loaded.
        /// Initializes the console user interface by setting up event listeners for UI elements
        /// like submit and exit buttons, as well as the input field's submission.
        /// </summary>
        private void Awake()
        {
            InitializeUI();
        }

        /// <summary>
        /// Initializes the console user interface components by setting up event listeners for user interaction.
        /// </summary>
        /// <remarks>
        /// This method connects the submit and exit buttons to their respective callback handlers.
        /// Additionally, it configures the input field to handle the submission of text commands upon user input.
        /// </remarks>
        private void InitializeUI()
        {
            submitButton.onClick.AddListener(OnSubmitCommand);
            exitButton.onClick.AddListener(() => JCCommandConsole.Instance.EnableConsole(false));
            
            inputField.onSubmit.AddListener(_ => OnSubmitCommand());
        }

        /// <summary>
        /// Handles the submission of commands entered in the console input field.
        /// This method is invoked when the submit button is clicked or when the user presses Enter in the input field.
        /// </summary>
        /// <remarks>
        /// - Retrieves the text from the input field, trims any leading or trailing whitespace, and validates the input.
        /// - Logs the submitted command in the console output.
        /// - Executes the command via the command execution system of the JCCommandConsole class.
        /// - Clears the input field and refocuses it for further input.
        /// </remarks>
        private void OnSubmitCommand()
        {
            string command = inputField.text.Trim();
            if (string.IsNullOrEmpty(command)) return;

            Log($"> {command}");
            JCCommandConsole.Instance.ExecuteCommand(command);
            inputField.text = "";
            inputField.ActivateInputField();
        }

        /// <summary>
        /// Appends a message to the console output and updates the displayed output text in the console UI.
        /// </summary>
        /// <param name="message">The message to be logged to the console.</param>
        public void Log(string message)
        {
            consoleHistory += $"{message}\n";
            outputText.text = consoleHistory;
        }

        /// Logs an error message to the console with red-colored text to indicate an error.
        /// <param name="message">The error message to be logged to the console.</param>
        public void LogError(string message)
        {
            Log($"<color=red>Error: {message}</color>");
        }
    }
}