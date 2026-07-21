// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.DotEnv.Writer;

namespace Bodu.Text.DotEnv.Nodes;

/// <summary>
/// Represents a string-valued leaf in a mutable DotEnv document tree.
/// </summary>
public sealed class DotEnvValue
    : DotEnvNode
{
    /// <summary>The string value.</summary>
    private string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvValue" /> class with the supplied value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public DotEnvValue(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        _value = value;
    }

    /// <inheritdoc />
    public override DotEnvValueKind ValueKind => DotEnvValueKind.String;

    /// <summary>
    /// Gets or sets the string value.
    /// </summary>
    /// <value>The value; never <see langword="null" />.</value>
    /// <exception cref="ArgumentNullException">Thrown when the assigned value is <see langword="null" />.</exception>
    public string Value
    {
        get => _value;
        set
        {
            ThrowHelper.ThrowIfNull(value);
            _value = value;
        }
    }

    /// <inheritdoc />
    public override DotEnvNode DeepClone() =>
        new DotEnvValue(Value);

    /// <inheritdoc />
    public override void WriteTo(ref Utf8DotEnvWriter writer) =>
        writer.WriteString(Value);
}
