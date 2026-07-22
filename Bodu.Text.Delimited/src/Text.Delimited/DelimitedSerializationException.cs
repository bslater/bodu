// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Represents an error that occurs while mapping .NET records to or from delimited text with
/// <see cref="DelimitedSerializer" />, such as a non-collection root or a value that cannot be converted to the target
/// column type.
/// </summary>
public sealed class DelimitedSerializationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedSerializationException" /> class.
    /// </summary>
    public DelimitedSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedSerializationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DelimitedSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedSerializationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DelimitedSerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
