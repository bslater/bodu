// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginMissingAttributeException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// The exception thrown when a candidate assembly does not declare a <see cref="NotableDatePluginAttribute" />.
/// </summary>
public sealed class PluginMissingAttributeException
    : NotableDatePluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="assemblyName">The name of the assembly missing the attribute.</param>
    public PluginMissingAttributeException(string message, string assemblyName)
        : base(message)
    {
        AssemblyName = assemblyName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    public PluginMissingAttributeException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class with an inner exception.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The underlying cause.</param>
    public PluginMissingAttributeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginMissingAttributeException" /> class.
    /// </summary>
    public PluginMissingAttributeException()
        : base()
    {
    }

    /// <summary>
    /// Gets the name of the assembly missing the attribute.
    /// </summary>
    /// <value>The assembly name.</value>
    public string AssemblyName { get; }
}
