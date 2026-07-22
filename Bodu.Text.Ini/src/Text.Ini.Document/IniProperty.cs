// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Document;

/// <summary>
/// Represents a single name/value property of an <see cref="IniElement" /> object — a global key, a section, or a
/// section entry.
/// </summary>
public readonly struct IniProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IniProperty" /> struct.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value element.</param>
    internal IniProperty(string name, IniElement value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the property name — a global key, a section name, or a section entry key.
    /// </summary>
    /// <value>The name.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the property value element — a string for a global key or section entry, or an object for a section.
    /// </summary>
    /// <value>The value element.</value>
    public IniElement Value { get; }
}
