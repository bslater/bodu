// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlScalarNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Represents a scalar value in the intermediate value tree, carrying the CLR value decoded once during parsing
/// alongside the <see cref="TomlTokenType" /> that classifies it.
/// </summary>
/// <remarks>
/// The boxed <see cref="Value" /> is one of <see cref="string" />, <see cref="long" />, <see cref="double" />,
/// <see cref="bool" />, <see cref="DateTimeOffset" />, <see cref="DateTime" />, <see cref="DateOnly" />, or
/// <see cref="TimeOnly" />, selected by <see cref="TokenType" />.
/// </remarks>
internal sealed class TomlScalarNode
    : TomlReaderNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlScalarNode" /> class.
    /// </summary>
    /// <param name="tokenType">The token type that classifies the scalar.</param>
    /// <param name="value">The decoded CLR value.</param>
    /// <param name="offset">The zero-based source character offset at which the scalar begins.</param>
    internal TomlScalarNode(TomlTokenType tokenType, object value, int offset)
        : base(offset)
    {
        TokenType = tokenType;
        Value = value;
    }

    /// <inheritdoc />
    internal override TomlReaderNodeKind Kind => TomlReaderNodeKind.Scalar;

    /// <summary>
    /// Gets the token type that classifies the scalar.
    /// </summary>
    /// <returns>The scalar's token type.</returns>
    internal TomlTokenType TokenType { get; }

    /// <summary>
    /// Gets the decoded CLR value of the scalar.
    /// </summary>
    /// <returns>The boxed scalar value.</returns>
    internal object Value { get; }
}
