// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginMissingAttributeException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Thrown by <see cref="ExternalPluginLoader" /> when a trusted plugin assembly loads successfully but does not expose a valid
/// <see cref="NotableDatePluginAttribute" /> naming a type that implements <see cref="INotableDatePlugin" />.
/// </summary>
public sealed class PluginMissingAttributeException
    : NotableDatePluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class.
    /// </summary>
    public PluginMissingAttributeException()
        : base()
    {
        AssemblyPath = string.Empty;
        Reason = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class with the specified error message.
    /// </summary>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    public PluginMissingAttributeException(string message)
        : base(message)
    {
        AssemblyPath = string.Empty;
        Reason = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class with the specified error message
    /// and a reference to the inner exception that caused this exception.
    /// </summary>
    /// <param name="message">
    /// The message that describes the error.
    /// </param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception.
    /// </param>
    public PluginMissingAttributeException(string message, Exception innerException)
        : base(message, innerException)
    {
        AssemblyPath = string.Empty;
        Reason = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class for a plugin assembly
    /// that failed attribute validation.
    /// </summary>
    /// <param name="assemblyPath">
    /// The path of the plugin assembly that failed the attribute check.
    /// </param>
    /// <param name="reason">
    /// A human-readable explanation of why the attribute check failed, such as a missing attribute, an unresolved
    /// plugin type, or a declared type that does not implement <see cref="INotableDatePlugin" />.
    /// </param>
    /// <remarks>
    /// This constructor preserves the assembly path and validation reason so callers can inspect the failed plugin
    /// candidate in addition to the formatted exception message.
    /// </remarks>
    public PluginMissingAttributeException(string assemblyPath, string reason)
        : base($"Plugin assembly '{assemblyPath}' is missing a valid NotableDatePluginAttribute: {reason}.")
    {
        AssemblyPath = assemblyPath;
        Reason = reason;
    }

    /// <summary>
    /// Gets the path of the plugin assembly that failed the attribute check.
    /// </summary>
    /// <value>
    /// The filesystem path supplied at construction, or <see cref="string.Empty" /> when the exception was created
    /// without assembly-path context.
    /// </value>
    public string AssemblyPath { get; }

    /// <summary>
    /// Gets the description of the specific attribute-validation fault.
    /// </summary>
    /// <value>
    /// A human-readable explanation of why the attribute check failed, or <see cref="string.Empty" /> when the
    /// exception was created without validation-failure context.
    /// </value>
    public string Reason { get; }
}
