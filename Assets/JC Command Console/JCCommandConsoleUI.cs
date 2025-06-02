using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Manages the UI for the Command Console Pro system.
    /// Provides input field, command logging, and predictive text functionality.
    /// </summary>
    [AddComponentMenu("Jet Creative/Command Console/Console UI")]
    public class JCCommandConsoleUI : MonoBehaviour
    {
        #region Singleton

        /// <summary>
        /// Singleton instance of the JCCommandConsoleUI.
        /// </summary>
        public static JCCommandConsoleUI Instance { get; private set; }

        #endregion

        #region UI References

        [Header("UI References")]
        [SerializeField]
        private GameObject consolePanel;
        [SerializeField] private ScrollRect outputScrollRect;
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private TMP_InputField commandInputField;
        [SerializeField] private TMP_Text predictiveText;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button closeButton;

        #endregion

        #region Command History

        /// <summary>
        /// Maximum number of commands to store in history.
        /// </summary>
        [Header("Command History")]
        [SerializeField] private int maxHistorySize = 50;
        [SerializeField] private int maxOutputLines = 100;
        private readonly List<string> commandHistory = new();
        private int historyIndex = -1;

        #endregion

        #region Input Actions

        [Header("Input Actions")]
        [SerializeField] private InputActionReference toggleConsoleAction;
        [SerializeField] private InputActionReference submitCommandAction;
        [SerializeField] private InputActionReference acceptPredictionAction;
        [SerializeField] private InputActionReference previousCommandAction;
        [SerializeField] private InputActionReference nextCommandAction;

        #endregion

        #region Style Settings

        [Header("Style Settings")]
        [SerializeField] private Color commandColor = new Color(0.8f, 0.8f, 1f);
        [SerializeField] private Color resultColor = Color.white;
        [SerializeField] private Color errorColor = new Color(1f, 0.5f, 0.5f);
        [SerializeField] private Color predictiveColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);

        #endregion

        #region Cursor Management

        [Header("Cursor Settings")]
        [SerializeField] private bool forceMouseVisibleOnOpen = true;

        // Store previous cursor state
        private CursorLockMode previousCursorLockMode;
        private bool previousCursorVisible;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            //Ensure the console is initially disabled
            if (consolePanel)
                consolePanel.SetActive(false);

            // Initialize UI if not set through inspector
            VerifyUI();
        }

        private void OnEnable()
        {
            // Register input actions
            if (toggleConsoleAction)
            {
                toggleConsoleAction.action.Enable();
                toggleConsoleAction.action.performed += ToggleConsole;
            }
            
            if (submitCommandAction)
            {
                submitCommandAction.action.Enable();
                submitCommandAction.action.performed += SubmitCommandInput;
            }
            
            if (acceptPredictionAction)
            {
                acceptPredictionAction.action.Enable();
                acceptPredictionAction.action.performed += AcceptPrediction;
            }
            
            if (previousCommandAction)
            {
                previousCommandAction.action.Enable();
                previousCommandAction.action.performed += NavigateHistoryUp;
            }
            
            if (nextCommandAction)
            {
                nextCommandAction.action.Enable();
                nextCommandAction.action.performed += NavigateHistoryDown;
            }

            // Set up UI button events
            if (submitButton)
                submitButton.onClick.AddListener(SubmitCommand);
            
            if (closeButton)
                closeButton.onClick.AddListener(CloseConsole);

            // Set up input field events
            if (commandInputField)
            {
                commandInputField.onValueChanged.AddListener(UpdatePredictiveText);
                commandInputField.onSubmit.AddListener(OnInputFieldSubmit);
            }
        }

        private void OnDisable()
        {
            // Unregister input actions
            if (toggleConsoleAction)
                toggleConsoleAction.action.performed -= ToggleConsole;
            
            if (submitCommandAction)
                submitCommandAction.action.performed -= SubmitCommandInput;
            
            if (acceptPredictionAction)
                acceptPredictionAction.action.performed -= AcceptPrediction;
            
            if (previousCommandAction)
                previousCommandAction.action.performed -= NavigateHistoryUp;
            
            if (nextCommandAction)
                nextCommandAction.action.performed -= NavigateHistoryDown;

            // Remove UI button listeners
            if (submitButton)
                submitButton.onClick.RemoveListener(SubmitCommand);
            
            if (closeButton)
                closeButton.onClick.RemoveListener(CloseConsole);

            // Remove input field listeners
            if (commandInputField)
            {
                commandInputField.onValueChanged.RemoveListener(UpdatePredictiveText);
                commandInputField.onSubmit.RemoveListener(OnInputFieldSubmit);
            }
        }

        private void Start()
        {
            // Generate command cache if not already done
            if (!JCCommandConsole.Instance.HasGeneratedCache)
            {
                JCCommandConsole.Instance?.GenerateCommandCache();
            }
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// Verify all UI components are set in the inspector.
        /// </summary>
        private void VerifyUI()
        {
            // Create console panel if needed
            if (!consolePanel)
            {
                Debug.LogWarning("Console panel not assigned, JCCommandConsoleUI requires manual UI setup.");
            }

            // Check if essential components are assigned
            if (!outputText)
            {
                Debug.LogWarning("Output text component not assigned in JCCommandConsoleUI.");
            }

            if (!commandInputField)
            {
                Debug.LogWarning("Command input field not assigned in JCCommandConsoleUI.");
            }

            if (!predictiveText)
            {
                Debug.LogWarning("Predictive text component not assigned in JCCommandConsoleUI.");
            }
        }

        #endregion

        #region Console Control

        /// <summary>
        /// Toggles the console visibility based on input action.
        /// </summary>
        private void ToggleConsole(InputAction.CallbackContext context)
        {
            if (consolePanel.activeSelf)
                CloseConsole();
            else
                OpenConsole();
        }

        /// <summary>
        /// Opens the console and focuses the input field.
        /// </summary>
        public void OpenConsole()
        {
            if (!consolePanel)
                return;

            // Check for empty command cache and display warning
            CheckAndWarnEmptyCommandCache();

            // Store current cursor state before opening console
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            consolePanel.SetActive(true);
            
            // Apply cursor settings for console use
            if (forceMouseVisibleOnOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            // Ensure input field is selected and focused
            if (commandInputField)
            {
                commandInputField.ActivateInputField();
                commandInputField.Select();
                UpdatePredictiveText(commandInputField.text);
            }

            // Scroll output to bottom
            ScrollOutputToBottom();
        }

        /// <summary>
        /// Checks if the command cache is empty and displays a warning message if it is.
        /// Provides instructions on how to generate the command cache.
        /// </summary>
        private void CheckAndWarnEmptyCommandCache()
        {
            var commandCache = JCCommandConsole.GetCommandCache();
            
            if (commandCache != null && commandCache.IsEmpty())
            {
                string warningMessage = "⚠️ Command cache is empty! No commands are available.\n" +
                                      "To generate the command cache:\n" +
                                      "• Editor: Tools → Jet Creative → Command Console\n" +
                                      "• Inspector: Select CommandCache.asset and click 'Generate Command Cache'\n" +
                                      "• Runtime: Call JCCommandConsole.Instance.GenerateCommandCache()";
                
                LogToOutput(warningMessage, errorColor);
                LogToOutput("Type 'help' for basic console information.", resultColor);
            }
        }

        /// <summary>
        /// Closes the console.
        /// </summary>
        public void CloseConsole()
        {
            if (!consolePanel)
                return;

            consolePanel.SetActive(false);
            
            // Restore previous cursor state
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }

        /// <summary>
        /// Checks if the console is currently open.
        /// </summary>
        public bool IsConsoleOpen()
        {
            return consolePanel && consolePanel.activeSelf;
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Submits the command from the input field.
        /// </summary>
        public void SubmitCommand()
        {
            if (commandInputField == null)
                return;

            string command = commandInputField.text.Trim();
            if (string.IsNullOrEmpty(command))
                return;

            // Add command to history
            AddToHistory(command);

            // Log the command to output
            LogToOutput($"> {command}", commandColor);

            // Execute the command
            string result = JCCommandConsole.Instance.ExecuteCommand(command);
            
            // Check if result contains an error
            bool isError = result.StartsWith("Error:", StringComparison.OrdinalIgnoreCase);
            LogToOutput(result, isError ? errorColor : resultColor);

            // Clear input field and refocus it
            commandInputField.text = "";
            commandInputField.ActivateInputField();
            commandInputField.Select();
            
            // Clear predictive text
            if (predictiveText != null)
                predictiveText.text = GetFormattedHelpText();
            
            // Scroll to the bottom of the output
            ScrollOutputToBottom();
        }

        /// <summary>
        /// Submits the command from input action.
        /// </summary>
        private void SubmitCommandInput(InputAction.CallbackContext context)
        {
            SubmitCommand();
        }

        /// <summary>
        /// Handles submit event from the input field.
        /// </summary>
        private void OnInputFieldSubmit(string command)
        {
            SubmitCommand();
        }

        /// <summary>
        /// Logs a message to the output text area with specified color.
        /// </summary>
        private void LogToOutput(string message, Color color)
        {
            if (outputText == null)
                return;

            // Convert color to hex
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            
            // Add the message with color
            outputText.text += $"<color=#{colorHex}>{message}</color>\n";
            
            // Limit number of lines in output
            if (maxOutputLines > 0)
            {
                var lines = outputText.text.Split('\n');
                if (lines.Length > maxOutputLines)
                {
                    outputText.text = string.Join("\n", lines.Skip(lines.Length - maxOutputLines));
                }
            }
            
            // Scroll to bottom
            ScrollOutputToBottom();
        }
        
        /// <summary>
        /// Scrolls the output text area to the bottom.
        /// </summary>
        private void ScrollOutputToBottom()
        {
            if (outputScrollRect != null)
            {
                // Wait for end of frame to ensure layout has been updated
                Canvas.ForceUpdateCanvases();
                outputScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        /// <summary>
        /// Clears the console output.
        /// </summary>
        public void ClearOutput()
        {
            if (outputText != null)
                outputText.text = string.Empty;
        }

        #endregion

        #region Command History

        /// <summary>
        /// Adds a command to history.
        /// </summary>
        private void AddToHistory(string command)
        {
            // Don't add duplicates in a row
            if (commandHistory.Count > 0 && commandHistory[0] == command)
                return;

            commandHistory.Insert(0, command);
            if (commandHistory.Count > maxHistorySize)
                commandHistory.RemoveAt(commandHistory.Count - 1);

            historyIndex = -1;
        }

        /// <summary>
        /// Navigates to the previous command in history.
        /// </summary>
        private void NavigateHistoryUp(InputAction.CallbackContext context)
        {
            if (commandHistory.Count == 0 || commandInputField == null)
                return;

            if (!IsConsoleOpen())
                return;

            historyIndex = Mathf.Min(historyIndex + 1, commandHistory.Count - 1);
            commandInputField.text = commandHistory[historyIndex];
            commandInputField.caretPosition = commandInputField.text.Length;
            UpdatePredictiveText(commandInputField.text);
        }

        /// <summary>
        /// Navigates to the next command in history.
        /// </summary>
        private void NavigateHistoryDown(InputAction.CallbackContext context)
        {
            if (commandInputField == null || !IsConsoleOpen())
                return;

            if (historyIndex <= 0)
            {
                historyIndex = -1;
                commandInputField.text = "";
            }
            else
            {
                historyIndex--;
                commandInputField.text = commandHistory[historyIndex];
                commandInputField.caretPosition = commandInputField.text.Length;
            }
            
            UpdatePredictiveText(commandInputField.text);
        }

        /// <summary>
        /// Clears the command history.
        /// </summary>
        public void ClearHistory()
        {
            commandHistory.Clear();
            historyIndex = -1;
        }

        #endregion

        #region Predictive Text

        /// <summary>
        /// Updates the predictive text based on current input.
        /// </summary>
        private void UpdatePredictiveText(string currentInput)
        {

            if (string.IsNullOrEmpty(currentInput))
            {
                predictiveText.text = GetFormattedHelpText();
                return;
            }

            // Parse the current input to identify tokens
            List<string> tokens = currentInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            
            string targetCmd = string.Empty;
            var consoleCmd = string.Empty; 
            var commandName = string.Empty;

            //if first token is not a target command then add empty token at index 0 to make indexing consistent
            if (JCCommandConsole.StartsWithAny(tokens[0], JCCommandConsole.TargetCmds, out string prefaceCmd))
                targetCmd = prefaceCmd;
            else
                tokens.Insert(0, "");
            
            if ( tokens.Count > 1 && JCCommandConsole.ConsoleCmds.Contains(tokens[1].ToLower()))
                consoleCmd = tokens[1].ToLower();
            
            if (tokens.Count > 2 && JCCommandConsole.Instance.GetCommandTypeInfo(tokens[2].ToLower()) != null)
                commandName = tokens[2].ToLower();
            
            // Check if the last token is valid
            bool lastTokenValid = IsValidToken(tokens[^1]);
            string currentToken = tokens[^1];
            
            // Get predictions for the next word
            string[] predictions = JCCommandConsole.Instance.PredictCurrentWord(currentInput);
            
            //Format predictions
            if (predictions.Length > 0)
            {
                string predictionColorHex = ColorUtility.ToHtmlStringRGB(predictiveColor);
                
                // For the third token (command name), show type information
                if (tokens.Count == 3 && !string.IsNullOrEmpty(consoleCmd))
                {
                    string predCommand = predictions[0].ToLower();
                    string typeInfo = JCCommandConsole.Instance.GetCommandTypeInfo(predCommand);
                    
                    if (!string.IsNullOrEmpty(typeInfo))
                    {
                        predictiveText.text = commandInputField.text + $"{predCommand[currentToken.Length..]} <color=#{predictionColorHex}>{typeInfo}</color>";
                        return;
                    }
                }

                // Show the first prediction
                string prediction = predictions[0];
                
                // If the prediction completes the current input, show it
                if (tokens.Count > 0 && prediction.StartsWith(currentToken, StringComparison.OrdinalIgnoreCase))
                {
                    string completion = prediction[currentToken.Length..];
                    predictiveText.text = $"{currentInput}<color=#{predictionColorHex}>{completion}</color>";
                }
            }
            else
            {
                // Check if we're handling a method/delegate call with parameters
                if (tokens.Count >= 4 && consoleCmd == "call")
                {
                    //string commandName = tokens[2].ToLower();
                    
                    if (JCCommandConsole.Instance.GetCommandTypeInfo(commandName) != null)
                    {
                        string typeInfo = JCCommandConsole.Instance.GetCommandTypeInfo(commandName);
                        //determine if there are too many parameters
                        var numParameters = typeInfo.Split(',').Length;
                        
                        if (tokens.Count - 2 > numParameters)
                            predictiveText.text = commandInputField.text + $"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>     Too many parameters...</color>";
                        
                        string predictionColorHex = ColorUtility.ToHtmlStringRGB(predictiveColor);
                        predictiveText.text = commandInputField.text + $"{commandName[currentToken.Length..]}<color=#{predictionColorHex}>{typeInfo}</color>";
                        return;
                    }
                }
                
                // Check if we're handling a property set with parameters
                if (tokens.Count >= 4 && tokens[1].ToLower() == "set")
                {
                    //check is too many parameters
                    if (tokens.Count > 4)
                    {
                        predictiveText.text = commandInputField.text + $"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>     Too many parameters...</color>";
                        return;
                    }
                    
                    if (JCCommandConsole.Instance.GetCommandTypeInfo(commandName) != null)
                    {
                        string typeInfo = JCCommandConsole.Instance.GetCommandTypeInfo(commandName);
                        string predictionColorHex = ColorUtility.ToHtmlStringRGB(predictiveColor);
                        predictiveText.text = commandInputField.text + $"<color=#{predictionColorHex}>{typeInfo}</color>";
                        return;
                    }
                }
                
                if (!lastTokenValid && predictions.Length == 0)
                {
                    predictiveText.text = commandInputField.text + $"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>     Invalid command...</color>";
                    return;
                }
                
                // No predictions available
                predictiveText.text = "";
            }
        }

        
        /// <summary>
        /// Checks if a token is valid in the command context.
        /// </summary>
        private bool IsValidToken(string token)
        {
            // Check if it's a preface command
            if (JCCommandConsole.TargetCmds.Any(token.StartsWith))
            {
                return true;
            }
            
            // Check if it's a console command
            if (JCCommandConsole.ConsoleCmds.Contains(token.ToLower()))
                return true;
            
            // Check if it's "select"
            if (token.ToLower() == "select")
                return true;
            
            // Check if it's a command name
            var allCommands = JCCommandConsole.Instance.GetAllCommands();
            
            return allCommands.Contains(token.ToLower());
        }

        /// <summary>
        /// Gets formatted help text for when input is empty.
        /// </summary>
        private string GetFormattedHelpText()
        {
            string predictionColorHex = ColorUtility.ToHtmlStringRGB(predictiveColor);
            return $"<color=#{predictionColorHex}>Type a command or use @ to target GameObjects</color>";
        }

        /// <summary>
        /// Accepts the current prediction and fills in the input field.
        /// </summary>
        private void AcceptPrediction(InputAction.CallbackContext context)
        {
            if (!commandInputField || !IsConsoleOpen())
                return;

            string[] predictions = JCCommandConsole.Instance.PredictCurrentWord(commandInputField.text);
            if (predictions.Length > 0)
            {
                string[] tokens = commandInputField.text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string updatedText;
                
                // Check if we're completing a token or adding a new one
                if (tokens.Length > 0 && predictions[0].StartsWith(tokens[^1], StringComparison.OrdinalIgnoreCase))
                {
                    // Replace the current token with the prediction
                    tokens[^1] = predictions[0];
                    updatedText = string.Join(" ", tokens);
                }
                else
                {
                    // Add the prediction as a new token
                    updatedText = commandInputField.text.TrimEnd() + " " + predictions[0];
                }
                
                commandInputField.text = updatedText;
                commandInputField.caretPosition = updatedText.Length;
                UpdatePredictiveText(updatedText);
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Executes a command programmatically.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="logToConsole">Whether to log the command and result to the console</param>
        /// <returns>The result of the command execution</returns>
        public string ExecuteCommand(string command, bool logToConsole = true)
        {
            if (string.IsNullOrEmpty(command))
                return "Error: Empty command.";

            if (logToConsole && outputText)
                LogToOutput($"> {command}", commandColor);

            string result = JCCommandConsole.Instance.ExecuteCommand(command);
            
            if (logToConsole && outputText)
            {
                bool isError = result.StartsWith("Error:", StringComparison.OrdinalIgnoreCase);
                LogToOutput(result, isError ? errorColor : resultColor);
            }

            return result;
        }

        /// <summary>
        /// Logs a message to the console output.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="isError">Whether the message is an error</param>
        public void Log(string message, bool isError = false)
        {
            if (!outputText)
                return;

            LogToOutput(message, isError ? errorColor : resultColor);
        }

        #endregion
    }
}