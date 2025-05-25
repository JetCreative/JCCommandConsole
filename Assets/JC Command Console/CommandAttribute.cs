using System;

namespace JetCreative.Console
{
    /// <summary>
    /// Represents a custom attribute that can be applied to methods to mark them as
    /// commands usable in JC Command Console. This attribute allows the method to be
    /// registered as a command during runtime.
    /// </summary>
    /// <remarks>
    /// Methods decorated with this attribute can be invoked as commands in the console.
    /// The command name can be explicitly defined when applying the attribute or,
    /// if omitted, defaults to the method's name in lowercase.
    /// </remarks>
    /// <example>
    /// Use this attribute to annotate methods intended to serve as executable console commands.
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the command associated with the method. This property is defined in the
        /// <see cref="CommandAttribute"/> and is used to identify the method as a console command.
        /// If no value is explicitly provided, the name of the method is used as the default command name.
        /// </summary>
        public string CommandName { get; private set; }

        /// <summary>
        /// Attribute to mark methods as console commands that can be executed by the JCCommandConsole.
        /// </summary>
        /// <remarks>
        /// Methods decorated with this attribute can be identified and invoked as commands in the console system.
        /// The <c>CommandName</c> property allows assigning an optional name for the command,
        /// which can be different from the method's name.
        /// </remarks>
        public CommandAttribute(string commandName = null)
        {
            CommandName = commandName;
        }
    }
}