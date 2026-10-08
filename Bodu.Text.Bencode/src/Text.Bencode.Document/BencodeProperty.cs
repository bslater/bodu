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
/// <para>
/// A Bencode dictionary key is a byte string, and need not be text: a tracker's scrape response, for example, keys its
/// files by 20-byte info hashes. <see cref="Name" /> decodes the key as UTF-8 for convenience, so a key that is not
/// valid UTF-8 reads with U+FFFD in place of each invalid sequence and two such keys can share a name.
/// <see cref="GetNameBytes" /> and <see cref="NameEquals(ReadOnlySpan{byte})" /> read the key's exact bytes.
/// </para>
/// <para>
/// The <see cref="Value" /> and the key's bytes are views onto the owning <see cref="BencodeDocument" /> and are valid
/// only for that document's lifetime; after the document is disposed, reading them throws
/// <see cref="ObjectDisposedException" />. <see cref="Name" /> is decoded when the property is produced and stays
/// readable.
/// </para>
/// </remarks>
public readonly struct BencodeProperty
{
    /// <summary>The owning document, or <see langword="null" /> for the default instance.</summary>
    private readonly BencodeDocument? _document;

    /// <summary>The row index of the property's key within the owning document.</summary>
    private readonly int _keyRow;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeProperty" /> struct.
    /// </summary>
    /// <param name="name">The property name, decoded from the dictionary key as UTF-8 text.</param>
    /// <param name="document">The owning document.</param>
    /// <param name="keyRow">The row index of the property's key within <paramref name="document" />.</param>
    /// <param name="value">The property value.</param>
    internal BencodeProperty(string name, BencodeDocument document, int keyRow, BencodeElement value)
    {
        Name = name;
        _document = document;
        _keyRow = keyRow;
        Value = value;
    }

    /// <summary>
    /// Gets the property name, decoded from the dictionary key as UTF-8 text.
    /// </summary>
    /// <value>
    /// The property name. A key that is not valid UTF-8 decodes with U+FFFD in place of each invalid sequence; use
    /// <see cref="GetNameBytes" /> or <see cref="NameEquals(ReadOnlySpan{byte})" /> for the key's exact bytes.
    /// </value>
    public string Name { get; }

    /// <summary>
    /// Gets the property value.
    /// </summary>
    /// <value>A <see cref="BencodeElement" /> view of the value.</value>
    public BencodeElement Value { get; }

    /// <summary>
    /// Copies the dictionary key's raw bytes to a new array.
    /// </summary>
    /// <returns>A copy of the key's bytes, exactly as they appear in the document.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this property is the default value and belongs to no document.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public byte[] GetNameBytes() =>
        OwningDocument.GetKeySpan(_keyRow).ToArray();

    /// <summary>
    /// Determines whether the dictionary key's raw bytes equal the supplied bytes, without allocating.
    /// </summary>
    /// <param name="name">The key bytes to compare with.</param>
    /// <returns>
    /// <see langword="true" /> when the key's bytes equal <paramref name="name" />; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this property is the default value and belongs to no document.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public bool NameEquals(ReadOnlySpan<byte> name) =>
        OwningDocument.GetKeySpan(_keyRow).SequenceEqual(name);

    /// <summary>
    /// Returns the property name.
    /// </summary>
    /// <returns>The value of <see cref="Name" />.</returns>
    public override string ToString() =>
        Name;

    /// <summary>
    /// Gets the owning document.
    /// </summary>
    /// <value>The document the property belongs to.</value>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this property is the default value and belongs to no document.
    /// </exception>
    private BencodeDocument OwningDocument =>
        _document ?? throw new InvalidOperationException(BencodeResourceStrings.Op_Invalid_DefaultProperty);
}
