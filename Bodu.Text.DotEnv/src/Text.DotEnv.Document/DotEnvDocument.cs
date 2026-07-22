// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvDocument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.DotEnv.Reader;

namespace Bodu.Text.DotEnv.Document;

/// <summary>
/// Provides a read-only, forward-parsed view over a DotEnv document, shaped after <c>System.Text.Json</c>'s
/// <c>JsonDocument</c>. The document is a flat object of string-valued keys; comment trivia is not retained.
/// </summary>
/// <remarks>
/// The document owns a flat store of decoded entries and must be disposed when no longer needed; every
/// <see cref="DotEnvElement" /> obtained from it becomes invalid once the document is disposed.
/// </remarks>
public sealed class DotEnvDocument
    : IDisposable
{
    /// <summary>The decoded key/value entries in source order.</summary>
    private readonly List<KeyValuePair<string, string>> _entries;

    /// <summary>Whether the document has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvDocument" /> class over the supplied entries.
    /// </summary>
    /// <param name="entries">The decoded key/value entries in source order.</param>
    private DotEnvDocument(List<KeyValuePair<string, string>> entries)
    {
        _entries = entries;
    }

    /// <summary>
    /// Gets the root element, which is always an object.
    /// </summary>
    /// <value>The root <see cref="DotEnvElement" />.</value>
    /// <exception cref="ObjectDisposedException">Thrown when the document has been disposed.</exception>
    public DotEnvElement RootElement
    {
        get
        {
            ThrowIfDisposed();

            return new DotEnvElement(this, DotEnvElement.RootIndex);
        }
    }

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a read-only DotEnv document.
    /// </summary>
    /// <param name="utf8DotEnv">The DotEnv source bytes.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="DotEnvFormatException">Thrown when the bytes are not valid DotEnv.</exception>
    public static DotEnvDocument Parse(ReadOnlySpan<byte> utf8DotEnv) =>
        Parse(utf8DotEnv, DotEnvReaderOptions.Default);

    /// <summary>
    /// Parses the supplied UTF-8 bytes into a read-only DotEnv document using the supplied reader options.
    /// </summary>
    /// <param name="utf8DotEnv">The DotEnv source bytes.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="DotEnvFormatException">Thrown when the bytes are not valid DotEnv.</exception>
    public static DotEnvDocument Parse(ReadOnlySpan<byte> utf8DotEnv, DotEnvReaderOptions options)
    {
        var reader = new Utf8DotEnvReader(utf8DotEnv, options);
        var entries = new List<KeyValuePair<string, string>>();
        string? pendingKey = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DotEnvTokenType.PropertyName:
                    pendingKey = reader.GetString();
                    break;

                case DotEnvTokenType.String:
                    entries.Add(new KeyValuePair<string, string>(pendingKey!, reader.GetString()));
                    pendingKey = null;
                    break;

                default:
                    break;
            }
        }

        return new DotEnvDocument(entries);
    }

    /// <summary>
    /// Parses the supplied DotEnv text into a read-only DotEnv document.
    /// </summary>
    /// <param name="dotEnv">The DotEnv source text.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dotEnv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="DotEnvFormatException">Thrown when the text is not valid DotEnv.</exception>
    public static DotEnvDocument Parse(string dotEnv)
    {
        ThrowHelper.ThrowIfNull(dotEnv);

        return Parse(Encoding.UTF8.GetBytes(dotEnv));
    }

    /// <summary>
    /// Releases the document's backing store. Elements obtained from this document must not be used afterward.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        _entries.Clear();
    }

    /// <summary>
    /// Gets the number of entries in the document.
    /// </summary>
    /// <returns>The entry count.</returns>
    internal int EntryCount => _entries.Count;

    /// <summary>
    /// Gets the key of the entry at the specified index.
    /// </summary>
    /// <param name="index">The zero-based entry index.</param>
    /// <returns>The entry key.</returns>
    internal string KeyAt(int index)
    {
        ThrowIfDisposed();

        return _entries[index].Key;
    }

    /// <summary>
    /// Gets the value of the entry at the specified index.
    /// </summary>
    /// <param name="index">The zero-based entry index.</param>
    /// <returns>The entry value.</returns>
    internal string ValueAt(int index)
    {
        ThrowIfDisposed();

        return _entries[index].Value;
    }

    /// <summary>
    /// Finds the index of the first entry with the specified key.
    /// </summary>
    /// <param name="key">The key name.</param>
    /// <returns>The zero-based entry index, or <c>-1</c> when the key is absent.</returns>
    internal int IndexOf(string key)
    {
        ThrowIfDisposed();

        for (int i = 0; i < _entries.Count; i++)
        {
            if (string.Equals(_entries[i].Key, key, StringComparison.Ordinal))
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
