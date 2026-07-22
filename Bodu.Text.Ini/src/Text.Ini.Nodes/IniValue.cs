// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini.Writer;

namespace Bodu.Text.Ini.Nodes;

/// <summary>
/// Represents a string-valued leaf in a mutable INI document tree.
/// </summary>
public sealed class IniValue
    : IniNode
{
    /// <summary>The string value.</summary>
    private string _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniValue" /> class with the supplied value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public IniValue(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        _value = value;
    }

    /// <inheritdoc />
    public override IniValueKind ValueKind => IniValueKind.String;

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
    public override IniNode DeepClone()
    {
        var clone = new IniValue(Value);
        foreach (string comment in LeadingComments)
            clone.LeadingComments.Add(comment);

        return clone;
    }

    /// <inheritdoc />
    public override void WriteTo(ref Utf8IniWriter writer) =>
        writer.WriteString(Value);
}
