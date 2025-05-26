using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        /// Add these new fields to the ConsoleUI class
        private string currentPrediction = "";
        [SerializeField] private TMP_Text predictionOverlay;
        private Color predictionColor = new Color(0.7f, 0.7f, 0.7f, 0.5f); // Light gray, semi-transparent

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
            
            // Match prediction overlay's settings to input field
            // GameObject predictionObj = new GameObject("PredictionOverlay");
            // predictionObj.transform.SetParent(inputField.transform, false);
            // predictionOverlay = predictionObj.AddComponent<TMP_Text>();
            predictionOverlay.font = inputField.textComponent.font;
            predictionOverlay.fontSize = inputField.textComponent.fontSize;
            predictionOverlay.color = predictionColor;
            predictionOverlay.alignment = inputField.textComponent.alignment;
            
            // Match the position and size with input field's text component
            RectTransform predRect = predictionOverlay.GetComponent<RectTransform>();
            RectTransform inputRect = inputField.textComponent.GetComponent<RectTransform>();
            predRect.anchorMin = inputRect.anchorMin;
            predRect.anchorMax = inputRect.anchorMax;
            predRect.offsetMin = inputRect.offsetMin;
            predRect.offsetMax = inputRect.offsetMax;
            
            // Add input change listener
            inputField.onValueChanged.AddListener(OnInputValueChanged);
        }

        private void OnInputValueChanged(string text)
        {
            // Initialize empty prediction
            string prediction = "";
            
            // Split input into words split by a space
            List<string> words = text.Split(new[] { ' ' }, StringSplitOptions.None).ToList();;

            RemoveConsecutiveEmptyEntries(words);
            
            if (words.Count == 0 || (words.Count == 1 && string.IsNullOrEmpty(words[0])))
            {
                predictionOverlay.text = "";
                return;
            }
            
            
            //check for invalid words
            bool isError = false;
            //List<string> invalidWords = new List<string>();
            //count -1 to skip current word
            for (int i = 0; i < words.Count - 1 ; i++)
            {
                string word = words[i]; 
                string prevWord = i > 0 ? words[i - 1] : "";
                
                bool IsPrefaceCmd(string word) => JCCommandConsole.Instance.prefaceConsoleCommands.Any(word.StartsWith);
                bool IsPropCmd(string word) => JCCommandConsole.Instance.propertyConsoleCommands.Contains(word);
                bool IsMethod(string word) => JCCommandConsole.Instance.methodCommands.ContainsKey(word);
                bool IsProperty(string word) => JCCommandConsole.Instance.properties.ContainsKey(word);
                bool IsField(string word) => JCCommandConsole.Instance.fields.ContainsKey(word);
                bool IsParameter()
                {
                    // Find the last method in previous words
                    var lastMethodWord = words.Take(i).LastOrDefault(w => IsMethod(w));
                    if (lastMethodWord != null)
                    {
                        var methodInfo = JCCommandConsole.Instance.methodCommands[lastMethodWord];
                        var parameters = methodInfo.GetParameters();
                        // Calculate parameter position (skip method name and count from there)
                        int paramPosition = i - words.IndexOf(lastMethodWord) - 1;
                        return paramPosition >= 0 && paramPosition < parameters.Length;
                    }
                
                    // Check if it's a property/field parameter
                    if (i >= 2 && words[i-1] != null)
                    {
                        var twoWordsAgo = words[i - 2];
                        var prevWord = words[i - 1];
                        if (twoWordsAgo == "set" && (IsProperty(prevWord) || IsField(prevWord)))
                        {
                            return true;
                        }
                    }
                
                    return false;
                }

                bool wordIsValid =
                    //preface symbols are only valid on first word
                    (IsPrefaceCmd(word) && i == 0)
                    //Prop cmds are invalid after methods or properties or fields
                    || (IsPropCmd(word) && !words.Take(i).Any(w => IsMethod(w) || IsProperty(w) || IsField(w) || IsPropCmd(w)))
                    //Methods are invalid after prop cmds, fields, and properties
                    || (IsMethod(word) && !words.Take(i).Any(w => IsMethod(w) || IsProperty(w) || IsField(w) || IsPropCmd(w)))
                    //Properties and fields are invalid if not after a property cmd
                    || (IsProperty(word) && IsPropCmd(prevWord))
                    || (IsField(word) && IsPropCmd(prevWord))
                    //check if any previous word is method or property or field
                    || IsParameter();

                if (!wordIsValid)
                {
                    isError = true;
                    //invalidWords.Add(word);
                    break;
                }

            }

            string currentWord = words[words.Count - 1].ToLower();
            string previousWord = words.Count > 1 ? words[words.Count - 2].ToLower() : "";

            //Debug.Log("Previous word " + previousWord + " Current word " + currentWord + " Words count " + words.Count);

            // Check if the current word starts with any preface command
            bool startsWithPreface = JCCommandConsole.Instance.prefaceConsoleCommands
                .Any(preface => currentWord.StartsWith(preface));
            //if so then predict nothing
            if (startsWithPreface)
            {
                predictionOverlay.text = "";
                return;
            }
            
            bool lastWasPreface = JCCommandConsole.Instance.prefaceConsoleCommands
                .Any(preface => previousWord.StartsWith(preface));

            //This is first word or previous word starts with a preface command
            if (lastWasPreface || string.IsNullOrEmpty(previousWord))
            {
                // Predict method names from methodCommands
                var methodPredictions = JCCommandConsole.Instance.methodCommands.Keys
                    .Where(cmd => cmd.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(cmd => cmd)
                    .FirstOrDefault();
                    
                if (methodPredictions != null)
                {
                    prediction = text + methodPredictions.Substring(currentWord.Length);
                }
                else
                {
                    // If no method predictions, try property console commands
                    var propCommandPrediction = JCCommandConsole.Instance.propertyConsoleCommands
                        .Where(cmd => cmd.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(cmd => cmd)
                        .FirstOrDefault();
                        
                    if (propCommandPrediction != null)
                    {
                        prediction = text + propCommandPrediction.Substring(currentWord.Length);
                    }
                }
            }
            //following a method
            else if (JCCommandConsole.Instance.methodCommands.ContainsKey(previousWord))
            {
                // Show parameter info for the method
                var methodInfo = JCCommandConsole.Instance.methodCommands[previousWord];
                var parameters = methodInfo.GetParameters();
                if (parameters.Length > 0)
                {
                    int currentParamIndex = words.Count - 2; // -2 because we exclude method name and start from 0
                    if (currentParamIndex < parameters.Length)
                    {
                        prediction = $"{text} [{parameters[currentParamIndex].ParameterType.Name} {parameters[currentParamIndex].Name}]";
                    }
                }
            }
            //following a property or field console command e.g. "get" or "set"
            else if (JCCommandConsole.Instance.propertyConsoleCommands.Contains(previousWord))
            {
                // Predict property or field names
                var propertyPrediction = JCCommandConsole.Instance.properties.Keys
                    .Where(prop => prop.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(prop => prop)
                    .FirstOrDefault();
                    
                if (propertyPrediction != null)
                {
                    prediction = text + propertyPrediction.Substring(currentWord.Length);
                }
                else
                {
                    var fieldPrediction = JCCommandConsole.Instance.fields.Keys
                        .Where(field => field.StartsWith(currentWord, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(field => field)
                        .FirstOrDefault();
                        
                    if (fieldPrediction != null)
                    {
                        prediction = text + fieldPrediction.Substring(currentWord.Length);
                    }
                }
            }
            else if (previousWord == "set")
            {
                // Show type for property or field if found
                if (JCCommandConsole.Instance.properties.TryGetValue(currentWord, out PropertyInfo propInfo))
                {
                    prediction = $"{text} [{propInfo.PropertyType.Name}]";
                }
                else if (JCCommandConsole.Instance.fields.TryGetValue(currentWord, out FieldInfo fieldInfo))
                {
                    prediction = $"{text} [{fieldInfo.FieldType.Name}]";
                }
            }
            
            //if error then add message at end of prediction
            if (isError)
            {
                prediction = prediction.Length > 0 
                    ? prediction + " <color=red>     Invalid command.....</color>" 
                    : text + " <color=red>     Invalid command.....</color>";
            }

            predictionOverlay.text = prediction;

            //Debug.Log(prediction.Length > 0 ? $"Prediction: {prediction}" : "No prediction");
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
        public void OnSubmitCommand()
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

        
        public void AcceptPrediction()
        {
            if (string.IsNullOrEmpty(predictionOverlay.text)) return;

            inputField.text = predictionOverlay.text + " ";
            inputField.caretPosition = inputField.text.Length;
            predictionOverlay.text = "";
        }

        /// <summary>
        /// Removes consecutive empty entries from the word list to include spaces
        /// </summary>
        /// <param name="words">List of words to clean up</param>
        private void RemoveConsecutiveEmptyEntries(List<string> words)
        {
            for (int currentIndex = 0; currentIndex < words.Count; currentIndex++)
            {
                if (!string.IsNullOrEmpty(words[currentIndex])) 
                    continue;
            
                while (currentIndex + 1 < words.Count && string.IsNullOrEmpty(words[currentIndex + 1]))
                {
                    words.RemoveAt(currentIndex);
                }
            }
        }
    }
}