// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// Base exception for failures raised by <see cref="ExternalPluginLoader" /> while loading or validating an external
/// notable- date plugin assembly.
/// </summary>
/// <remarks>
/// Derived exception types surface the specific failure mode: <see cref="PluginNotTrustedException" /> when the trust
/// policy rejects the candidate, <see cref="PluginMissingAttributeException" /> when the assembly does not carry a
/// valid <see cref="NotableDatePluginAttribute" />, and <see cref="PluginActivationException" /> when the declared
/// plugin type fails to instantiate.
/// </remarks>
public class NotableDatePluginException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class with a message.
    /// </summary>
    /// <param name="message">The exception message.</param>
    public NotableDatePluginException(string message)
        : base(message)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class with a message and an inner
    /// exception.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The underlying cause.</param>
    public NotableDatePluginException(string message, Exception innerException)
        : base(message, innerException)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class.
    /// </summary>
    public NotableDatePluginException()
        : base()
    { }
}
