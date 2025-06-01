using System;
using System.Collections.Generic;
using JetCreative.Serialization;
using UnityEditor;

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Singleton ScriptableObject that stores cached command information for the Command Console Pro system.
    /// This cache is generated at edit-time and serialized to avoid expensive reflection operations at runtime.
    /// </summary>
    /// <remarks>
    /// <para>The CommandCache contains:</para>
    /// <list type="bullet">
    /// <item><description>Method commands - callable methods with parameters</description></item>
    /// <item><description>Property commands - properties with getters and/or setters</description></item>
    /// <item><description>Field commands - accessible fields</description></item>
    /// <item><description>Delegate commands - invokable delegates and events</description></item>
    /// <item><description>Type information for static vs instance commands</description></item>
    /// </list>
    /// 
    /// <para>Cache Generation:</para>
    /// The cache is typically generated through the Console Pro Editor window or automatically
    /// when the console system initializes. It scans all loaded assemblies for members marked
    /// with the [Command] attribute and stores their reflection information in a serialized format.
    /// 
    /// <para>Performance:</para>
    /// By pre-caching command information, the system avoids expensive reflection calls during
    /// runtime command execution, resulting in better performance and reduced GC allocation.
    /// </remarks>
    public class CommandCache : ScriptableSingleton<CommandCache>
    {

        #region Settings

        /// <summary>
        /// Whether to include private members when scanning for commands.
        /// When false, only public members are included in the command cache.
        /// </summary>
        public bool IncludePrivateMembers = false;
        
        /// <summary>
        /// Whether to include example commands from the ConsoleExampleCommands class.
        /// Useful for testing and demonstrating console functionality.
        /// </summary>
        public bool IncludeExampleCommands = true;
        
        /// <summary>
        /// List of namespaces to include when scanning for commands.
        /// If this list is not empty, only commands in these namespaces will be cached.
        /// Leave empty to include all namespaces (subject to exclusion list).
        /// </summary>
        public List<string> IncludeNamespaces = new List<string> { "JetCreative" };
        
        /// <summary>
        /// List of namespaces to exclude when scanning for commands.
        /// Commands in these namespaces will be ignored during cache generation.
        /// </summary>
        public List<string> ExcludeNamespaces = new List<string> { "UnityEngine.Internal", "System.Runtime" };
        
        /// <summary>
        /// UI state for showing/hiding the include namespaces list in the editor.
        /// </summary>
        public bool ShowIncludeNamespaces = true;
        
        /// <summary>
        /// UI state for showing/hiding the exclude namespaces list in the editor.
        /// </summary>
        public bool ShowExcludeNamespaces = true;

        #endregion

        #region Command Cache

        /// <summary>
        /// Serializable dictionary for storing method command information.
        /// </summary>
        [Serializable] public class MethodDictionary : SerializedDictionary<string, SerializableMethodInfo> { }
        
        /// <summary>
        /// Serializable dictionary for storing property command information.
        /// </summary>
        [Serializable] public class PropertyDictionary : SerializedDictionary<string, SerializedPropertyInfo> { }
        
        /// <summary>
        /// Serializable dictionary for storing field command information.
        /// </summary>
        [Serializable] public class FieldDictionary : SerializedDictionary<string, SerializedFieldInfo> { }
        
        /// <summary>
        /// Serializable dictionary for storing delegate command information.
        /// </summary>
        [Serializable] public class DelegateDictionary : SerializedDictionary<string, SerializedFieldInfo> { }
        
        /// <summary>
        /// Serializable dictionary for storing command declaration types.
        /// </summary>
        [Serializable] public class CommandDeclarationTypes : SerializedDictionary<string, Type> { }
        
        /// <summary>
        /// Serializable dictionary for storing static/instance flags.
        /// </summary>
        [Serializable] public class CommandStatic : SerializedDictionary<string, bool> { }
        
        
        /// <summary>
        /// Dictionary of method commands, keyed by command name.
        /// Contains serialized method information for callable commands.
        /// </summary>
        public MethodDictionary MethodCommands = new ();
        
        /// <summary>
        /// Dictionary of property commands with getters, keyed by command name.
        /// Used for 'get property_name' style commands.
        /// </summary>
        public PropertyDictionary PropertyGetCommands = new (); 
        
        /// <summary>
        /// Dictionary of property commands with setters, keyed by command name.
        /// Used for 'set property_name value' style commands.
        /// </summary>
        public PropertyDictionary PropertySetCommands = new (); 
        
        /// <summary>
        /// Dictionary of field commands, keyed by command name.
        /// Contains serialized field information for field access commands.
        /// </summary>
        public FieldDictionary FieldCommands = new (); 
        
        /// <summary>
        /// Dictionary of delegate commands, keyed by command name.
        /// Contains serialized field information for delegate/event commands.
        /// </summary>
        public DelegateDictionary DelegateCommands = new (); 
        
        /// <summary>
        /// Dictionary mapping commands to their declaring types, used for static command execution.
        /// Essential for determining which type to invoke static members on.
        /// </summary>
        public CommandDeclarationTypes CommandDeclaringTypes = new (); 
        
        /// <summary>
        /// Stores whether each command is static or instance-based.
        /// Used to determine if a target object is required for command execution.
        /// </summary>
        public CommandStatic IsCommandStatic = new ();
        
        /// <summary>
        /// Gets the total number of cached commands across all categories.
        /// </summary>
        /// <returns>Total count of all cached commands</returns>
        public int GetTotalCommandCount()
        {
            return MethodCommands.Count + 
                   PropertyGetCommands.Count + 
                   PropertySetCommands.Count + 
                   FieldCommands.Count + 
                   DelegateCommands.Count;
        }
        
        /// <summary>
        /// Checks if the command cache is empty (no commands have been cached).
        /// </summary>
        /// <returns>True if the cache is empty, false otherwise</returns>
        public bool IsEmpty()
        {
            return GetTotalCommandCount() == 0;
        }

        #endregion
        
    }
}