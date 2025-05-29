using System;
using System.Collections.Generic;
using System.Reflection;
using JetCreative.Serialization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace JetCreative.CommandConsolePro
{
    public class CommandCache : ScriptableSingleton<CommandCache>
    {

        #region Settings

        public bool IncludePrivateMembers = false;
        public bool IncludeExampleCommands = true;
        public List<string> IncludeNamespaces = new List<string> { "JetCreative" };
        public List<string> ExcludeNamespaces = new List<string> { "UnityEngine.Internal", "System.Runtime" };
        //public string NewNamespace = "";
        public bool ShowIncludeNamespaces = true;
        public bool ShowExcludeNamespaces = true;

        #endregion

        #region Command Cache

        [Serializable] public class MethodDictionary : SerializedDictionary<string, SerializableMethodInfo> { }
        [Serializable] public class PropertyDictionary : SerializedDictionary<string, SerializedPropertyInfo> { }
        [Serializable] public class FieldDictionary : SerializedDictionary<string, SerializedFieldInfo> { }
        [Serializable] public class DelegateDictionary : SerializedDictionary<string, SerializedFieldInfo> { }
        [Serializable] public class CommandDeclarationTypes : SerializedDictionary<string, Type> { }
        [Serializable] public class CommandStatic : SerializedDictionary<string, bool> { }
        
        
        /// <summary>
        /// Dictionary of method commands, keyed by command name
        /// </summary>
        public MethodDictionary MethodCommands = new ();
        
        public int MethodCount;
        
        /// <summary>
        /// Dictionary of property commands with getters, keyed by command name
        /// </summary>
        public PropertyDictionary PropertyGetCommands = new (); 
        
        /// <summary>
        /// Dictionary of property commands with setters, keyed by command name
        /// </summary>
        public PropertyDictionary PropertySetCommands = new (); 
        
        /// <summary>
        /// Dictionary of field commands, keyed by command name
        /// </summary>
        public FieldDictionary FieldCommands = new (); 
        
        /// <summary>
        /// Dictionary of delegate commands, keyed by command name
        /// </summary>
        public DelegateDictionary DelegateCommands = new (); 
        
        /// <summary>
        /// Dictionary mapping commands to their declaring types, used for static command execution
        /// </summary>
        public CommandDeclarationTypes CommandDeclaringTypes = new (); 
        
        /// <summary>
        /// Stores whether each command is static or instance-based
        /// </summary>
        public CommandStatic IsCommandStatic = new ();
        

        #endregion
        
    }
}