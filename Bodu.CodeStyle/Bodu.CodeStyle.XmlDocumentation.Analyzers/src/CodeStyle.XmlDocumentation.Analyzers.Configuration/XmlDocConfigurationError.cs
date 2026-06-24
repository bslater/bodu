// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigurationError.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Bodu.CodeStyle.XmlDocumentation.Analyzers.Configuration;

/// <summary>
/// Describes a single configuration-loading failure surfaced from a <c>bodu.xmldocstyle.json</c> additional file,
/// pairing the diagnostic <see cref="Location" /> with a human-readable message.
/// </summary>
internal sealed class XmlDocConfigurationError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocConfigurationError" /> class.
    /// </summary>
    /// <param name="location">The location to attribute the error to (the configuration file).</param>
    /// <param name="message">The human-readable description of the failure.</param>
    public XmlDocConfigurationError(Location location, string message)
    {
        this.Location = location;
        this.Message = message;
    }

    /// <summary>
    /// Gets the location to attribute the error to.
    /// </summary>
    /// <value>The configuration file location.</value>
    public Location Location { get; }

    /// <summary>
    /// Gets the human-readable description of the failure.
    /// </summary>
    /// <value>The error message.</value>
    public string Message { get; }
}
