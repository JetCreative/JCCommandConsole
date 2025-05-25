using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace JetCreative.Console
{
    /// <summary>
    /// JCCommandConsole is a central class responsible for managing and executing console commands in game.
    /// It provides functionalities for registering and executing commands,
    /// enabling or disabling the console UI, and integrating commands decorated with the <c>Command</c> attribute.
    /// </summary>
    public class JCCommandConsole : MonoBehaviour
    {
        /// <summary>
        /// The singleton instance of the <see cref="JCCommandConsole"/> class, ensuring that only one instance is active at any time.
        /// This field is used internally to store a reference to the active <see cref="JCCommandConsole"/> instance.
        /// </summary>
        private static JCCommandConsole instance;

        /// <summary>
        /// A dictionary that maps command names to their corresponding method information.
        /// This variable is used to register and store all available console commands.
        /// Commands can be dynamically added by associating a command name, typically
        /// defined in the custom <see cref="CommandAttribute"/>, with a method.
        /// </summary>
        private Dictionary<string, MethodInfo> commands = new Dictionary<string, MethodInfo>();
       
        /// <summary>
        /// Gets the singleton instance of the <see cref="JCCommandConsole"/> class.
        /// If an instance does not already exist, a new instance is created and persisted across scenes.
        /// </summary>
        /// <value>
        /// The singleton instance of <see cref="JCCommandConsole"/>.
        /// </value>
        public static JCCommandConsole Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("CommandConsole");
                    instance = go.AddComponent<JCCommandConsole>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        /// <summary>
        /// Called when the script instance is being loaded. This method ensures that only one instance of the
        /// JCCommandConsole exists, initializes it, and makes it persistent across scenes. If another instance
        /// is found, the duplicate is destroyed. Additionally, this method registers all available commands
        /// by inspecting the available assemblies.
        /// </summary>
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                RegisterCommands();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Registers available commands into the console system by scanning all loaded assemblies
        /// for methods decorated with the <see cref="CommandAttribute"/>. Commands are stored
        /// in a dictionary, mapping their command names to the associated method information.
        /// </summary>
        /// <remarks>
        /// The method parses all types and methods within the currently loaded assemblies,
        /// identifies those with the <see cref="CommandAttribute"/>, and stores them with
        /// their associated names in the internal command dictionary. If a command name is
        /// provided explicitly in the attribute, it is used; otherwise, the method name
        /// (converted to lowercase) is utilized as the command name.
        /// </remarks>
        private void RegisterCommands()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | 
                                                BindingFlags.Static | BindingFlags.Instance);
                    
                    foreach (var method in methods)
                    {
                        var commandAttribute = method.GetCustomAttribute<CommandAttribute>();
                        if (commandAttribute != null)
                        {
                            string commandName = commandAttribute.CommandName ?? method.Name.ToLower();
                            commands[commandName] = method;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Executes a given command string by finding a corresponding method and invoking it with the provided parameters.
        /// </summary>
        /// <param name="commandInput">The input command string, including the command name and any required parameters.</param>
        public void ExecuteCommand(string commandInput)
        {
            string[] parts = commandInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string commandName = parts[0].ToLower();
            if (!commands.TryGetValue(commandName, out MethodInfo methodInfo))
            {
                ConsoleUI.Instance.LogError($"Command not found: {commandName}");
                return;
            }

            try
            {
                var parameters = methodInfo.GetParameters();
                object[] paramValues = new object[parameters.Length];
                
                if (parts.Length - 1 != parameters.Length)
                {
                    ConsoleUI.Instance.LogError($"Invalid number of parameters. Expected {parameters.Length}, got {parts.Length - 1}");
                    return;
                }

                for (int i = 0; i < parameters.Length; i++)
                {
                    paramValues[i] = Convert.ChangeType(parts[i + 1], parameters[i].ParameterType);
                }

                object result = null;
                if (methodInfo.IsStatic)
                {
                    result = methodInfo.Invoke(null, paramValues);
                }
                else
                {
                    var instances = FindObjectsOfType(methodInfo.DeclaringType);
                    if (instances.Length > 0)
                    {
                        result = methodInfo.Invoke(instances[0], paramValues);
                    }
                    else
                    {
                        ConsoleUI.Instance.LogError($"No instance of {methodInfo.DeclaringType.Name} found in scene");
                        return;
                    }
                }

                if (result != null)
                {
                    ConsoleUI.Instance.Log(result.ToString());
                }
            }
            catch (Exception e)
            {
                ConsoleUI.Instance.LogError($"Error executing command: {e.Message}");
            }
        }

        /// <summary>
        /// Toggles the console interface's visibility.
        /// </summary>
        /// <param name="enabled">A boolean value that determines whether the console should be enabled (true) or disabled (false).</param>
        public void EnableConsole(bool enabled)
        {
            ConsoleUI.Instance.gameObject.SetActive(enabled);
        }

        /// <summary>
        /// A test command that logs a message to the Unity debug console.
        /// </summary>
        /// <remarks>
        /// This is a static method decorated with the Command attribute.
        /// It can be called in the custom command console to ensure the system is working correctly.
        /// </remarks>
        [Command]
        public static void Test()
        {
            Debug.Log("console test");
        }

        /// Checks if the Console UI is currently active.
        /// <returns>
        /// Returns true if the Console UI is active, otherwise false.
        /// </returns>
        [Command]
        public bool CheckConsole()
        {
            return ConsoleUI.Instance.gameObject.activeSelf;
        }

        /// Adds two numbers and returns the result.
        /// <param name="a">The first number to be added.</param>
        /// <param name="b">The second number to be added.</param>
        /// <return>The sum of the two float parameters.</return>
        [Command("addition")]
        public float AdditionTime(float a, float b) => a + b;
    }
}