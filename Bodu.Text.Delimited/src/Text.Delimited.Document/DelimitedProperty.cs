// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Represents a single column-name/value field of a <see cref="DelimitedElement" /> record object.
/// </summary>
public readonly struct DelimitedProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DelimitedProperty" /> struct.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="value">The field value element.</param>
    internal DelimitedProperty(string name, DelimitedElement value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the column name.
    /// </summary>
    /// <value>The column name.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the field value element.
    /// </summary>
    /// <value>The value element.</value>
    public DelimitedElement Value { get; }
}
