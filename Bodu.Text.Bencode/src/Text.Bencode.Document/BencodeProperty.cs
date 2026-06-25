// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeProperty.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Document;

/// <summary>
/// Represents a single key/value pair within an object <see cref="BencodeElement" />. Instances are produced by
/// <see cref="BencodeElement.ObjectEnumerator" />.
/// </summary>
/// <remarks>
/// The <see cref="Value" /> is a view onto the owning <see cref="BencodeDocument" /> and is valid only for that
/// document's lifetime; after the document is disposed, accessing the value throws
/// <see cref="ObjectDisposedException" />.
/// </remarks>
public readonly struct BencodeProperty
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeProperty" /> struct.
    /// </summary>
    /// <param name="name">The property name, decoded from the dictionary key as UTF-8 text.</param>
    /// <param name="value">The property value.</param>
    internal BencodeProperty(string name, BencodeElement value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>
    /// Gets the property name, decoded from the dictionary key as UTF-8 text.
    /// </summary>
    /// <value>The property name.</value>
    public string Name { get; }

    /// <summary>
    /// Gets the property value.
    /// </summary>
    /// <value>A <see cref="BencodeElement" /> view of the value.</value>
    public BencodeElement Value { get; }

    /// <summary>
    /// Returns the property name.
    /// </summary>
    /// <returns>The value of <see cref="Name" />.</returns>
    public override string ToString() =>
        Name;
}
