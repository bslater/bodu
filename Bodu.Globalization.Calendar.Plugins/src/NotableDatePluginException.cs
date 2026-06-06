// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDatePluginException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins;

/// <summary>
/// The base exception for failures encountered while loading a notable-date plugin.
/// </summary>
public class NotableDatePluginException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    public NotableDatePluginException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class with an inner exception.
    /// </summary>
    /// <param name="message">The message describing the failure.</param>
    /// <param name="innerException">The underlying cause.</param>
    public NotableDatePluginException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDatePluginException" /> class.
    /// </summary>
    public NotableDatePluginException()
        : base()
    {
    }
}
