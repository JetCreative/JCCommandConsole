using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
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

        public Dictionary<string, MethodInfo> methodCommands { get; private set; }= new Dictionary<string, MethodInfo>();
        
        public Dictionary<string, PropertyInfo> properties { get; private set; }= new Dictionary<string, PropertyInfo>();
        public Dictionary<string, FieldInfo> fields { get; private set; }= new Dictionary<string, FieldInfo>();
        //private Dictionary<string, EventInfo> events = new Dictionary<string, EventInfo>();
        
        public string[] prefaceConsoleCommands { get; private set; }= new string[] { "@", "@@"};
        public string[] propertyConsoleCommands { get; private set; }= new string[] { "get", "set"};
        

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
                            this.methodCommands[commandName] = method;
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

        private GameObject FindTargetGameObject(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return null;

            // First try to find by exact name (case insensitive)
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            GameObject target = allObjects.FirstOrDefault(obj => obj.name.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            if (target != null) return target;

            // Then try to find by tag (case insensitive)
            string[] allTags = UnityEditorInternal.InternalEditorUtility.tags;
            string matchingTag = allTags.FirstOrDefault(tag => tag.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(matchingTag))
            {
                try {
                    target = GameObject.FindWithTag(matchingTag);
                    if (target != null) return target;
                }
                catch {}
            }

            return null;
        }

        private GameObject[] FindTargetGameObjects(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return null;

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            return allObjects.Where(obj => obj.name.Equals(identifier, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        public void ExecuteCommand(string commandInput)
        {
            string[] parts = commandInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            GameObject[] targetGameObjects = null;
            GameObject targetGameObject = null;
            int startParamIndex = 0;
            
            // Check if target is specified with @@ or @ symbol at start
            if (parts[0].StartsWith("@@"))
            {
                string targetIdentifier = parts[0].Substring(2);
                targetGameObjects = FindTargetGameObjects(targetIdentifier);
                if (targetGameObjects == null || targetGameObjects.Length == 0)
                {
                    ConsoleUI.Instance.LogError($"No GameObjects found with name '{targetIdentifier}'");
                    return;
                }
                startParamIndex = 1;
                
                if (parts.Length <= startParamIndex)
                {
                    ConsoleUI.Instance.LogError("No command specified after target");
                    return;
                }
            }
            else if (parts[0].StartsWith("@"))
            {
                string targetIdentifier = parts[0].Substring(1); 
                targetGameObject = FindTargetGameObject(targetIdentifier);
                if (targetGameObject == null)
                {
                    ConsoleUI.Instance.LogError($"Target GameObject/Tag '{targetIdentifier}' not found");
                    return;
                }
                startParamIndex = 1;
                
                if (parts.Length <= startParamIndex)
                {
                    ConsoleUI.Instance.LogError("No command specified after target");
                    return;
                }
            }

            string commandName = parts[startParamIndex].ToLower();

            // Check if it's a property/field get command
            if (commandName == "get" && parts.Length >= startParamIndex + 2)
            {
                string targetName = parts[startParamIndex + 1].ToLower();

                // Try get property
                if (properties.TryGetValue(targetName, out PropertyInfo propertyInfo))
                {
                    try
                    {
                        if (targetGameObjects != null)
                        {
                            foreach (var gameObj in targetGameObjects)
                            {
                                var component = gameObj.GetComponent(propertyInfo.DeclaringType);
                                if (component != null)
                                {
                                    object getPropertyValue = propertyInfo.GetValue(component);
                                    ConsoleUI.Instance.Log($"{gameObj.name}: {targetName} = {getPropertyValue}");
                                }
                            }
                            return;
                        }

                        object target = null;
                        if (!propertyInfo.GetMethod.IsStatic)
                        {
                            if (targetGameObject != null)
                            {
                                target = targetGameObject.GetComponent(propertyInfo.DeclaringType);
                                if (target == null)
                                {
                                    ConsoleUI.Instance.LogError($"Component {propertyInfo.DeclaringType.Name} not found on {targetGameObject.name}");
                                    return;
                                }
                            }
                            else
                            {
                                var instances = FindObjectsOfType(propertyInfo.DeclaringType);
                                if (instances.Length == 0)
                                {
                                    ConsoleUI.Instance.LogError($"No instance of {propertyInfo.DeclaringType.Name} found in scene");
                                    return;
                                }
                                target = instances[0];
                            }
                        }

                        object value = propertyInfo.GetValue(target);
                        ConsoleUI.Instance.Log($"{targetName} = {value}");
                        return;
                    }
                    catch (Exception e)
                    {
                        ConsoleUI.Instance.LogError($"Error getting property value: {e.Message}");
                        return;
                    }
                }

                // Try get field
                if (fields.TryGetValue(targetName, out FieldInfo fieldInfo))
                {
                    try
                    {
                        if (targetGameObjects != null)
                        {
                            foreach (var gameObj in targetGameObjects)
                            {
                                var component = gameObj.GetComponent(fieldInfo.DeclaringType);
                                if (component != null)
                                {
                                    object getFieldValue = fieldInfo.GetValue(component);
                                    ConsoleUI.Instance.Log($"{gameObj.name}: {targetName} = {getFieldValue}");
                                }
                            }
                            return;
                        }

                        object target = null;
                        if (!fieldInfo.IsStatic)
                        {
                            if (targetGameObject != null)
                            {
                                target = targetGameObject.GetComponent(fieldInfo.DeclaringType);
                                if (target == null)
                                {
                                    ConsoleUI.Instance.LogError($"Component {fieldInfo.DeclaringType.Name} not found on {targetGameObject.name}");
                                    return;
                                }
                            }
                            else
                            {
                                var instances = FindObjectsOfType(fieldInfo.DeclaringType);
                                if (instances.Length == 0)
                                {
                                    ConsoleUI.Instance.LogError($"No instance of {fieldInfo.DeclaringType.Name} found in scene");
                                    return;
                                }
                                target = instances[0];
                            }
                        }

                        object value = fieldInfo.GetValue(target);
                        ConsoleUI.Instance.Log($"{targetName} = {value}");
                        return;
                    }
                    catch (Exception e)
                    {
                        ConsoleUI.Instance.LogError($"Error getting field value: {e.Message}");
                        return;
                    }
                }

                ConsoleUI.Instance.LogError($"Property or field not found: {targetName}");
                return;
            }

            // Check if it's a property setting command
            if (commandName == "set" && parts.Length >= startParamIndex + 3)
            {
                string targetName = parts[startParamIndex + 1].ToLower();
                string value = parts[startParamIndex + 2];
                
                // Try set property
                if (properties.TryGetValue(targetName, out PropertyInfo propertyInfo))
                {
                    try
                    {
                        if (targetGameObjects != null)
                        {
                            foreach (var gameObj in targetGameObjects)
                            {
                                var component = gameObj.GetComponent(propertyInfo.DeclaringType);
                                if (component != null)
                                {
                                    object propertyValue = Convert.ChangeType(value, propertyInfo.PropertyType);
                                    propertyInfo.SetValue(component, propertyValue);
                                    ConsoleUI.Instance.Log($"Property {targetName} set to {propertyValue} on {gameObj.name}");
                                }
                            }
                            return;
                        }

                        object target = null;
                        if (!propertyInfo.GetMethod.IsStatic)
                        {
                            if (targetGameObject != null)
                            {
                                target = targetGameObject.GetComponent(propertyInfo.DeclaringType);
                                if (target == null)
                                {
                                    ConsoleUI.Instance.LogError($"Component {propertyInfo.DeclaringType.Name} not found on {targetGameObject.name}");
                                    return;
                                }
                            }
                            else
                            {
                                var instances = FindObjectsOfType(propertyInfo.DeclaringType);
                                if (instances.Length == 0)
                                {
                                    ConsoleUI.Instance.LogError($"No instance of {propertyInfo.DeclaringType.Name} found in scene");
                                    return;
                                }
                                target = instances[0];
                            }
                        }

                        object convertedValue = Convert.ChangeType(value, propertyInfo.PropertyType);
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
                        if (targetGameObjects != null)
                        {
                            foreach (var gameObj in targetGameObjects)
                            {
                                var component = gameObj.GetComponent(fieldInfo.DeclaringType);
                                if (component != null)
                                {
                                    object fieldValue = Convert.ChangeType(value, fieldInfo.FieldType);
                                    fieldInfo.SetValue(component, fieldValue);
                                    ConsoleUI.Instance.Log($"Field {targetName} set to {fieldValue} on {gameObj.name}");
                                }
                            }
                            return;
                        }

                        object target = null;
                        if (!fieldInfo.IsStatic)
                        {
                            if (targetGameObject != null)
                            {
                                target = targetGameObject.GetComponent(fieldInfo.DeclaringType);
                                if (target == null)
                                {
                                    ConsoleUI.Instance.LogError($"Component {fieldInfo.DeclaringType.Name} not found on {targetGameObject.name}");
                                    return;
                                }
                            }
                            else
                            {
                                var instances = FindObjectsOfType(fieldInfo.DeclaringType);
                                if (instances.Length == 0)
                                {
                                    ConsoleUI.Instance.LogError($"No instance of {fieldInfo.DeclaringType.Name} found in scene");
                                    return;
                                }
                                target = instances[0];
                            }
                        }

                        object convertedValue = Convert.ChangeType(value, fieldInfo.FieldType);
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

            // Regular method command execution 
            if (methodCommands.TryGetValue(commandName, out MethodInfo methodInfo))
            {
                try
                {
                    var parameters = methodInfo.GetParameters();
                    object[] paramValues = new object[parameters.Length];
                    
                    int expectedParams = parts.Length - (startParamIndex + 1);
                    if (expectedParams != parameters.Length)
                    {
                        ConsoleUI.Instance.LogError($"Invalid number of parameters. Expected {parameters.Length}, got {expectedParams}");
                        return;
                    }

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        paramValues[i] = Convert.ChangeType(parts[i + startParamIndex + 1], parameters[i].ParameterType);
                    }

                    if (targetGameObjects != null)
                    {
                        foreach (var gameObj in targetGameObjects)
                        {
                            var target = gameObj.GetComponent(methodInfo.DeclaringType);
                            if (target != null)
                            {
                                object methodResult = methodInfo.Invoke(target, paramValues);
                                if (methodResult != null)
                                {
                                    ConsoleUI.Instance.Log($"{gameObj.name}: {methodResult}");
                                }
                            }
                        }
                        return;
                    }

                    object result = null;
                    if (methodInfo.IsStatic)
                    {
                        result = methodInfo.Invoke(null, paramValues);
                    }
                    else
                    {
                        object target = null;
                        if (targetGameObject != null)
                        {
                            target = targetGameObject.GetComponent(methodInfo.DeclaringType);
                            if (target == null)
                            {
                                ConsoleUI.Instance.LogError($"Component {methodInfo.DeclaringType.Name} not found on {targetGameObject.name}");
                                return;
                            }
                        }
                        else
                        {
                            var instances = FindObjectsOfType(methodInfo.DeclaringType);
                            if (instances.Length > 0)
                            {
                                target = instances[0];
                            }
                            else
                            {
                                ConsoleUI.Instance.LogError($"No instance of {methodInfo.DeclaringType.Name} found in scene");
                                return;
                            }
                        }
                        result = methodInfo.Invoke(target, paramValues);
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
        public static void Test(float testValue, int testInt)
        {
            Debug.Log("console test " + testValue);
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

        
    }
}