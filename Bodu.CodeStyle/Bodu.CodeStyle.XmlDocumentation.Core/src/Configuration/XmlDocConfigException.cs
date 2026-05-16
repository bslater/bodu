// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlDocConfigException.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.CodeStyle.XmlDocumentation.Configuration;

/// <summary>
/// Indicates that the contents of a <c>bodu.xmldocstyle.json</c> file are invalid or cannot be applied.
/// </summary>
public sealed class XmlDocConfigException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocConfigException" /> class.
    /// </summary>
    public XmlDocConfigException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocConfigException" /> class with a descriptive message.
    /// </summary>
    /// <param name="message">The descriptive error message.</param>
    public XmlDocConfigException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlDocConfigException" /> class with a descriptive message
    /// and an inner exception that triggered the failure.
    /// </summary>
    /// <param name="message">The descriptive error message.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public XmlDocConfigException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
