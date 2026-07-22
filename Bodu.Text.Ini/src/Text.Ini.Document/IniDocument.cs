// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Ini.Reader;

namespace Bodu.Text.Ini.Document;

/// <summary>
/// Provides a read-only, pre-parsed view over an INI document, shaped after <c>System.Text.Json</c>'s
/// <c>JsonDocument</c>. The document is the normalized two-level object-of-objects: the root object exposes hoisted
/// global keys as string properties and sections as object properties. Comment trivia is not retained.
/// </summary>
/// <remarks>
/// The document owns a normalized store of decoded entries — with the duplicate-section and duplicate-key policies
/// already applied — and must be disposed when no longer needed; every <see cref="IniElement" /> obtained from it
/// becomes invalid once the document is disposed.
/// </remarks>
public sealed class IniDocument
    : IDisposable
{
    /// <summary>The normalized backing store.</summary>
    private readonly IniNormalizedDocument _store;

    /// <summary>Whether the document has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniDocument" /> class over the supplied normalized store.
    /// </summary>
    /// <param name="store">The normalized backing store.</param>
    private IniDocument(IniNormalizedDocument store)
    {
        _store = store;
    }

    /// <summary>
    /// Gets the root element, which is always an object.
    /// </summary>
    /// <value>The root <see cref="IniElement" />.</value>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    public IniElement RootElement
    {
        get
        {
            ThrowIfDisposed();

            return new IniElement(this, IniElement.RootSectionIndex, IniElement.ObjectEntryIndex);
        }
    }

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a read-only INI document using the default reader and document options.
    /// </summary>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="IniFormatException">Thrown when the bytes are not valid INI.</exception>
    public static IniDocument Parse(ReadOnlySpan<byte> utf8Ini) =>
        Parse(utf8Ini, IniReaderOptions.Default, IniDocumentOptions.Default);

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a read-only INI document using the supplied reader and document options.
    /// </summary>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <param name="readerOptions">The source-order reader options.</param>
    /// <param name="documentOptions">The document-model duplicate policies.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="IniFormatException">
    /// Thrown when the bytes are not valid INI, or when a duplicate section, duplicate key, or global-key/section-name
    /// collision violates the configured policies.
    /// </exception>
    public static IniDocument Parse(ReadOnlySpan<byte> utf8Ini, IniReaderOptions readerOptions, IniDocumentOptions documentOptions) =>
        new(IniNormalizedDocument.Parse(utf8Ini, readerOptions, documentOptions));

    /// <summary>
    /// Parses the supplied INI text into a read-only INI document.
    /// </summary>
    /// <param name="ini">The INI source text.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="ini" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="IniFormatException">Thrown when the text is not valid INI.</exception>
    public static IniDocument Parse(string ini)
    {
        ThrowHelper.ThrowIfNull(ini);

        return Parse(Encoding.UTF8.GetBytes(ini));
    }

    /// <summary>
    /// Releases the document's backing store. Elements obtained from this document must not be used afterward.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        _store.GlobalEntries.Clear();
        _store.Sections.Clear();
    }

    /// <summary>
    /// Gets the number of hoisted global entries.
    /// </summary>
    /// <value>The global entry count.</value>
    internal int GlobalCount => _store.GlobalEntries.Count;

    /// <summary>
    /// Gets the number of sections.
    /// </summary>
    /// <value>The section count.</value>
    internal int SectionCount => _store.Sections.Count;

    /// <summary>
    /// Gets the key of the global entry at the specified index.
    /// </summary>
    /// <param name="index">The zero-based global entry index.</param>
    /// <returns>The entry key.</returns>
    internal string GlobalKeyAt(int index)
    {
        ThrowIfDisposed();

        return _store.GlobalEntries[index].Key;
    }

    /// <summary>
    /// Gets the value of the global entry at the specified index.
    /// </summary>
    /// <param name="index">The zero-based global entry index.</param>
    /// <returns>The entry value.</returns>
    internal string GlobalValueAt(int index)
    {
        ThrowIfDisposed();

        return _store.GlobalEntries[index].Value;
    }

    /// <summary>
    /// Gets the name of the section at the specified index.
    /// </summary>
    /// <param name="index">The zero-based section index.</param>
    /// <returns>The section name.</returns>
    internal string SectionNameAt(int index)
    {
        ThrowIfDisposed();

        return _store.Sections[index].Name;
    }

    /// <summary>
    /// Gets the number of entries in the section at the specified index.
    /// </summary>
    /// <param name="section">The zero-based section index.</param>
    /// <returns>The section's entry count.</returns>
    internal int SectionEntryCount(int section)
    {
        ThrowIfDisposed();

        return _store.Sections[section].Entries.Count;
    }

    /// <summary>
    /// Gets the key of the specified section entry.
    /// </summary>
    /// <param name="section">The zero-based section index.</param>
    /// <param name="index">The zero-based entry index within the section.</param>
    /// <returns>The entry key.</returns>
    internal string SectionKeyAt(int section, int index)
    {
        ThrowIfDisposed();

        return _store.Sections[section].Entries[index].Key;
    }

    /// <summary>
    /// Gets the value of the specified section entry.
    /// </summary>
    /// <param name="section">The zero-based section index.</param>
    /// <param name="index">The zero-based entry index within the section.</param>
    /// <returns>The entry value.</returns>
    internal string SectionValueAt(int section, int index)
    {
        ThrowIfDisposed();

        return _store.Sections[section].Entries[index].Value;
    }

    /// <summary>
    /// Finds the index of the global entry with the specified key.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <returns>The zero-based global entry index, or <c>-1</c> when the key is absent.</returns>
    internal int IndexOfGlobal(string key)
    {
        ThrowIfDisposed();

        for (int i = 0; i < _store.GlobalEntries.Count; i++)
        {
            if (string.Equals(_store.GlobalEntries[i].Key, key, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the index of the section with the specified name.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <returns>The zero-based section index, or <c>-1</c> when the section is absent.</returns>
    internal int IndexOfSection(string name)
    {
        ThrowIfDisposed();

        for (int i = 0; i < _store.Sections.Count; i++)
        {
            if (string.Equals(_store.Sections[i].Name, name, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Finds the index of the entry with the specified key within a section.
    /// </summary>
    /// <param name="section">The zero-based section index.</param>
    /// <param name="key">The key name.</param>
    /// <returns>The zero-based entry index, or <c>-1</c> when the key is absent.</returns>
    internal int IndexOfEntry(int section, string key)
    {
        ThrowIfDisposed();

        List<KeyValuePair<string, string>> entries = _store.Sections[section].Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            if (string.Equals(entries[i].Key, key, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> when the document has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    internal void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
