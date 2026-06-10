// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlScalarSyntax.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization.Syntax;

namespace Bodu.Text.Serialization.Toml.Syntax;

/// <summary>
/// Represents a TOML scalar value — a string, integer, float, Boolean, or one of the four RFC 3339 date-time kinds. The
/// decoded value is boxed and the node's <see cref="TomlSyntaxNode.Kind" /> identifies which scalar it is.
/// </summary>
public sealed class TomlScalarSyntax
    : TomlSyntaxNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlScalarSyntax" /> class.
    /// </summary>
    /// <param name="kind">The scalar kind.</param>
    /// <param name="value">The decoded scalar value.</param>
    /// <param name="span">The span the scalar covers in the source.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public TomlScalarSyntax(TomlSyntaxKind kind, object value, SourceSpan span)
        : base(kind, span)
    {
        ThrowHelper.ThrowIfNull(value);
        Value = value;
    }

    /// <summary>
    /// Gets the decoded scalar value, boxed. The runtime type corresponds to <see cref="TomlSyntaxNode.Kind" />:
    /// <see cref="string" />, <see cref="long" />, <see cref="double" />, <see cref="bool" />,
    /// <see cref="System.DateTimeOffset" />, <see cref="System.DateTime" />, <see cref="System.DateOnly" />, or
    /// <see cref="System.TimeOnly" />.
    /// </summary>
    /// <returns>The boxed scalar value.</returns>
    public object Value { get; }

    /// <inheritdoc />
    public override IEnumerable<SyntaxNode> ChildNodesAndTokens() => [];
}
