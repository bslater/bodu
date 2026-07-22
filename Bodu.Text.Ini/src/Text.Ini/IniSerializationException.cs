// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Represents an error that occurs while mapping a .NET object to or from INI with <see cref="IniSerializer" />, such
/// as an unsupported root type, a member nested beyond INI's two levels, a missing required key, or a value that cannot
/// be converted to the target type.
/// </summary>
public sealed class IniSerializationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IniSerializationException" /> class.
    /// </summary>
    public IniSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniSerializationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public IniSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniSerializationException" /> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public IniSerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
