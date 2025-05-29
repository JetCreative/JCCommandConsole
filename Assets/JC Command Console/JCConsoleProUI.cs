using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Manages the UI for the Command Console Pro system.
    /// Provides input field, command logging, and predictive text functionality.
    /// </summary>
    public class JCConsoleProUI : MonoBehaviour
    {
        #region Singleton

        /// <summary>
        /// Singleton instance of the JCConsoleProUI.
        /// </summary>
        public static JCConsoleProUI Instance { get; private set; }

        #endregion

        #region UI References

        [Header("UI References")]
        [SerializeField]
        internal GameObject consolePanel;
        [SerializeField] internal ScrollRect outputScrollRect;
        [SerializeField] internal TMP_Text outputText;
        [SerializeField] internal TMP_InputField commandInputField;
        [SerializeField] internal TMP_Text predictiveText;
        [SerializeField] internal Button submitButton;
        [SerializeField] internal Button closeButton;

        #endregion

        #region Command History

        /// <summary>
        /// Maximum number of commands to store in history.
        /// </summary>
        [Header("Command History")]
        [SerializeField] private int maxHistorySize = 50;
        [SerializeField] private int maxOutputLines = 100;
        private List<string> commandHistory = new List<string>();
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
            if (consolePanel != null)
                consolePanel.SetActive(false);

            // Initialize UI if not set through inspector
            VerifyUI();
        }

        private void OnEnable()
        {
            // Register input actions
            if (toggleConsoleAction != null)
            {
                toggleConsoleAction.action.Enable();
                toggleConsoleAction.action.performed += ToggleConsole;
            }
            
            if (submitCommandAction != null)
            {
                submitCommandAction.action.Enable();
                submitCommandAction.action.performed += SubmitCommandInput;
            }
            
            if (acceptPredictionAction != null)
            {
                acceptPredictionAction.action.Enable();
                acceptPredictionAction.action.performed += AcceptPrediction;
            }
            
            if (previousCommandAction != null)
            {
                previousCommandAction.action.Enable();
                previousCommandAction.action.performed += NavigateHistoryUp;
            }
            
            if (nextCommandAction != null)
            {
                nextCommandAction.action.Enable();
                nextCommandAction.action.performed += NavigateHistoryDown;
            }

            // Set up UI button events
            if (submitButton != null)
                submitButton.onClick.AddListener(SubmitCommand);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseConsole);

            // Set up input field events
            if (commandInputField != null)
            {
                commandInputField.onValueChanged.AddListener(UpdatePredictiveText);
                commandInputField.onSubmit.AddListener(OnInputFieldSubmit);
            }
        }

        private void OnDisable()
        {
            // Unregister input actions
            if (toggleConsoleAction != null)
                toggleConsoleAction.action.performed -= ToggleConsole;
            
            if (submitCommandAction != null)
                submitCommandAction.action.performed -= SubmitCommandInput;
            
            if (acceptPredictionAction != null)
                acceptPredictionAction.action.performed -= AcceptPrediction;
            
            if (previousCommandAction != null)
                previousCommandAction.action.performed -= NavigateHistoryUp;
            
            if (nextCommandAction != null)
                nextCommandAction.action.performed -= NavigateHistoryDown;

            // Remove UI button listeners
            if (submitButton != null)
                submitButton.onClick.RemoveListener(SubmitCommand);
            
            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseConsole);

            // Remove input field listeners
            if (commandInputField != null)
            {
                commandInputField.onValueChanged.RemoveListener(UpdatePredictiveText);
                commandInputField.onSubmit.RemoveListener(OnInputFieldSubmit);
            }
        }

        private void Start()
        {
            // Generate command cache if not already done
            if (!JCCommandConsolePro.Instance.HasGeneratedCache)
            {
                JCCommandConsolePro.Instance?.GenerateCommandCache();
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
            if (consolePanel == null)
            {
                Debug.LogWarning("Console panel not assigned, JCConsoleProUI requires manual UI setup.");
            }

            // Check if essential components are assigned
            if (outputText == null)
            {
                Debug.LogWarning("Output text component not assigned in JCConsoleProUI.");
            }

            if (commandInputField == null)
            {
                Debug.LogWarning("Command input field not assigned in JCConsoleProUI.");
            }

            if (predictiveText == null)
            {
                Debug.LogWarning("Predictive text component not assigned in JCConsoleProUI.");
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
            if (consolePanel == null)
                return;

            consolePanel.SetActive(true);
            
            // Ensure input field is selected and focused
            if (commandInputField != null)
            {
                commandInputField.ActivateInputField();
                commandInputField.Select();
                UpdatePredictiveText(commandInputField.text);
            }

            // Scroll output to bottom
            ScrollOutputToBottom();
        }

        /// <summary>
        /// Closes the console.
        /// </summary>
        public void CloseConsole()
        {
            if (consolePanel == null)
                return;

            consolePanel.SetActive(false);
        }

        /// <summary>
        /// Checks if the console is currently open.
        /// </summary>
        public bool IsConsoleOpen()
        {
            return consolePanel != null && consolePanel.activeSelf;
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
            string result = JCCommandConsolePro.Instance.ExecuteCommand(command);
            
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
            
            // Check if the last token is valid
            //string lastToken = tokens[^1] != "" ? tokens[^1] : tokens[^2];
            bool lastTokenValid = IsValidToken(tokens[^1]);
            string currentToken = tokens[^1];
            
            // Get predictions for the next word
            string[] predictions = JCCommandConsolePro.Instance.PredictNextWords(currentInput);
            
            if (!lastTokenValid && predictions.Length == 0)
            {
                predictiveText.text = commandInputField.text + $"<color=#{ColorUtility.ToHtmlStringRGB(errorColor)}>     Invalid command...</color>";
                return;
            }

            if (predictions.Length > 0)
            {
                string predictionColorHex = ColorUtility.ToHtmlStringRGB(predictiveColor);
                
                // For the third token (command name), show type information
                if (tokens.Count == 3 && JCCommandConsolePro.consolecmds.Contains(tokens[1].ToLower()))
                {
                    string commandName = predictions[0].ToLower();
                    string typeInfo = JCCommandConsolePro.Instance.GetCommandTypeInfo(commandName);
                    
                    if (!string.IsNullOrEmpty(typeInfo))
                    {
                        predictiveText.text = commandInputField.text + $"{commandName[currentToken.Length..]} <color=#{predictionColorHex}>{typeInfo}</color>";
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
                    
                    // // Show additional predictions if available
                    // if (predictions.Length > 1)
                    // {
                    //     predictiveText.text += $"\n<color=#{predictionColorHex}>(+ {predictions.Length - 1} more options)</color>";
                    // }
                }
                // else
                // {
                //     // Show the list of predictions
                //     predictiveText.text = string.Join("\n", predictions.Take(5)
                //         .Select(p => $"<color=#{predictionColorHex}>{p}</color>"));
                //     
                //     if (predictions.Length > 5)
                //     {
                //         predictiveText.text += $"\n<color=#{predictionColorHex}>(+ {predictions.Length - 5} more options)</color>";
                //     }
                // }
            }
            else
            {
                // Check if we're handling a method call with parameters
                if (tokens.Count >= 4 && tokens[1].ToLower() == "call")
                {
                    string commandName = tokens[2].ToLower();
                    
                    if (JCCommandConsolePro.Instance.GetCommandTypeInfo(commandName) != null)
                    {
                        string typeInfo = JCCommandConsolePro.Instance.GetCommandTypeInfo(commandName);
                        string predictionColorHex = ColorUtility.ToHtmlStringRGB(predictiveColor);
                        predictiveText.text = commandInputField.text + $"{commandName[currentToken.Length..]}<color=#{predictionColorHex}>{typeInfo}</color>";
                        return;
                    }
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
            // if (string.IsNullOrEmpty(token))
            //     return false;
                
            // Check if it's a preface command
            foreach (var preface in JCCommandConsolePro.TargetCmds)
            {
                if (token.StartsWith(preface))
                    return true;
            }
            
            // Check if it's a console command
            if (JCCommandConsolePro.consolecmds.Contains(token.ToLower()))
                return true;
            
            // Check if it's "select"
            if (token.ToLower() == "select")
                return true;
            
            // Check if it's a command name
            var allCommands = JCCommandConsolePro.Instance.GetAllCommands();
            if (allCommands.Contains(token.ToLower()))
                return true;
            
            return false;
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
            if (commandInputField == null || !IsConsoleOpen())
                return;

            string[] predictions = JCCommandConsolePro.Instance.PredictNextWords(commandInputField.text);
            if (predictions.Length > 0)
            {
                string[] tokens = commandInputField.text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string updatedText;
                
                // Check if we're completing a token or adding a new one
                if (tokens.Length > 0 && predictions[0].StartsWith(tokens[tokens.Length - 1], StringComparison.OrdinalIgnoreCase))
                {
                    // Replace the current token with the prediction
                    tokens[tokens.Length - 1] = predictions[0];
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

            if (logToConsole && outputText != null)
                LogToOutput($"> {command}", commandColor);

            string result = JCCommandConsolePro.Instance.ExecuteCommand(command);
            
            if (logToConsole && outputText != null)
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
            if (outputText == null)
                return;

            LogToOutput(message, isError ? errorColor : resultColor);
        }

        #endregion
    }
}