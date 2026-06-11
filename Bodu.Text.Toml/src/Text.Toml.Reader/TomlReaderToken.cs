// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Represents a single entry in the flattened token stream a <see cref="TomlDocumentReader" /> advances over.
/// </summary>
/// <remarks>
/// Structural tokens (<see cref="TomlTokenType.StartTable" />, <see cref="TomlTokenType.EndTable" />,
/// <see cref="TomlTokenType.StartArray" />, and <see cref="TomlTokenType.EndArray" />) carry no value. A
/// <see cref="TomlTokenType.PropertyName" /> token carries its key as a <see cref="string" />. A scalar token carries
/// its decoded CLR value.
/// </remarks>
internal readonly struct TomlReaderToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlReaderToken" /> struct.
    /// </summary>
    /// <param name="tokenType">The kind of the token.</param>
    /// <param name="value">The value carried by the token, or <see langword="null" /> for a structural token.</param>
    /// <param name="offset">The zero-based source byte offset at which the token begins.</param>
    internal TomlReaderToken(TomlTokenType tokenType, object? value, int offset)
    {
        TokenType = tokenType;
        Value = value;
        Offset = offset;
    }

    /// <summary>
    /// Gets the kind of the token.
    /// </summary>
    /// <returns>The token kind.</returns>
    internal TomlTokenType TokenType { get; }

    /// <summary>
    /// Gets the value carried by the token.
    /// </summary>
    /// <returns>
    /// The key for a property-name token, the decoded value for a scalar token, or <see langword="null" /> for a
    /// structural token.
    /// </returns>
    internal object? Value { get; }

    /// <summary>
    /// Gets the zero-based source byte offset at which the token begins.
    /// </summary>
    /// <returns>The source offset.</returns>
    internal int Offset { get; }
}
