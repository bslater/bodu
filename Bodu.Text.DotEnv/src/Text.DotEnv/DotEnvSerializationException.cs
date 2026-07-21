// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

/// <summary>
/// Represents an error that occurs while mapping a .NET object to or from DotEnv with <see cref="DotEnvSerializer" />,
/// such as an unsupported root type, a missing required key, or a value that cannot be converted to the target type.
/// </summary>
public sealed class DotEnvSerializationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvSerializationException" /> class.
    /// </summary>
    public DotEnvSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvSerializationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public DotEnvSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvSerializationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public DotEnvSerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
