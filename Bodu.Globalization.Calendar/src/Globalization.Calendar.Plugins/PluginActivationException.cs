// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginActivationException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Thrown by <see cref="ExternalPluginLoader" /> when the declared plugin type cannot be instantiated — typically because the
/// type lacks a public parameterless constructor, or because the constructor threw.
/// </summary>
public sealed class PluginActivationException
    : NotableDatePluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class.
    /// </summary>
    public PluginActivationException()
        : base()
    {
        AssemblyPath = string.Empty;
        PluginType = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class with the specified error message.
    /// </summary>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    public PluginActivationException(string message)
        : base(message)
    {
        AssemblyPath = string.Empty;
        PluginType = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class with the specified error message
    /// and a reference to the inner exception that caused this exception.
    /// </summary>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception.
    /// </param>
    public PluginActivationException(string message, Exception innerException)
        : base(message, innerException)
    {
        AssemblyPath = string.Empty;
        PluginType = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class for a failed plugin activation.
    /// </summary>
    /// <param name="assemblyPath">
    /// The path of the plugin assembly whose declared type could not be activated.
    /// </param>
    /// <param name="pluginType">
    /// The declared plugin type, if discoverable.
    /// </param>
    /// <param name="innerException">
    /// The underlying activation failure.
    /// </param>
    /// <remarks>
    /// This constructor preserves the assembly path and declared plugin type, allowing callers to inspect the failed
    /// activation target in addition to the formatted exception message.
    /// </remarks>
    public PluginActivationException(string assemblyPath, Type? pluginType, Exception innerException)
        : base($"Failed to activate plugin type '{pluginType?.FullName ?? "<unknown>"}' from assembly '{assemblyPath}': {innerException.Message}", innerException)
    {
        AssemblyPath = assemblyPath;
        PluginType = pluginType;
    }

    /// <summary>
    /// Gets the path of the plugin assembly whose declared type could not be activated.
    /// </summary>
    /// <value>
    /// The filesystem path supplied at construction, or <see cref="string.Empty" /> when the exception was created
    /// without assembly-path context.
    /// </value>
    public string AssemblyPath { get; }

    /// <summary>
    /// Gets the declared plugin type, if it could be resolved.
    /// </summary>
    /// <value>
    /// The CLR <see cref="Type" /> declared by the plugin's <see cref="NotableDatePluginAttribute" />, or
    /// <see langword="null" /> when the type could not be located or when the exception was created without
    /// plugin-type context.
    /// </value>
    public Type? PluginType { get; }
}
