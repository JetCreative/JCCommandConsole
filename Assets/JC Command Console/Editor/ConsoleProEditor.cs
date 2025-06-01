using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Editor window for configuring and managing the Command Console Pro system.
    /// </summary>
    public class ConsoleProEditor : EditorWindow
    {
        #region Editor Window Settings

        private bool includePrivateMembers => cache.IncludePrivateMembers;
        private bool includeExampleCommands => cache.IncludeExampleCommands;
        private List<string> includeNamespaces => cache.IncludeNamespaces;
        private List<string> excludeNamespaces => cache.ExcludeNamespaces;
        
        private string newNamespace = "";
        private bool showIncludeNamespaces => cache.ShowIncludeNamespaces;
        private bool showExcludeNamespaces => cache.ShowExcludeNamespaces;

        private static CommandCache _cache;

        private CommandCache cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = JCCommandConsolePro.GetCommandCache();
                }
                return _cache;
            }
        }
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
            
            _cache = JCCommandConsolePro.GetCommandCache();
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
            
            cache.IncludePrivateMembers = EditorGUILayout.Toggle("Include Private Members", includePrivateMembers);
            cache.IncludeExampleCommands = EditorGUILayout.Toggle("Include Example Commands", includeExampleCommands);
            
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
            cache.ShowIncludeNamespaces = EditorGUILayout.Foldout(showIncludeNamespaces, "Include Namespaces");
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
            cache.ShowExcludeNamespaces = EditorGUILayout.Foldout(showExcludeNamespaces, "Exclude Namespaces");
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
                var consoleUI = Object.FindFirstObjectByType<JCConsoleProUI>();
                if (consoleUI != null)
                {
                    Selection.activeGameObject = consoleUI.gameObject;
                    EditorGUIUtility.PingObject(consoleUI.gameObject);
                }
                else
                {
                    Debug.LogWarning("No Console UI found in scene. Please add and check out the readme for more info.");
                }
            }
            EditorGUILayout.EndVertical();
        }

        #endregion
        
    }
}