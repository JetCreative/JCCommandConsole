using System;

namespace JetCreative.CommandConsolePro
{
    /// <summary>
    /// Represents a custom attribute that can be applied to methods, properties, fields, and delegates to mark them as
    /// commands usable in Command Console Pro. This attribute allows the element to be
    /// registered as a command during runtime.
    /// </summary>
    /// <remarks>
    /// Elements decorated with this attribute can be invoked as commands in the console.
    /// The command name can be explicitly defined when applying the attribute or,
    /// if omitted, defaults to the element's name in lowercase.
    /// 
    /// Usage examples:
    /// - Methods: Use "call MethodName [parameters]"
    /// - Properties: Use "get PropertyName" or "set PropertyName value"
    /// - Fields: Use "get FieldName" or "set FieldName value"
    /// - Delegates: Use "call DelegateName [parameters]"
    /// 
    /// To target specific GameObjects:
    /// - @ObjectName - Targets GameObject with the specified name
    /// - @@ObjectName - Targets all GameObjects with the specified name
    /// - #TagName - Targets GameObject with the specified tag
    /// - ##TagName - Targets all GameObjects with the specified tag
    /// - select - Targets the currently selected GameObject in the editor
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Delegate, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the command associated with the element. This property is defined in the
        /// <see cref="CommandAttribute"/> and is used to identify the element as a console command.
        /// If no value is explicitly provided, the name of the element is used as the default command name.
        /// </summary>
        public string CommandName { get; private set; }
        
        /// <summary>
        /// Override the "includePrivate" setting in the Command Cache to force private members to be included in the command cache.
        /// </summary>
        public bool IncludePrivate { get; private set; }

        /// <summary>
        /// Attribute to mark methods, properties, fields, and delegates as console commands that can be executed by JCCommandConsolePro.
        /// </summary>
        /// <param name="commandName">Optional custom name for the command. If null, the element's name will be used.
        /// The stored command name will always be lower case.</param>
        /// <param name="includePrivate">Optionally force including private access when generating the cache.</param>
        /// <remarks>
        /// Elements decorated with this attribute can be identified and invoked as commands in the console system.
        /// The <c>CommandName</c> property allows assigning an optional name for the command,
        /// which can be different from the element's name.
        /// </remarks>
        public CommandAttribute(string commandName = null, bool includePrivate = false)
        {
            CommandName = commandName?.ToLower();
            IncludePrivate = includePrivate;
        }
    }
}