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
        private static JCCommandConsole instance;

        private Dictionary<string, MethodInfo> commands = new Dictionary<string, MethodInfo>();
        private Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();
        private Dictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>();
        //private Dictionary<string, EventInfo> events = new Dictionary<string, EventInfo>();

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
            
            OnTestFloatChanged = (value) => Debug.Log($"TestFloat changed to {value}");
            TestAction = () => Debug.Log("TestAction invoked");
            
        }

        private void RegisterCommands()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // Register methods
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

                    // Register properties
                    var properties = type.GetProperties(BindingFlags.Public | 
                                                  BindingFlags.Static | BindingFlags.Instance);
                    
                    foreach (var property in properties)
                    {
                        var commandAttribute = property.GetCustomAttribute<CommandAttribute>();
                        if (commandAttribute != null)
                        {
                            string propertyName = commandAttribute.CommandName ?? property.Name.ToLower();
                            this.properties[propertyName] = property;
                        }
                    }

                    // Register fields
                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                              BindingFlags.Static | BindingFlags.Instance);

                    foreach (var field in fields)
                    {
                        var commandAttribute = field.GetCustomAttribute<CommandAttribute>();
                        if (commandAttribute != null)
                        {
                            string fieldName = commandAttribute.CommandName ?? field.Name.ToLower();
                            this.fields[fieldName] = field;
                        }
                    }

                    // Register events
                    // var events = type.GetEvents(BindingFlags.Public | BindingFlags.NonPublic |
                    //                           BindingFlags.Static | BindingFlags.Instance);
                    //
                    // foreach (var eventInfo in events)
                    // {
                    //     var commandAttribute = eventInfo.GetCustomAttribute<CommandAttribute>();
                    //     if (commandAttribute != null)
                    //     {
                    //         string eventName = commandAttribute.CommandName ?? eventInfo.Name.ToLower();
                    //         this.events[eventName] = eventInfo;
                    //     }
                    // }
                }
            }
        }

        public void ExecuteCommand(string commandInput)
        {
            string[] parts = commandInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            string commandName = parts[0].ToLower();

            // Check if it's a property setting command
            if (commandName == "set" && parts.Length == 3)
            {
                string targetName = parts[1].ToLower();
                
                // Try set property
                if (properties.TryGetValue(targetName, out PropertyInfo propertyInfo))
                {
                    try
                    {
                        object target = null;
                        if (!propertyInfo.GetMethod.IsStatic)
                        {
                            var instances = FindObjectsOfType(propertyInfo.DeclaringType);
                            if (instances.Length == 0)
                            {
                                ConsoleUI.Instance.LogError($"No instance of {propertyInfo.DeclaringType.Name} found in scene");
                                return;
                            }
                            target = instances[0];
                        }

                        object convertedValue = Convert.ChangeType(parts[2], propertyInfo.PropertyType);
                        propertyInfo.SetValue(target, convertedValue);
                        ConsoleUI.Instance.Log($"Property {targetName} set to {convertedValue}");
                        return;
                    }
                    catch (Exception e)
                    {
                        ConsoleUI.Instance.LogError($"Error setting property: {e.Message}");
                        return;
                    }
                }

                // Try set field
                if (fields.TryGetValue(targetName, out FieldInfo fieldInfo))
                {
                    try
                    {
                        object target = null;
                        if (!fieldInfo.IsStatic)
                        {
                            var instances = FindObjectsOfType(fieldInfo.DeclaringType);
                            if (instances.Length == 0)
                            {
                                ConsoleUI.Instance.LogError($"No instance of {fieldInfo.DeclaringType.Name} found in scene");
                                return;
                            }
                            target = instances[0];
                        }

                        object convertedValue = Convert.ChangeType(parts[2], fieldInfo.FieldType);
                        fieldInfo.SetValue(target, convertedValue);
                        ConsoleUI.Instance.Log($"Field {targetName} set to {convertedValue}");
                        return;
                    }
                    catch (Exception e)
                    {
                        ConsoleUI.Instance.LogError($"Error setting field: {e.Message}");
                        return;
                    }
                }
            }

            // // Check if it's an event invoke command
            // if (commandName == "invoke" && parts.Length == 2)
            // {
            //     string eventName = parts[1].ToLower();
            //     if (events.TryGetValue(eventName, out EventInfo eventInfo))
            //     {
            //         try
            //         {
            //             object target = null;
            //             if (!eventInfo.GetRaiseMethod().IsStatic) 
            //             {
            //                 var instances = FindObjectsOfType(eventInfo.DeclaringType);
            //                 if (instances.Length == 0)
            //                 {
            //                     ConsoleUI.Instance.LogError($"No instance of {eventInfo.DeclaringType.Name} found in scene");
            //                     return;
            //                 }
            //                 target = instances[0];
            //             }
            //
            //             var delegateType = eventInfo.EventHandlerType;
            //             if (delegateType == typeof(Action))
            //             {
            //                 var raiseMethod = eventInfo.GetRaiseMethod();
            //                 raiseMethod?.Invoke(target, new object[] { });
            //                 ConsoleUI.Instance.Log($"Event {eventName} invoked");
            //             }
            //             else
            //             {
            //                 ConsoleUI.Instance.LogError($"Event {eventName} has unsupported delegate type");
            //             }
            //             return;
            //         }
            //         catch (Exception e)
            //         {
            //             ConsoleUI.Instance.LogError($"Error invoking event: {e.Message}");
            //             return;
            //         }
            //     }
            // }

            // Regular method command execution
            if (commands.TryGetValue(commandName, out MethodInfo methodInfo))
            {
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
            else
            {
                ConsoleUI.Instance.LogError($"Command not found: {commandName}");
            }
        }

        public void EnableConsole(bool enabled)
        {
            ConsoleUI.Instance.gameObject.SetActive(enabled);
        }

        [Command]
        public static void Test()
        {
            Debug.Log("console test");
        }

        [Command]
        public bool CheckConsole()
        {
            return ConsoleUI.Instance.gameObject.activeSelf;
        }

        [Command("addition")]
        public float AdditionTime(float a, float b) => a + b;

        [Command]
        public int TestInt { get; set; }= 0;

        [Command] private float testfloat;
        [Command] public event Action<float> OnTestFloatChanged;
        [Command] public static event Action TestAction;
    }
}