// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PluginActivationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// The exception thrown when the declared plugin type cannot be activated or does not implement
/// <see cref="INotableDatePlugin" />.
/// </summary>
public sealed class PluginActivationException
    : NotableDatePluginException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="pluginType">The plugin type that could not be activated, if known.</param>
    public PluginActivationException(string message, Type? pluginType)
        : base(message)
    {
        PluginType = pluginType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class with an inner exception.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="pluginType">The plugin type that could not be activated, if known.</param>
    /// <param name="innerException">The underlying cause.</param>
    public PluginActivationException(string message, Type? pluginType, Exception innerException)
        : base(message, innerException)
    {
        PluginType = pluginType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    public PluginActivationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The underlying cause.</param>
    public PluginActivationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginActivationException" /> class.
    /// </summary>
    public PluginActivationException()
        : base()
    {
    }

    /// <summary>
    /// Gets the plugin type that could not be activated.
    /// </summary>
    /// <value>The plugin type, or <see langword="null" /> when not known.</value>
    public Type? PluginType { get; }
}
