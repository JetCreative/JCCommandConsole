using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Static class for handling command registration and execution for the Command Console Pro system.
    /// </summary>
    public static class JCCommandConsolePro
    {
        #region Command Caches

        /// <summary>
        /// Dictionary of method commands, keyed by command name
        /// </summary>
        private static Dictionary<string, MethodInfo> methodCommands = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// Dictionary of property commands, keyed by command name
        /// </summary>
        private static Dictionary<string, PropertyInfo> propertyCommands = new Dictionary<string, PropertyInfo>();

        /// <summary>
        /// Dictionary of field commands, keyed by command name
        /// </summary>
        private static Dictionary<string, FieldInfo> fieldCommands = new Dictionary<string, FieldInfo>();

        /// <summary>
        /// Dictionary of delegate commands, keyed by command name
        /// </summary>
        private static Dictionary<string, FieldInfo> delegateCommands = new Dictionary<string, FieldInfo>();

        /// <summary>
        /// Dictionary mapping commands to their declaring types, used for static command execution
        /// </summary>
        private static Dictionary<string, Type> commandDeclaringTypes = new Dictionary<string, Type>();

        /// <summary>
        /// Stores whether each command is static or instance-based
        /// </summary>
        private static Dictionary<string, bool> isCommandStatic = new Dictionary<string, bool>();

        #endregion

        #region Command Format Configuration

        /// <summary>
        /// Array of preface commands used to indicate specific intentions
        /// </summary>
        public static string[] prefacecmds = { "@", "@@", "#", "##" };

        /// <summary>
        /// Array of console commands that set intention for the next word
        /// </summary>
        public static string[] consolecmds = { "get", "set", "select", "call" };

        #endregion

        #region Command Registration

        public static bool HasGeneratedCache;
        
        /// <summary>
        /// Generates a cache of all methods, delegates, properties, and field commands marked by the Command attribute.
        /// </summary>
        /// <param name="includePrivate">Whether to include private members in the cache</param>
        /// <param name="includeExampleCommands">Whether to include example commands from ConsoleExampleCommands</param>
        /// <param name="includeNamespaces">Specific namespaces to include (null or empty means all)</param>
        /// <param name="excludeNamespaces">Specific namespaces to exclude</param>
        /// <returns>The total number of commands registered</returns>
        public static int GenerateCommandCache(bool includePrivate = false, bool includeExampleCommands = true, 
            string[] includeNamespaces = null, string[] excludeNamespaces = null)
        {
            // Clear existing caches
            methodCommands.Clear();
            propertyCommands.Clear();
            fieldCommands.Clear();
            delegateCommands.Clear();
            commandDeclaringTypes.Clear();
            isCommandStatic.Clear();

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
            if (includePrivate)
                bindingFlags |= BindingFlags.NonPublic;

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
                                string commandName = attribute.CommandName ?? method.Name.ToLower();
                                methodCommands[commandName] = method;
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
                                propertyCommands[commandName] = property;
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
                                string commandName = attribute.CommandName ?? field.Name.ToLower();
                                
                                // Check if it's a delegate
                                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                                {
                                    delegateCommands[commandName] = field;
                                }
                                else
                                {
                                    fieldCommands[commandName] = field;
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
                                string commandName = attribute.CommandName ?? eventInfo.Name.ToLower();
                                
                                // Get the backing field if possible
                                var field = type.GetField(eventInfo.Name, bindingFlags);
                                if (field != null)
                                {
                                    delegateCommands[commandName] = field;
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
            HasGeneratedCache = true;
            return commandCount;
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Executes a command from the provided input string.
        /// </summary>
        /// <param name="input">The command input string to parse and execute</param>
        /// <returns>Result of command execution or error message</returns>
        public static string ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "Error: Empty command.";

            try
            {
                // Tokenize the input
                string[] tokens = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    return "Error: Empty command.";

                int currentTokenIndex = 0;
                GameObject[] targetObjects = null;
                bool isSelectCommand = false;

                // Check if first token is a preface command
                if (currentTokenIndex < tokens.Length && StartsWithAny(tokens[currentTokenIndex], prefacecmds, out string prefaceCmd))
                {
                    string objectIdentifier = tokens[currentTokenIndex].Substring(prefaceCmd.Length);
                    currentTokenIndex++;

                    // Handle different preface commands
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
                        default:
                            return $"Error: Unknown preface command '{prefaceCmd}'.";
                    }
                }
                // Check if first token is "select"
                else if (currentTokenIndex < tokens.Length && tokens[currentTokenIndex].ToLower() == "select")
                {
                    currentTokenIndex++;
                    isSelectCommand = true;

                    #if UNITY_EDITOR
                    var selectedObjects = UnityEditor.Selection.gameObjects;
                    if (selectedObjects.Length == 0)
                        return "Error: No GameObject selected in the Editor.";
                    targetObjects = selectedObjects;
                    #else
                    return "Error: 'select' command can only be used in the Editor.";
                    #endif
                }

                // We need a console command next
                if (currentTokenIndex >= tokens.Length)
                    return "Error: Missing console command (get, set, call).";

                string consoleCmd = tokens[currentTokenIndex].ToLower();
                if (!consolecmds.Contains(consoleCmd))
                    return $"Error: Invalid console command '{consoleCmd}'. Valid commands are: {string.Join(", ", consolecmds)}.";
                currentTokenIndex++;

                // We need a command name next
                if (currentTokenIndex >= tokens.Length)
                    return $"Error: Missing command name after '{consoleCmd}'.";

                string commandName = tokens[currentTokenIndex].ToLower();
                currentTokenIndex++;

                // Handle static vs. instance command
                bool isStatic = false;
                if (isCommandStatic.TryGetValue(commandName, out isStatic) && isStatic)
                {
                    // For static commands, we don't need a target object
                    targetObjects = null;
                }
                else if (targetObjects == null && !isSelectCommand)
                {
                    return $"Error: Non-static command '{commandName}' requires a target GameObject. Use @ or # preface, or 'select' in Editor.";
                }

                // Execute the command based on console command type
                switch (consoleCmd)
                {
                    case "get":
                        return ExecuteGetCommand(commandName, targetObjects);
                        
                    case "set":
                        if (currentTokenIndex >= tokens.Length)
                            return $"Error: Missing value for 'set {commandName}'.";
                        
                        string valueString = string.Join(" ", tokens.Skip(currentTokenIndex));
                        return ExecuteSetCommand(commandName, valueString, targetObjects);
                        
                    case "call":
                        string[] parameters = currentTokenIndex < tokens.Length 
                            ? tokens.Skip(currentTokenIndex).ToArray() 
                            : new string[0];
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
        private static string ExecuteGetCommand(string commandName, GameObject[] targetObjects)
        {
            // Check if command exists
            if (propertyCommands.TryGetValue(commandName, out PropertyInfo propertyInfo))
            {
                return GetPropertyValue(propertyInfo, targetObjects);
            }
            else if (fieldCommands.TryGetValue(commandName, out FieldInfo fieldInfo))
            {
                return GetFieldValue(fieldInfo, targetObjects);
            }
            else
            {
                return $"Error: No property or field command found with name '{commandName}'.";
            }
        }

        /// <summary>
        /// Executes a "set" command to set a property or field value.
        /// </summary>
        private static string ExecuteSetCommand(string commandName, string valueString, GameObject[] targetObjects)
        {
            // Check if command exists
            if (propertyCommands.TryGetValue(commandName, out PropertyInfo propertyInfo))
            {
                return SetPropertyValue(propertyInfo, valueString, targetObjects);
            }
            else if (fieldCommands.TryGetValue(commandName, out FieldInfo fieldInfo))
            {
                return SetFieldValue(fieldInfo, valueString, targetObjects);
            }
            else
            {
                return $"Error: No property or field command found with name '{commandName}'.";
            }
        }

        /// <summary>
        /// Executes a "call" command to invoke a method or delegate.
        /// </summary>
        private static string ExecuteCallCommand(string commandName, string[] parameters, GameObject[] targetObjects)
        {
            // Check if command exists
            if (methodCommands.TryGetValue(commandName, out MethodInfo methodInfo))
            {
                return InvokeMethod(methodInfo, parameters, targetObjects);
            }
            else if (delegateCommands.TryGetValue(commandName, out FieldInfo delegateField))
            {
                return InvokeDelegate(delegateField, parameters, targetObjects);
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
        public static IEnumerable<string> GetAllCommands()
        {
            var commands = new List<string>();
            commands.AddRange(methodCommands.Keys);
            commands.AddRange(propertyCommands.Keys);
            commands.AddRange(fieldCommands.Keys);
            commands.AddRange(delegateCommands.Keys);
            return commands.Distinct();
        }

        /// <summary>
        /// Gets type information for a command to aid in prediction.
        /// </summary>
        public static string GetCommandTypeInfo(string commandName)
        {
            if (methodCommands.TryGetValue(commandName, out MethodInfo methodInfo))
            {
                var parameters = methodInfo.GetParameters();
                if (parameters.Length == 0)
                    return "(method) returns " + FormatTypeName(methodInfo.ReturnType);
                
                var paramInfo = string.Join(", ", parameters.Select(p => $"{FormatTypeName(p.ParameterType)} {p.Name}"));
                return $"(method) ({paramInfo}) returns {FormatTypeName(methodInfo.ReturnType)}";
            }
            else if (propertyCommands.TryGetValue(commandName, out PropertyInfo propertyInfo))
            {
                return $"(property) {FormatTypeName(propertyInfo.PropertyType)}";
            }
            else if (fieldCommands.TryGetValue(commandName, out FieldInfo fieldInfo))
            {
                return $"(field) {FormatTypeName(fieldInfo.FieldType)}";
            }
            else if (delegateCommands.TryGetValue(commandName, out FieldInfo delegateField))
            {
                if (delegateField.FieldType.IsSubclassOf(typeof(Delegate)))
                {
                    var invokeMethod = delegateField.FieldType.GetMethod("Invoke");
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
        /// Predicts the next possible words in a command.
        /// </summary>
        public static string[] PredictNextWords(string currentInput)
        {
            var tokens = currentInput.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var predictions = new List<string>();
            
            // No tokens yet, suggest preface commands and "select"
            if (tokens.Length == 0)
            {
                predictions.AddRange(prefacecmds);
                predictions.Add("select");
                return predictions.ToArray();
            }
            
            // First token - suggest GameObject names or tags after @ or #
            if (tokens.Length == 1)
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
                    
                    foreach (var prefaceCmd in prefacecmds)
                    {
                        if (prefaceCmd.StartsWith(partialCommand) || 
                            (partialCommand.StartsWith(prefaceCmd) && partialCommand.Length > prefaceCmd.Length))
                        {
                            predictions.Add(prefaceCmd);
                        }
                    }
                    
                    if ("select".StartsWith(partialCommand))
                    {
                        predictions.Add("select");
                    }
                }
                
                return predictions.ToArray();
            }
            
            // Second token after a preface command or "select" - suggest console commands
            if (tokens.Length == 2 && (
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
            if (tokens.Length == 3 && consolecmds.Contains(tokens[1].ToLower()))
            {
                var consoleCmd = tokens[1].ToLower();
                var partialCommandName = tokens[2].ToLower();
                
                // For "get" and "set", suggest property and field commands
                if (consoleCmd == "get" || consoleCmd == "set")
                {
                    foreach (var cmd in propertyCommands.Keys.Concat(fieldCommands.Keys))
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
            if (tokens.Length == 4 && tokens[1].ToLower() == "set")
            {
                var commandName = tokens[2].ToLower();
                
                // For property commands
                if (propertyCommands.TryGetValue(commandName, out PropertyInfo propertyInfo))
                {
                    if (propertyInfo.PropertyType.IsEnum)
                    {
                        return GetEnumValues(propertyInfo.PropertyType);
                    }
                    else if (propertyInfo.PropertyType == typeof(bool))
                    {
                        return new[] { "true", "false" };
                    }
                }
                // For field commands
                else if (fieldCommands.TryGetValue(commandName, out FieldInfo fieldInfo))
                {
                    if (fieldInfo.FieldType.IsEnum)
                    {
                        return GetEnumValues(fieldInfo.FieldType);
                    }
                    else if (fieldInfo.FieldType == typeof(bool))
                    {
                        return new[] { "true", "false" };
                    }
                }
            }
            
            // For method or delegate parameters, check parameter types
            if (tokens.Length >= 4 && tokens[1].ToLower() == "call")
            {
                var commandName = tokens[2].ToLower();
                int paramIndex = tokens.Length - 4;
                
                // For method commands
                if (methodCommands.TryGetValue(commandName, out MethodInfo methodInfo))
                {
                    var parameters = methodInfo.GetParameters();
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
                else if (delegateCommands.TryGetValue(commandName, out FieldInfo delegateField) && 
                         delegateField.FieldType.IsSubclassOf(typeof(Delegate)))
                {
                    var invokeMethod = delegateField.FieldType.GetMethod("Invoke");
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
