// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlScalarWriterNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Represents a scalar value buffered in the <see cref="Utf8TomlWriter" /> value tree, pairing the decoded CLR value
/// with the <see cref="TomlTokenType" /> that selects how it is formatted.
/// </summary>
/// <remarks>
/// The boxed <see cref="Value" /> is one of <see cref="string" />, <see cref="long" />, <see cref="double" />,
/// <see cref="bool" />, <see cref="DateTimeOffset" />, <see cref="DateTime" />, <see cref="DateOnly" />, or
/// <see cref="TimeOnly" />, selected by <see cref="TokenType" />.
/// </remarks>
internal sealed class TomlScalarWriterNode
    : TomlWriterNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlScalarWriterNode" /> class.
    /// </summary>
    /// <param name="tokenType">The token type that classifies the scalar.</param>
    /// <param name="value">The decoded CLR value to format.</param>
    internal TomlScalarWriterNode(TomlTokenType tokenType, object value)
    {
        TokenType = tokenType;
        Value = value;
    }

    /// <summary>
    /// Gets the token type that classifies the scalar and selects its TOML formatting.
    /// </summary>
    /// <returns>The scalar's token type.</returns>
    internal TomlTokenType TokenType { get; }

    /// <summary>
    /// Gets the decoded CLR value of the scalar.
    /// </summary>
    /// <returns>The boxed scalar value.</returns>
    internal object Value { get; }
}
