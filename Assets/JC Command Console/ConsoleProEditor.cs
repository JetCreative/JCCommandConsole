using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Editor window for configuring and managing the Command Console Pro system.
    /// </summary>
    public class ConsoleProEditor : EditorWindow
    {
        #region Editor Window Settings

        private bool includePrivateMembers = false;
        private bool includeExampleCommands = true;
        private List<string> includeNamespaces = new List<string> { "JetCreative" };
        private List<string> excludeNamespaces = new List<string> { "UnityEngine.Internal", "System.Runtime" };
        private string newNamespace = "";
        private bool showIncludeNamespaces = true;
        private bool showExcludeNamespaces = true;

        #endregion

        #region Menu Items

        /// <summary>
        /// Opens the Console Pro Editor window from the menu.
        /// </summary>
        [MenuItem("Tools/Jet Creative/Command Console")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConsoleProEditor>("Command Console Pro");
            window.minSize = new Vector2(400, 300);
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            GUILayout.Label("Command Console Pro Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawGenerateCacheSection();
            EditorGUILayout.Space();

            DrawCommandFilteringSection();
            EditorGUILayout.Space();

            DrawNamespaceFilteringSection();
            EditorGUILayout.Space();

            DrawInputActionSection();
        }

        /// <summary>
        /// Draws the section for generating the command cache.
        /// </summary>
        private void DrawGenerateCacheSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Command Cache", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Command Cache", GUILayout.Height(30)))
            {
                JCCommandConsolePro console = JCCommandConsolePro.Instance;
                
                int commandCount = console.GenerateCommandCache(
                    includePrivateMembers,
                    includeExampleCommands,
                    includeNamespaces.ToArray(),
                    excludeNamespaces.ToArray()
                );
                
                EditorUtility.DisplayDialog(
                    "Command Cache Generated",
                    $"Successfully registered {commandCount} commands.",
                    "OK"
                );
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.HelpBox(
                "Click to scan all assemblies for [Command] attributes and build the command cache. " +
                "This needs to be done after making changes to commands or after recompiling scripts.",
                MessageType.Info
            );
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the section for command filtering options.
        /// </summary>
        private void DrawCommandFilteringSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Command Filtering", EditorStyles.boldLabel);
            
            includePrivateMembers = EditorGUILayout.Toggle("Include Private Members", includePrivateMembers);
            includeExampleCommands = EditorGUILayout.Toggle("Include Example Commands", includeExampleCommands);
            
            EditorGUILayout.HelpBox(
                "Private members are only included if you specifically enable them here. " +
                "Example commands refer to those in the ConsoleExampleCommands class.",
                MessageType.Info
            );
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the section for namespace filtering options.
        /// </summary>
        private void DrawNamespaceFilteringSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Namespace Filtering", EditorStyles.boldLabel);
            
            // Include Namespaces
            showIncludeNamespaces = EditorGUILayout.Foldout(showIncludeNamespaces, "Include Namespaces");
            if (showIncludeNamespaces)
            {
                EditorGUILayout.HelpBox(
                    "If any namespaces are listed here, only commands in these namespaces will be included. " +
                    "Leave empty to include all namespaces.",
                    MessageType.Info
                );
                
                EditorGUI.indentLevel++;
                
                // Show existing include namespaces
                for (int i = 0; i < includeNamespaces.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    includeNamespaces[i] = EditorGUILayout.TextField(includeNamespaces[i]);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        includeNamespaces.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // Add new namespace
                EditorGUILayout.BeginHorizontal();
                newNamespace = EditorGUILayout.TextField("Add Namespace", newNamespace);
                if (GUILayout.Button("+", GUILayout.Width(20)) && !string.IsNullOrWhiteSpace(newNamespace))
                {
                    includeNamespaces.Add(newNamespace);
                    newNamespace = "";
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Exclude Namespaces
            showExcludeNamespaces = EditorGUILayout.Foldout(showExcludeNamespaces, "Exclude Namespaces");
            if (showExcludeNamespaces)
            {
                EditorGUILayout.HelpBox(
                    "Commands in these namespaces will be excluded from the console.",
                    MessageType.Info
                );
                
                EditorGUI.indentLevel++;
                
                // Show existing exclude namespaces
                for (int i = 0; i < excludeNamespaces.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    excludeNamespaces[i] = EditorGUILayout.TextField(excludeNamespaces[i]);
                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        excludeNamespaces.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                // Add new namespace
                EditorGUILayout.BeginHorizontal();
                newNamespace = EditorGUILayout.TextField("Add Namespace", newNamespace);
                if (GUILayout.Button("+", GUILayout.Width(20)) && !string.IsNullOrWhiteSpace(newNamespace))
                {
                    excludeNamespaces.Add(newNamespace);
                    newNamespace = "";
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the section for configuring input actions.
        /// </summary>
        private void DrawInputActionSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Console Input Setup", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "To set up console input controls, select a JCConsoleProUI GameObject in your scene " +
                "and configure the Input Actions in the Inspector.",
                MessageType.Info
            );
            
            if (GUILayout.Button("Find/Select Console UI in Scene"))
            {
                var consoleUI = Object.FindObjectOfType<JCConsoleProUI>();
                if (consoleUI != null)
                {
                    Selection.activeGameObject = consoleUI.gameObject;
                    EditorGUIUtility.PingObject(consoleUI.gameObject);
                }
                else
                {
                    // Show popup to create a new console UI
                    CreateConsoleUIPopup.ShowWindow();
                }
            }
            
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region ConsoleUI Creation Popup

        /// <summary>
        /// Popup window for creating a new JCConsoleProUI in the scene.
        /// </summary>
        private class CreateConsoleUIPopup : EditorWindow
        {
            private bool createCanvas = true;
            private Canvas existingCanvas;

            public static void ShowWindow()
            {
                var window = GetWindow<CreateConsoleUIPopup>("Create Console UI");
                window.minSize = new Vector2(400, 200);
                window.maxSize = new Vector2(400, 200);
                window.ShowUtility();
            }

            private void OnGUI()
            {
                EditorGUILayout.LabelField("Create JCConsoleProUI", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                EditorGUILayout.HelpBox(
                    "No JCConsoleProUI found in the scene. Create one now?",
                    MessageType.Info
                );

                EditorGUILayout.Space();

                createCanvas = EditorGUILayout.Toggle("Create new Canvas", createCanvas);

                if (!createCanvas)
                {
                    existingCanvas = (Canvas)EditorGUILayout.ObjectField(
                        "Existing Canvas", 
                        existingCanvas, 
                        typeof(Canvas), 
                        true
                    );
                }

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Cancel"))
                {
                    Close();
                }

                GUI.enabled = createCanvas || existingCanvas != null;
                if (GUILayout.Button("Create"))
                {
                    CreateConsoleUI();
                    Close();
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }

            private void CreateConsoleUI()
            {
                Canvas canvas;
                GameObject consoleRoot;

                // Create or use canvas
                if (createCanvas)
                {
                    // Create new canvas with proper settings
                    GameObject canvasObj = new GameObject("CommandConsoleCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    
                    // Add CanvasScaler for resolution independence
                    CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f; // Balance width/height scaling
                    
                    // Add GraphicRaycaster for UI interactions
                    canvasObj.AddComponent<GraphicRaycaster>();
                    
                    // Create EventSystem if it doesn't exist
                    if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                    {
                        GameObject eventSystem = new GameObject("EventSystem");
                        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    }
                    
                    // Create console root object
                    consoleRoot = new GameObject("CommandConsoleUI");
                    consoleRoot.transform.SetParent(canvas.transform, false);
                }
                else
                {
                    // Use existing canvas
                    canvas = existingCanvas;
                    
                    // Create console root object
                    consoleRoot = new GameObject("CommandConsoleUI");
                    consoleRoot.transform.SetParent(canvas.transform, false);
                }

                // Add JCConsoleProUI component
                JCConsoleProUI consoleUI = consoleRoot.AddComponent<JCConsoleProUI>();
                
                // Create main console panel
                GameObject consolePanel = CreatePanel("ConsolePanel", consoleRoot.transform);
                RectTransform consolePanelRT = consolePanel.GetComponent<RectTransform>();
                
                // Set console panel to middle third of the screen
                consolePanelRT.anchorMin = new Vector2(1/3f, 0.25f);
                consolePanelRT.anchorMax = new Vector2(2/3f, 0.75f);
                consolePanelRT.offsetMin = Vector2.zero;
                consolePanelRT.offsetMax = Vector2.zero;
                
                // Background image
                Image panelImage = consolePanel.GetComponent<Image>();
                panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
                
                // Create exit button in top-right corner
                GameObject exitButton = CreateButton("ExitButton", consolePanel.transform, "X");
                RectTransform exitButtonRT = exitButton.GetComponent<RectTransform>();
                exitButtonRT.anchorMin = new Vector2(1, 1);
                exitButtonRT.anchorMax = new Vector2(1, 1);
                exitButtonRT.pivot = new Vector2(1, 1);
                exitButtonRT.sizeDelta = new Vector2(40, 40);
                exitButtonRT.anchoredPosition = new Vector2(-10, -10);
                
                // Create input area at bottom
                GameObject inputArea = CreatePanel("InputArea", consolePanel.transform);
                RectTransform inputAreaRT = inputArea.GetComponent<RectTransform>();
                inputAreaRT.anchorMin = new Vector2(0, 0);
                inputAreaRT.anchorMax = new Vector2(1, 0);
                inputAreaRT.pivot = new Vector2(0.5f, 0);
                inputAreaRT.sizeDelta = new Vector2(0, 60);
                inputAreaRT.anchoredPosition = new Vector2(0, 10);
                inputArea.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
                
                // Create input field
                GameObject inputField = CreateInputField("InputField", inputArea.transform, "Enter command...");
                RectTransform inputFieldRT = inputField.GetComponent<RectTransform>();
                inputFieldRT.anchorMin = new Vector2(0, 0);
                inputFieldRT.anchorMax = new Vector2(1, 1);
                inputFieldRT.offsetMin = new Vector2(10, 10);
                inputFieldRT.offsetMax = new Vector2(-110, -10);
                
                // Create predictive text overlay
                GameObject predictiveText = CreateText("PredictiveText", inputArea.transform, "");
                RectTransform predictiveTextRT = predictiveText.GetComponent<RectTransform>();
                predictiveTextRT.anchorMin = new Vector2(0, 0);
                predictiveTextRT.anchorMax = new Vector2(1, 1);
                predictiveTextRT.offsetMin = new Vector2(10, 10);
                predictiveTextRT.offsetMax = new Vector2(-110, -10);
                predictiveText.GetComponent<TMP_Text>().color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
                
                // Create submit button
                GameObject submitButton = CreateButton("SubmitButton", inputArea.transform, "Enter");
                RectTransform submitButtonRT = submitButton.GetComponent<RectTransform>();
                submitButtonRT.anchorMin = new Vector2(1, 0);
                submitButtonRT.anchorMax = new Vector2(1, 1);
                submitButtonRT.pivot = new Vector2(1, 0.5f);
                submitButtonRT.sizeDelta = new Vector2(100, 0);
                submitButtonRT.anchoredPosition = new Vector2(-10, 0);
                
                // Create output area for logs
                GameObject outputContainer = CreatePanel("OutputContainer", consolePanel.transform);
                RectTransform outputContainerRT = outputContainer.GetComponent<RectTransform>();
                outputContainerRT.anchorMin = new Vector2(0, 0);
                outputContainerRT.anchorMax = new Vector2(1, 1);
                outputContainerRT.offsetMin = new Vector2(10, 70);
                outputContainerRT.offsetMax = new Vector2(-10, -50);
                outputContainer.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
                
                // Add scroll view to output container
                ScrollRect scrollRect = outputContainer.AddComponent<ScrollRect>();
                
                // Create viewport for scrolling
                GameObject viewport = CreatePanel("Viewport", outputContainer.transform);
                RectTransform viewportRT = viewport.GetComponent<RectTransform>();
                viewportRT.anchorMin = Vector2.zero;
                viewportRT.anchorMax = Vector2.one;
                viewportRT.offsetMin = Vector2.zero;
                viewportRT.offsetMax = Vector2.zero;
                viewport.GetComponent<Image>().color = Color.clear;
                viewport.AddComponent<Mask>().showMaskGraphic = false;
                
                // Create content for scroll view
                GameObject content = CreatePanel("Content", viewport.transform);
                RectTransform contentRT = content.GetComponent<RectTransform>();
                contentRT.anchorMin = new Vector2(0, 1);
                contentRT.anchorMax = new Vector2(1, 1);
                contentRT.pivot = new Vector2(0.5f, 1);
                contentRT.sizeDelta = new Vector2(0, 0);
                content.GetComponent<Image>().color = Color.clear;
                ContentSizeFitter sizeFitter = content.AddComponent<ContentSizeFitter>();
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                // Create output text
                GameObject outputText = CreateText("OutputText", content.transform, "");
                RectTransform outputTextRT = outputText.GetComponent<RectTransform>();
                outputTextRT.anchorMin = Vector2.zero;
                outputTextRT.anchorMax = Vector2.one;
                outputTextRT.offsetMin = new Vector2(5, 5);
                outputTextRT.offsetMax = new Vector2(-5, -5);
                TMP_Text tmpText = outputText.GetComponent<TMP_Text>();
                tmpText.alignment = TextAlignmentOptions.TopLeft;
                tmpText.fontSize = 16;
                tmpText.enableWordWrapping = true;
                
                // Setup scroll rect references
                scrollRect.content = contentRT;
                scrollRect.viewport = viewportRT;
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.scrollSensitivity = 20;
                
                // Hook up references to the console UI component
                consoleUI.consolePanel = consolePanel;
                consoleUI.outputScrollRect = scrollRect;
                consoleUI.outputText = tmpText;
                consoleUI.commandInputField = inputField.GetComponent<TMP_InputField>();
                consoleUI.predictiveText = predictiveText.GetComponent<TMP_Text>();
                consoleUI.submitButton = submitButton.GetComponent<Button>();
                consoleUI.closeButton = exitButton.GetComponent<Button>();
                
                // Initially disable the console panel
                consolePanel.SetActive(false);
                
                // Select the new console UI in the hierarchy
                Selection.activeGameObject = consoleRoot;
                EditorGUIUtility.PingObject(consoleRoot);
                
                // Log success
                Debug.Log("JCConsoleProUI created successfully. Configure Input Actions in the Inspector.");
            }

            // Helper methods for UI creation
            private GameObject CreatePanel(string name, Transform parent)
            {
                GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(parent, false);
                return panel;
            }

            private GameObject CreateText(string name, Transform parent, string text)
            {
                GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshPro));
                textObj.transform.SetParent(parent, false);
                textObj.GetComponent<TMP_Text>().text = text;
                return textObj;
            }

            private GameObject CreateButton(string name, Transform parent, string text)
            {
                GameObject buttonObj = CreatePanel(name, parent);
                Button button = buttonObj.AddComponent<Button>();
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = buttonObj.GetComponent<Image>();
                
                // Add text child
                GameObject textObj = CreateText(name + "Text", buttonObj.transform, text);
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
                textObj.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Center;
                
                return buttonObj;
            }

            private GameObject CreateInputField(string name, Transform parent, string placeholder)
            {
                GameObject inputObj = CreatePanel(name, parent);
                inputObj.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
                
                // Create text component
                GameObject textObj = CreateText("Text", inputObj.transform, "");
                textObj.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Left;
                textObj.GetComponent<RectTransform>().offsetMin = new Vector2(5, 5);
                textObj.GetComponent<RectTransform>().offsetMax = new Vector2(-5, -5);
                
                // Create placeholder
                GameObject placeholderObj = CreateText("Placeholder", inputObj.transform, placeholder);
                placeholderObj.GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Left;
                placeholderObj.GetComponent<TMP_Text>().color = new Color(0.5f, 0.5f, 0.5f);
                placeholderObj.GetComponent<RectTransform>().offsetMin = new Vector2(5, 5);
                placeholderObj.GetComponent<RectTransform>().offsetMax = new Vector2(-5, -5);
                
                // Add input field component
                TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
                inputField.textViewport = inputObj.GetComponent<RectTransform>();
                inputField.textComponent = textObj.GetComponent<TMP_Text>();
                inputField.placeholder = placeholderObj.GetComponent<TMP_Text>();
                
                return inputObj;
            }
        }

        #endregion
    }
}