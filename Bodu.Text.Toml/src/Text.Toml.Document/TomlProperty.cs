// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Document;

/// <summary>
/// Represents a single key/value pair within a table <see cref="TomlElement" />. Instances are produced by
/// <see cref="TomlElement.ObjectEnumerator" />.
/// </summary>
/// <remarks>
/// The <see cref="Value" /> is a view onto the owning <see cref="TomlDocument" /> and is valid only for that document's
/// lifetime; after the document is disposed, accessing the value throws <see cref="ObjectDisposedException" />.
/// </remarks>
public readonly struct TomlProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlProperty" /> struct.
    /// </summary>
    /// <param name="name">The property name, decoded from the table key.</param>
    /// <param name="value">The property value.</param>
    internal TomlProperty(string name, TomlElement value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the property name, decoded from the table key.
    /// </summary>
    /// <returns>The property name.</returns>
    public string Name { get; }

    /// <summary>
    /// Gets the property value.
    /// </summary>
    /// <returns>A <see cref="TomlElement" /> view of the value.</returns>
    public TomlElement Value { get; }

    /// <summary>
    /// Returns the property name.
    /// </summary>
    /// <returns>The value of <see cref="Name" />.</returns>
    public override string ToString() =>
        Name;
}
