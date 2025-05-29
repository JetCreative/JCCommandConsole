using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetCreative.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Static class for handling command registration and execution for the Command Console Pro system.
    /// </summary>
    public class JCCommandConsolePro: MonoBehaviour
    {
        #region Command Caches

        /// <summary>
        /// Dictionary of method commands, keyed by command name
        /// </summary>
        private Dictionary<string, SerializableMethodInfo> methodCommands => _cache.MethodCommands;

        /// <summary>
        /// Dictionary of property commands, keyed by command name
        /// </summary>
        private Dictionary<string, SerializedPropertyInfo> propertyGetCommands => _cache.PropertyGetCommands;
        
        /// <summary>
        /// Dictionary of property commands, keyed by command name
        /// </summary>
        private Dictionary<string, SerializedPropertyInfo> propertySetCommands => _cache.PropertySetCommands;

        /// <summary>
        /// Dictionary of field commands, keyed by command name
        /// </summary>
        private Dictionary<string, SerializedFieldInfo> fieldCommands => _cache.FieldCommands;

        /// <summary>
        /// Dictionary of delegate commands, keyed by command name
        /// </summary>
        private Dictionary<string, SerializedFieldInfo> delegateCommands => _cache.DelegateCommands;

        /// <summary>
        /// Dictionary mapping commands to their declaring types, used for static command execution
        /// </summary>
        private Dictionary<string, Type> commandDeclaringTypes => _cache.CommandDeclaringTypes;

        /// <summary>
        /// Stores whether each command is static or instance-based
        /// </summary>
        private Dictionary<string, bool> isCommandStatic => _cache.IsCommandStatic;
        
        [SerializeField] private CommandCache _cache;
        
        
        #endregion
        #region Lifecycle

        public static JCCommandConsolePro Instance { get; private set; }
        public static bool IsInitialized => Instance != null;
        private void OnValidate()
        {
            if (Instance != null && Instance != this) 
            {
                this.enabled = false;
                throw new Exception("Only one instance of JCCommandConsolePro can exist at a time.");
            }
            
            Instance = this;
            
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) 
            {
                Destroy(gameObject);
                throw new Exception("Only one instance of JCCommandConsolePro can exist at a time.");
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
//            Debug.Log(fieldCommands.Count);
        }

        #endregion

        #region Command Format Configuration

        /// <summary>
        /// Array of preface commands used to indicate specific intentions
        /// </summary>
        public static string[] TargetCmds = { "@", "@@", "#", "##", "select" };

        /// <summary>
        /// Array of console commands that set intention for the next word
        /// </summary>
        public static string[] consolecmds = { "get", "set", "call" };

        #endregion

        #region Command Registration

        public bool HasGeneratedCache => _cache;
        
        /// <summary>
        /// Generates a cache of all methods, delegates, properties, and field commands marked by the Command attribute.
        /// </summary>
        /// <param name="includePrivate">Whether to include private members in the cache</param>
        /// <param name="includeExampleCommands">Whether to include example commands from ConsoleExampleCommands</param>
        /// <param name="includeNamespaces">Specific namespaces to include (null or empty means all)</param>
        /// <param name="excludeNamespaces">Specific namespaces to exclude</param>
        /// <returns>The total number of commands registered</returns>
        public int GenerateCommandCache(bool includePrivate = false, bool includeExampleCommands = true, 
            string[] includeNamespaces = null, string[] excludeNamespaces = null)
        {
            if (!_cache)
            {
                _cache = GetCommandCache();
            }
            
            // Clear existing caches
            _cache.MethodCommands.Clear();
            _cache.PropertyGetCommands.Clear();
            _cache.PropertySetCommands.Clear();
            _cache.FieldCommands.Clear();
            _cache.DelegateCommands.Clear();
            _cache.CommandDeclaringTypes.Clear();
            _cache.IsCommandStatic.Clear();

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            // if (includePrivate)
            //     bindingFlags |= BindingFlags.NonPublic;

            int commandCount = 0;

            // Get all loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        // Skip if in excluded namespace
                        if (excludeNamespaces != null && 
                            excludeNamespaces.Any(ns => type.Namespace?.StartsWith(ns, StringComparison.OrdinalIgnoreCase) == true))
                            continue;

                        // Skip if not in included namespace (when specified)
                        if (includeNamespaces != null && includeNamespaces.Length > 0 && 
                            !includeNamespaces.Any(ns => type.Namespace?.StartsWith(ns, StringComparison.OrdinalIgnoreCase) == true))
                            continue;

                        // Skip ConsoleExampleCommands if not included
                        if (!includeExampleCommands && type == typeof(ConsoleExampleCommands))
                            continue;

                        // Register methods
                        foreach (var method in type.GetMethods(bindingFlags))
                        {
                            var attribute = method.GetCustomAttribute<CommandAttribute>();
                            if (attribute != null)
                            {
                                //check if should include private methods
                                if (method.IsPrivate && (!includePrivate && !attribute.IncludePrivate))
                                    continue;
                                
                                string commandName = attribute.CommandName ?? method.Name.ToLower();
                                methodCommands[commandName] = new SerializableMethodInfo(method);
                                commandDeclaringTypes[commandName] = type;
                                isCommandStatic[commandName] = method.IsStatic;
                                commandCount++;
                            }
                        }

                        // Register properties
                        foreach (var property in type.GetProperties(bindingFlags))
                        {
                            var attribute = property.GetCustomAttribute<CommandAttribute>();
                            if (attribute != null)
                            {
                                string commandName = attribute.CommandName ?? property.Name.ToLower();
                                bool isCached = false;
                                

                                if (property.GetMethod != null &&  (!property.GetMethod.IsPrivate || includePrivate || attribute.IncludePrivate))
                                {
                                    propertyGetCommands[commandName] = new SerializedPropertyInfo(property);
                                    isCached = true;
                                }
                                
                                //check if the setter is public or includePrivate is true
                                if (property.SetMethod != null && (!property.SetMethod.IsPrivate || includePrivate || attribute.IncludePrivate))
                                {
                                    propertySetCommands[commandName] = new SerializedPropertyInfo(property);
                                    isCached = true;
                                }

                                if (!isCached) continue;
                                
                                commandDeclaringTypes[commandName] = type;
                                isCommandStatic[commandName] = property.GetGetMethod()?.IsStatic ?? 
                                                              property.GetSetMethod()?.IsStatic ?? false;
                                commandCount++;
                            }
                        }

                        // Register fields
                        foreach (var field in type.GetFields(bindingFlags))
                        {
                            var attribute = field.GetCustomAttribute<CommandAttribute>();
                            if (attribute != null)
                            {
                                //check if should include private fields
                                if (field.IsPrivate && (!includePrivate && !attribute.IncludePrivate))
                                    continue;
                                
                                string commandName = attribute.CommandName ?? field.Name.ToLower();
                                
                                // Check if it's a delegate
                                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                                {
                                    delegateCommands[commandName] = new SerializedFieldInfo(field);
                                }
                                else
                                {
                                    fieldCommands[commandName] = new SerializedFieldInfo(field);
                                }
                                
                                commandDeclaringTypes[commandName] = type;
                                isCommandStatic[commandName] = field.IsStatic;
                                commandCount++;
                            }
                        }

                        // Register delegates (events)
                        foreach (var eventInfo in type.GetEvents(bindingFlags))
                        {
                            var attribute = eventInfo.GetCustomAttribute<CommandAttribute>();
                            if (attribute != null)
                            {
                                //check if should include private events
                                if (eventInfo.GetAddMethod().IsPrivate && (!includePrivate && !attribute.IncludePrivate))
                                    continue;
                                
                                string commandName = attribute.CommandName ?? eventInfo.Name.ToLower();
                                
                                // Get the backing field if possible
                                var field = type.GetField(eventInfo.Name, bindingFlags);
                                if (field != null)
                                {
                                    delegateCommands[commandName] = new SerializedFieldInfo(field);
                                    commandDeclaringTypes[commandName] = type;
                                    isCommandStatic[commandName] = field.IsStatic;
                                    commandCount++;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing assembly: {assembly.FullName}. {ex.Message}");
                }
            }

            Debug.Log($"Command Console Pro: {commandCount} commands registered successfully.");
            //HasGeneratedCache = true;
            return commandCount;
        }

        public static CommandCache GetCommandCache()
        {
            var storedCache = AssetDatabase.FindAssets($"t:{nameof(CommandCache)}");

            if (storedCache.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<CommandCache>(AssetDatabase.GUIDToAssetPath(storedCache[0]));
            }
            else
            {
                var newCache = ScriptableObject.CreateInstance<CommandCache>();
                AssetDatabase.CreateAsset(newCache, "Assets/CommandCache.asset");
                return newCache;
            }
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Executes a command from the provided input string.
        /// </summary>
        /// <param name="input">The command input string to parse and execute</param>
        /// <returns>Result of command execution or error message</returns>
        public string ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Error: Empty command.";

            try
            {
                // Tokenize the input
                var tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (tokens.Count == 0)
                    return "Error: Empty command.";

                int currentTokenIndex = 0;
                GameObject[] targetObjects = null;

                // Check if first token is a target command
                if (currentTokenIndex < tokens.Count && StartsWithAny(tokens[currentTokenIndex], TargetCmds, out string prefaceCmd))
                {
                    string objectIdentifier = tokens[currentTokenIndex].Substring(prefaceCmd.Length);
                    currentTokenIndex++;

                    // Handle different target commands
                    switch (prefaceCmd)
                    {
                        case "@":
                            var obj = GameObject.Find(objectIdentifier);
                            if (obj == null)
                                return $"Error: GameObject with name '{objectIdentifier}' not found.";
                            targetObjects = new[] { obj };
                            break;
                        case "@@":
                            targetObjects = Object.FindObjectsOfType<GameObject>()
                                .Where(go => go.name == objectIdentifier).ToArray();
                            if (targetObjects.Length == 0)
                                return $"Error: No GameObjects with name '{objectIdentifier}' found.";
                            break;
                        case "#":
                            targetObjects = GameObject.FindGameObjectsWithTag(objectIdentifier);
                            if (targetObjects.Length == 0)
                                return $"Error: No GameObjects with tag '{objectIdentifier}' found.";
                            targetObjects = new[] { targetObjects[0] };
                            break;
                        case "##":
                            targetObjects = GameObject.FindGameObjectsWithTag(objectIdentifier);
                            if (targetObjects.Length == 0)
                                return $"Error: No GameObjects with tag '{objectIdentifier}' found.";
                            break;
                        case "select" :
                            #if UNITY_EDITOR
                                var selectedObjects = UnityEditor.Selection.gameObjects;
                                if (selectedObjects.Length == 0)
                                   return "Error: No GameObject selected in the Editor.";
                                targetObjects = selectedObjects;
                            #else
                                  return "Error: 'select' command can only be used in the Editor.";
                            #endif
                                break;
                        default:
                            return $"Error: Unknown preface command '{prefaceCmd}'.";
                    }
                }

                // We need a console command next
                if (currentTokenIndex >= tokens.Count)
                    return "Error: Missing console command (get, set, call).";

                string consoleCmd = tokens[currentTokenIndex].ToLower();
                if (!consolecmds.Contains(consoleCmd))
                    return $"Error: Invalid console command '{consoleCmd}'. Valid commands are: {string.Join(", ", consolecmds)}.";
                currentTokenIndex++;

                // We need a command name next
                if (currentTokenIndex >= tokens.Count)
                    return $"Error: Missing command name after '{consoleCmd}'.";

                string commandName = tokens[currentTokenIndex].ToLower();
                currentTokenIndex++;

                // Handle static vs. instance command
                if (isCommandStatic.TryGetValue(commandName, out bool isStatic) && isStatic)
                {
                    // For static commands, we don't need a target object
                    targetObjects = null;
                }
                else if (targetObjects == null ) //&& !isSelectCommand
                {
                    return $"Error: Non-static command '{commandName}' requires a target GameObject. Use @ or # prefaces, or 'select' in Editor.";
                }

                // Execute the command based on console command type
                switch (consoleCmd)
                {
                    case "get":
                        return ExecuteGetCommand(commandName, targetObjects);
                        
                    case "set":
                        if (currentTokenIndex >= tokens.Count)
                            return $"Error: Missing value for 'set {commandName}'.";
                        
                        string valueString = string.Join(" ", tokens.Skip(currentTokenIndex));
                        return ExecuteSetCommand(commandName, valueString, targetObjects);
                        
                    case "call":
                        string[] parameters = currentTokenIndex < tokens.Count 
                            ? tokens.Skip(currentTokenIndex).ToArray() 
                            : Array.Empty<string>();
                        
                        return ExecuteCallCommand(commandName, parameters, targetObjects);
                        
                    default:
                        return $"Error: Unhandled console command '{consoleCmd}'.";
                }
            }
            catch (Exception ex)
            {
                return $"Error executing command: {ex.Message}";
            }
        }

        /// <summary>
        /// Executes a "get" command to retrieve a property or field value.
        /// </summary>
        private string ExecuteGetCommand(string commandName, GameObject[] targetObjects)
        {
            // Check if command exists
            if (propertyGetCommands.TryGetValue(commandName, out SerializedPropertyInfo propertyInfo))
            {
                return GetPropertyValue(propertyInfo.propertyInfo, targetObjects);
            }
            else if (fieldCommands.TryGetValue(commandName, out SerializedFieldInfo fieldInfo))
            {
                return GetFieldValue(fieldInfo.fieldInfo, targetObjects);
            }
            else
            {
                return $"Error: No property or field command found with name '{commandName}'.";
            }
        }

        /// <summary>
        /// Executes a "set" command to set a property or field value.
        /// </summary>
        private string ExecuteSetCommand(string commandName, string valueString, GameObject[] targetObjects)
        {
            // Check if command exists
            if (propertySetCommands.TryGetValue(commandName, out SerializedPropertyInfo propertyInfo))
            {
                return SetPropertyValue(propertyInfo.propertyInfo, valueString, targetObjects);
            }
            else if (fieldCommands.TryGetValue(commandName, out SerializedFieldInfo fieldInfo))
            {
                return SetFieldValue(fieldInfo.fieldInfo, valueString, targetObjects);
            }
            else
            {
                return $"Error: No property or field command found with name '{commandName}'.";
            }
        }

        /// <summary>
        /// Executes a "call" command to invoke a method or delegate.
        /// </summary>
        private string ExecuteCallCommand(string commandName, string[] parameters, GameObject[] targetObjects)
        {
            // Check if command exists
            if (methodCommands.TryGetValue(commandName, out SerializableMethodInfo methodInfo))
            {
                return InvokeMethod(methodInfo.methodInfo, parameters, targetObjects);
            }
            else if (delegateCommands.TryGetValue(commandName, out SerializedFieldInfo delegateField))
            {
                return InvokeDelegate(delegateField.fieldInfo, parameters, targetObjects);
            }
            else
            {
                return $"Error: No method or delegate command found with name '{commandName}'.";
            }
        }

        #endregion

        #region Command Execution Helpers

        /// <summary>
        /// Gets a property value from target objects.
        /// </summary>
        private static string GetPropertyValue(PropertyInfo propertyInfo, GameObject[] targetObjects)
        {
            if (propertyInfo.GetMethod == null)
                return $"Error: Property '{propertyInfo.Name}' has no getter.";

            if (propertyInfo.GetMethod.IsStatic)
            {
                var value = propertyInfo.GetValue(null);
                return $"{propertyInfo.Name} = {FormatValue(value)}";
            }
            else if (targetObjects != null && targetObjects.Length > 0)
            {
                List<string> results = new List<string>();
                foreach (var targetObject in targetObjects)
                {
                    var component = targetObject.GetComponent(propertyInfo.DeclaringType);
                    if (component != null)
                    {
                        var value = propertyInfo.GetValue(component);
                        results.Add($"{targetObject.name}.{propertyInfo.Name} = {FormatValue(value)}");
                    }
                    else
                    {
                        results.Add($"Error: {targetObject.name} has no component of type {propertyInfo.DeclaringType.Name}.");
                    }
                }
                return string.Join("\n", results);
            }
            return $"Error: Cannot get non-static property '{propertyInfo.Name}' without target GameObject.";
        }

        /// <summary>
        /// Gets a field value from target objects.
        /// </summary>
        private static string GetFieldValue(FieldInfo fieldInfo, GameObject[] targetObjects)
        {
            if (fieldInfo.IsStatic)
            {
                var value = fieldInfo.GetValue(null);
                return $"{fieldInfo.Name} = {FormatValue(value)}";
            }
            else if (targetObjects != null && targetObjects.Length > 0)
            {
                List<string> results = new List<string>();
                foreach (var targetObject in targetObjects)
                {
                    var component = targetObject.GetComponent(fieldInfo.DeclaringType);
                    if (component != null)
                    {
                        var value = fieldInfo.GetValue(component);
                        results.Add($"{targetObject.name}.{fieldInfo.Name} = {FormatValue(value)}");
                    }
                    else
                    {
                        results.Add($"Error: {targetObject.name} has no component of type {fieldInfo.DeclaringType.Name}.");
                    }
                }
                return string.Join("\n", results);
            }
            return $"Error: Cannot get non-static field '{fieldInfo.Name}' without target GameObject.";
        }

        /// <summary>
        /// Sets a property value on target objects.
        /// </summary>
        private static string SetPropertyValue(PropertyInfo propertyInfo, string valueString, GameObject[] targetObjects)
        {
            if (propertyInfo.SetMethod == null)
                return $"Error: Property '{propertyInfo.Name}' has no setter.";

            try
            {
                object convertedValue = ConvertStringToType(valueString, propertyInfo.PropertyType);

                if (propertyInfo.SetMethod.IsStatic)
                {
                    propertyInfo.SetValue(null, convertedValue);
                    return $"Set {propertyInfo.Name} = {FormatValue(convertedValue)}";
                }
                else if (targetObjects != null && targetObjects.Length > 0)
                {
                    List<string> results = new List<string>();
                    foreach (var targetObject in targetObjects)
                    {
                        var component = targetObject.GetComponent(propertyInfo.DeclaringType);
                        if (component != null)
                        {
                            propertyInfo.SetValue(component, convertedValue);
                            results.Add($"Set {targetObject.name}.{propertyInfo.Name} = {FormatValue(convertedValue)}");
                        }
                        else
                        {
                            results.Add($"Error: {targetObject.name} has no component of type {propertyInfo.DeclaringType.Name}.");
                        }
                    }
                    return string.Join("\n", results);
                }
                return $"Error: Cannot set non-static property '{propertyInfo.Name}' without target GameObject.";
            }
            catch (Exception ex)
            {
                return $"Error setting property '{propertyInfo.Name}': {ex.Message}";
            }
        }

        /// <summary>
        /// Sets a field value on target objects.
        /// </summary>
        private static string SetFieldValue(FieldInfo fieldInfo, string valueString, GameObject[] targetObjects)
        {
            if (fieldInfo.IsInitOnly)
                return $"Error: Field '{fieldInfo.Name}' is read-only.";

            try
            {
                object convertedValue = ConvertStringToType(valueString, fieldInfo.FieldType);

                if (fieldInfo.IsStatic)
                {
                    fieldInfo.SetValue(null, convertedValue);
                    return $"Set {fieldInfo.Name} = {FormatValue(convertedValue)}";
                }
                else if (targetObjects != null && targetObjects.Length > 0)
                {
                    List<string> results = new List<string>();
                    foreach (var targetObject in targetObjects)
                    {
                        var component = targetObject.GetComponent(fieldInfo.DeclaringType);
                        if (component != null)
                        {
                            fieldInfo.SetValue(component, convertedValue);
                            results.Add($"Set {targetObject.name}.{fieldInfo.Name} = {FormatValue(convertedValue)}");
                        }
                        else
                        {
                            results.Add($"Error: {targetObject.name} has no component of type {fieldInfo.DeclaringType.Name}.");
                        }
                    }
                    return string.Join("\n", results);
                }
                return $"Error: Cannot set non-static field '{fieldInfo.Name}' without target GameObject.";
            }
            catch (Exception ex)
            {
                return $"Error setting field '{fieldInfo.Name}': {ex.Message}";
            }
        }

        /// <summary>
        /// Invokes a method on target objects.
        /// </summary>
        private static string InvokeMethod(MethodInfo methodInfo, string[] parameters, GameObject[] targetObjects)
        {
            try
            {
                var methodParams = methodInfo.GetParameters();
                if (parameters.Length != methodParams.Length)
                {
                    return $"Error: Method '{methodInfo.Name}' requires {methodParams.Length} parameters, but {parameters.Length} were provided.";
                }

                // Convert parameters to the correct types
                object[] convertedParams = new object[methodParams.Length];
                for (int i = 0; i < methodParams.Length; i++)
                {
                    convertedParams[i] = ConvertStringToType(parameters[i], methodParams[i].ParameterType);
                }

                if (methodInfo.IsStatic)
                {
                    var result = methodInfo.Invoke(null, convertedParams);
                    return methodInfo.ReturnType == typeof(void)
                        ? $"Called {methodInfo.Name}()"
                        : $"{methodInfo.Name}() returned: {FormatValue(result)}";
                }
                else if (targetObjects != null && targetObjects.Length > 0)
                {
                    List<string> results = new List<string>();
                    foreach (var targetObject in targetObjects)
                    {
                        var component = targetObject.GetComponent(methodInfo.DeclaringType);
                        if (component != null)
                        {
                            var result = methodInfo.Invoke(component, convertedParams);
                            results.Add(methodInfo.ReturnType == typeof(void)
                                ? $"Called {targetObject.name}.{methodInfo.Name}()"
                                : $"{targetObject.name}.{methodInfo.Name}() returned: {FormatValue(result)}");
                        }
                        else
                        {
                            results.Add($"Error: {targetObject.name} has no component of type {methodInfo.DeclaringType.Name}.");
                        }
                    }
                    return string.Join("\n", results);
                }
                return $"Error: Cannot invoke non-static method '{methodInfo.Name}' without target GameObject.";
            }
            catch (Exception ex)
            {
                return $"Error invoking method '{methodInfo.Name}': {ex.Message}";
            }
        }

        /// <summary>
        /// Invokes a delegate on target objects.
        /// </summary>
        private static string InvokeDelegate(FieldInfo delegateField, string[] parameters, GameObject[] targetObjects)
        {
            try
            {
                if (delegateField.IsStatic)
                {
                    var delegateObj = delegateField.GetValue(null) as Delegate;
                    if (delegateObj == null)
                        return $"Error: Delegate '{delegateField.Name}' is null.";

                    var delegateParams = delegateObj.Method.GetParameters();
                    if (parameters.Length != delegateParams.Length)
                    {
                        return $"Error: Delegate '{delegateField.Name}' requires {delegateParams.Length} parameters, but {parameters.Length} were provided.";
                    }

                    // Convert parameters to the correct types
                    object[] convertedParams = new object[delegateParams.Length];
                    for (int i = 0; i < delegateParams.Length; i++)
                    {
                        convertedParams[i] = ConvertStringToType(parameters[i], delegateParams[i].ParameterType);
                    }

                    var result = delegateObj.DynamicInvoke(convertedParams);
                    var returnType = delegateObj.Method.ReturnType;
                    
                    return returnType == typeof(void)
                        ? $"Invoked delegate {delegateField.Name}"
                        : $"Delegate {delegateField.Name} returned: {FormatValue(result)}";
                }
                else if (targetObjects != null && targetObjects.Length > 0)
                {
                    List<string> results = new List<string>();
                    foreach (var targetObject in targetObjects)
                    {
                        var component = targetObject.GetComponent(delegateField.DeclaringType);
                        if (component != null)
                        {
                            var delegateObj = delegateField.GetValue(component) as Delegate;
                            if (delegateObj == null)
                            {
                                results.Add($"Error: Delegate '{targetObject.name}.{delegateField.Name}' is null.");
                                continue;
                            }

                            var delegateParams = delegateObj.Method.GetParameters();
                            if (parameters.Length != delegateParams.Length)
                            {
                                results.Add($"Error: Delegate '{targetObject.name}.{delegateField.Name}' requires {delegateParams.Length} parameters, but {parameters.Length} were provided.");
                                continue;
                            }

                            // Convert parameters to the correct types
                            object[] convertedParams = new object[delegateParams.Length];
                            for (int i = 0; i < delegateParams.Length; i++)
                            {
                                convertedParams[i] = ConvertStringToType(parameters[i], delegateParams[i].ParameterType);
                            }

                            var result = delegateObj.DynamicInvoke(convertedParams);
                            var returnType = delegateObj.Method.ReturnType;
                            
                            results.Add(returnType == typeof(void)
                                ? $"Invoked delegate {targetObject.name}.{delegateField.Name}"
                                : $"Delegate {targetObject.name}.{delegateField.Name} returned: {FormatValue(result)}");
                        }
                        else
                        {
                            results.Add($"Error: {targetObject.name} has no component of type {delegateField.DeclaringType.Name}.");
                        }
                    }
                    return string.Join("\n", results);
                }
                return $"Error: Cannot invoke non-static delegate '{delegateField.Name}' without target GameObject.";
            }
            catch (Exception ex)
            {
                return $"Error invoking delegate '{delegateField.Name}': {ex.Message}";
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Checks if a string starts with any of the provided prefixes and returns the matched prefix.
        /// </summary>
        private static bool StartsWithAny(string input, string[] prefixes, out string matchedPrefix)
        {
            foreach (var prefix in prefixes)
            {
                if (input.StartsWith(prefix))
                {
                    matchedPrefix = prefix;
                    return true;
                }
            }
            matchedPrefix = null;
            return false;
        }

        /// <summary>
        /// Formats a value for display in the console.
        /// </summary>
        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is Array array)
            {
                return $"[{string.Join(", ", array.Cast<object>().Select(FormatValue))}]";
            }

            return value.ToString();
        }

        /// <summary>
        /// Converts a string to the specified type.
        /// </summary>
        private static object ConvertStringToType(string value, Type targetType)
        {
            if (value == null)
                return null;

            // Handle common type aliases
            targetType = NormalizeType(targetType);

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                if (string.IsNullOrEmpty(value))
                    return null;
                targetType = underlyingType;
            }

            // Handle special Unity types
            if (targetType == typeof(Vector2))
            {
                var parts = value.Split(',');
                if (parts.Length == 2)
                {
                    return new Vector2(
                        float.Parse(parts[0].Trim()),
                        float.Parse(parts[1].Trim())
                    );
                }
            }
            else if (targetType == typeof(Vector3))
            {
                var parts = value.Split(',');
                if (parts.Length == 3)
                {
                    return new Vector3(
                        float.Parse(parts[0].Trim()),
                        float.Parse(parts[1].Trim()),
                        float.Parse(parts[2].Trim())
                    );
                }
            }
            else if (targetType == typeof(Vector4))
            {
                var parts = value.Split(',');
                if (parts.Length == 4)
                {
                    return new Vector4(
                        float.Parse(parts[0].Trim()),
                        float.Parse(parts[1].Trim()),
                        float.Parse(parts[2].Trim()),
                        float.Parse(parts[3].Trim())
                    );
                }
            }
            else if (targetType == typeof(Color))
            {
                var parts = value.Split(',');
                if (parts.Length >= 3)
                {
                    float r = float.Parse(parts[0].Trim());
                    float g = float.Parse(parts[1].Trim());
                    float b = float.Parse(parts[2].Trim());
                    float a = parts.Length > 3 ? float.Parse(parts[3].Trim()) : 1f;
                    return new Color(r, g, b, a);
                }
            }
            else if (targetType == typeof(GameObject))
            {
                return GameObject.Find(value);
            }
            else if (targetType == typeof(Transform))
            {
                var go = GameObject.Find(value);
                return go?.transform;
            }
            else if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value, true);
            }
            else if (targetType == typeof(bool))
            {
                if (value.ToLower() == "true" || value == "1")
                    return true;
                if (value.ToLower() == "false" || value == "0")
                    return false;
            }

            // Default conversion
            return Convert.ChangeType(value, targetType);
        }

        /// <summary>
        /// Normalizes type aliases to their actual types.
        /// </summary>
        private static Type NormalizeType(Type type)
        {
            var typeMap = new Dictionary<Type, Type>
            {
                { typeof(System.Single), typeof(float) },
                { typeof(System.Int32), typeof(int) },
                { typeof(System.Boolean), typeof(bool) },
                { typeof(System.Double), typeof(double) },
                { typeof(System.String), typeof(string) }
            };

            if (typeMap.TryGetValue(type, out Type normalizedType))
                return normalizedType;

            return type;
        }

        /// <summary>
        /// Gets all available commands for autocomplete.
        /// </summary>
        public IEnumerable<string> GetAllCommands()
        {
            var commands = new List<string>();
            commands.AddRange(methodCommands.Keys);
            commands.AddRange(propertyGetCommands.Keys);
            commands.AddRange(propertySetCommands.Keys);
            commands.AddRange(fieldCommands.Keys);
            commands.AddRange(delegateCommands.Keys);
            return commands.Distinct();
        }

        /// <summary>
        /// Gets type information for a command to aid in prediction.
        /// </summary>
        public string GetCommandTypeInfo(string commandName)
        {
            if (methodCommands.TryGetValue(commandName, out SerializableMethodInfo methodInfo))
            {
                var parameters = methodInfo.methodInfo.GetParameters();
                if (parameters.Length == 0)
                    return "(method) returns " + FormatTypeName(methodInfo.methodInfo.ReturnType);
                
                var paramInfo = string.Join(", ", parameters.Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
                return $"(method) ({paramInfo}) returns {FormatTypeName(methodInfo.methodInfo.ReturnType)}";
            }
            else if (propertyGetCommands.TryGetValue(commandName, out SerializedPropertyInfo propertyGetInfo))
            {
                return $"(property) {FormatTypeName(propertyGetInfo.propertyInfo.PropertyType)}";
            }
            else if (propertySetCommands.TryGetValue(commandName, out SerializedPropertyInfo propertySetInfo))
            {
                return $"(property) {FormatTypeName(propertySetInfo.propertyInfo.PropertyType)}";
            }
            else if (fieldCommands.TryGetValue(commandName, out SerializedFieldInfo fieldInfo))
            {
                return $"(field) {FormatTypeName(fieldInfo.fieldInfo.FieldType)}";
            }
            else if (delegateCommands.TryGetValue(commandName, out SerializedFieldInfo delegateField))
            {
                if (delegateField.fieldInfo.FieldType.IsSubclassOf(typeof(Delegate)))
                {
                    var invokeMethod = delegateField.fieldInfo.FieldType.GetMethod("Invoke");
                    if (invokeMethod != null)
                    {
                        var parameters = invokeMethod.GetParameters();
                        var paramInfo = string.Join(", ", parameters.Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
                        return $"(delegate) ({paramInfo}) returns {FormatTypeName(invokeMethod.ReturnType)}";
                    }
                }
                return $"(delegate)";
            }
            
            return null;
        }

        /// <summary>
        /// Formats a type name in a user-friendly way.
        /// </summary>
        private static string FormatTypeName(Type type)
        {
            if (type == typeof(void))
                return "void";
            if (type == typeof(int))
                return "int";
            if (type == typeof(float))
                return "float";
            if (type == typeof(double))
                return "double";
            if (type == typeof(bool))
                return "bool";
            if (type == typeof(string))
                return "string";
            
            return type.Name;
        }

        /// <summary>
        /// Returns all enum values for a given enum type.
        /// </summary>
        public static string[] GetEnumValues(Type enumType)
        {
            if (enumType == null || !enumType.IsEnum)
                return new string[0];
                
            return Enum.GetNames(enumType);
        }

        /// <summary>
        /// Removes consecutive empty entries from the word list to include spaces
        /// </summary>
        /// <param name="words">List of words to clean up</param>
        public static void RemoveConsecutiveEmptyEntries(List<string> words)
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
        
        /// <summary>
        /// Predicts the next possible words in a command.
        /// </summary>
        public string[] PredictNextWords(string currentInput)
        {
            var tokens = currentInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            //RemoveConsecutiveEmptyEntries(tokens);

//            Debug.Log("tokens" + tokens + "tokens count" + tokens.Count);
            
            var predictions = new List<string>();
            
            // No tokens yet, suggest preface commands and "select"
            if (tokens.Count == 0)
            {
                // predictions.AddRange(TargetCmds);
                // predictions.Add("select");
                // return predictions.ToArray();
                return Array.Empty<string>();
            }
            
            // First token - suggest GameObject names or tags after @ or #
            if (tokens.Count == 1)
            {
                string token = tokens[0];
                
                // Handle prefaced commands
                if (token.StartsWith("@") || token.StartsWith("@@") || 
                    token.StartsWith("#") || token.StartsWith("##"))
                {
                    // For @ and @@, suggest GameObject names
                    if (token.StartsWith("@") || token.StartsWith("@@"))
                    {
                        var prefix = token.StartsWith("@@") ? "@@" : "@";
                        var partialName = token.Substring(prefix.Length);
                        
                        // Get all GameObject names in scene
                        foreach (var go in Object.FindObjectsOfType<GameObject>())
                        {
                            if (string.IsNullOrEmpty(partialName) || go.name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase))
                            {
                                predictions.Add(prefix + go.name);
                            }
                        }
                    }
                    // For # and ##, suggest tags
                    else if (token.StartsWith("#") || token.StartsWith("##"))
                    {
                        var prefix = token.StartsWith("##") ? "##" : "#";
                        var partialTag = token.Substring(prefix.Length);
                        
                        foreach (var tag in UnityEditorInternal.InternalEditorUtility.tags)
                        {
                            if (string.IsNullOrEmpty(partialTag) || tag.StartsWith(partialTag, StringComparison.OrdinalIgnoreCase))
                            {
                                predictions.Add(prefix + tag);
                            }
                        }
                    }
                }
                // If first token is "select", suggest console commands
                else if (token == "select")
                {
                    predictions.AddRange(consolecmds);
                }
                // Suggest preface commands and "select"
                else
                {
                    var partialCommand = tokens[0];
                    
                    foreach (var prefaceCmd in TargetCmds)
                    {
                        if (prefaceCmd.StartsWith(partialCommand) || 
                            (partialCommand.StartsWith(prefaceCmd) && partialCommand.Length > prefaceCmd.Length))
                        {
                            predictions.Add(prefaceCmd);
                        }
                    }
                    
                    // if ("select".StartsWith(partialCommand))
                    // {
                    //     predictions.Add("select");
                    // }
                }
                
                return predictions.ToArray();
            }
            
            // Second token after a preface command or "select" - suggest console commands
            if (tokens.Count == 2 && (
                tokens[0].StartsWith("@") || tokens[0].StartsWith("#") || tokens[0] == "select"))
            {
                var partialCmd = tokens[1];
                
                foreach (var consoleCmd in consolecmds)
                {
                    if (string.IsNullOrEmpty(partialCmd) || consoleCmd.StartsWith(partialCmd, StringComparison.OrdinalIgnoreCase))
                    {
                        predictions.Add(consoleCmd);
                    }
                }
                
                return predictions.ToArray();
            }
            
            // Third token - suggest command names
            if (tokens.Count == 3 && consolecmds.Contains(tokens[1].ToLower()))
            {
                var consoleCmd = tokens[1].ToLower();
                var partialCommandName = tokens[2].ToLower();
                
                // For "get" and "set", suggest property and field commands
                if (consoleCmd == "get" || consoleCmd == "set")
                {
                    var propertyCommands = consoleCmd == "get" ? propertyGetCommands.Keys : propertySetCommands.Keys;
                    
                    foreach (var cmd in propertyCommands.Concat(fieldCommands.Keys))
                    {
                        if (string.IsNullOrEmpty(partialCommandName) || cmd.StartsWith(partialCommandName, StringComparison.OrdinalIgnoreCase))
                        {
                            predictions.Add(cmd);
                        }
                    }
                }
                // For "call", suggest method and delegate commands
                else if (consoleCmd == "call")
                {
                    foreach (var cmd in methodCommands.Keys.Concat(delegateCommands.Keys))
                    {
                        if (string.IsNullOrEmpty(partialCommandName) || cmd.StartsWith(partialCommandName, StringComparison.OrdinalIgnoreCase))
                        {
                            predictions.Add(cmd);
                        }
                    }
                }
                
                return predictions.ToArray();
            }
            
            // Fourth token for "set" commands - suggest possible values
            if (tokens.Count == 4 && tokens[1].ToLower() == "set")
            {
                var commandName = tokens[2].ToLower();
                
                // For property commands
                if (propertySetCommands.TryGetValue(commandName, out SerializedPropertyInfo propertyInfo))
                {
                    if (propertyInfo.propertyInfo.PropertyType.IsEnum)
                    {
                        return GetEnumValues(propertyInfo.propertyInfo.PropertyType);
                    }
                    else if (propertyInfo.propertyInfo.PropertyType == typeof(bool))
                    {
                        return new[] { "true", "false" };
                    }
                }
                // For field commands
                else if (fieldCommands.TryGetValue(commandName, out SerializedFieldInfo fieldInfo))
                {
                    if (fieldInfo.fieldInfo.FieldType.IsEnum)
                    {
                        return GetEnumValues(fieldInfo.fieldInfo.FieldType);
                    }
                    else if (fieldInfo.fieldInfo.FieldType == typeof(bool))
                    {
                        return new[] { "true", "false" };
                    }
                }
            }
            
            // For method or delegate parameters, check parameter types
            if (tokens.Count >= 4 && tokens[1].ToLower() == "call")
            {
                var commandName = tokens[2].ToLower();
                int paramIndex = tokens.Count - 4;
                
                // For method commands
                if (methodCommands.TryGetValue(commandName, out SerializableMethodInfo methodInfo))
                {
                    var parameters = methodInfo.methodInfo.GetParameters();
                    if (paramIndex < parameters.Length && parameters[paramIndex].ParameterType.IsEnum)
                    {
                        return GetEnumValues(parameters[paramIndex].ParameterType);
                    }
                    else if (paramIndex < parameters.Length && parameters[paramIndex].ParameterType == typeof(bool))
                    {
                        return new[] { "true", "false" };
                    }
                }
                // For delegate commands
                else if (delegateCommands.TryGetValue(commandName, out SerializedFieldInfo delegateField) && 
                         delegateField.fieldInfo.FieldType.IsSubclassOf(typeof(Delegate)))
                {
                    var invokeMethod = delegateField.fieldInfo.FieldType.GetMethod("Invoke");
                    if (invokeMethod != null)
                    {
                        var parameters = invokeMethod.GetParameters();
                        if (paramIndex < parameters.Length && parameters[paramIndex].ParameterType.IsEnum)
                        {
                            return GetEnumValues(parameters[paramIndex].ParameterType);
                        }
                        else if (paramIndex < parameters.Length && parameters[paramIndex].ParameterType == typeof(bool))
                        {
                            return new[] { "true", "false" };
                        }
                    }
                }
            }
            
            return predictions.ToArray();
        }

        #endregion
    }
}
