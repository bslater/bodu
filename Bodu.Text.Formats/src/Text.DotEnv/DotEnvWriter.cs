// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Text.DotEnv;

/// <summary>
/// Provides a forward-only, streaming writer for DotEnv-format output, writing key/value entries and comments to a
/// <see cref="TextWriter" /> without accumulating an in-memory <see cref="DotEnvDocument" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DotEnvWriter" /> is the streaming counterpart to <see cref="DotEnv.Format(DotEnvDocument)" />. Values are
/// quoted and escaped using the same rules as <see cref="DotEnv.Format(DotEnvDocument)" />: empty values are written as
/// <c>KEY=</c>, safe values are written unquoted, and all other values are double-quoted with escape sequences applied.
/// </para>
/// <para>
/// The writer takes ownership of the supplied <see cref="TextWriter" /> and disposes it when <see cref="Dispose" /> is
/// called.
/// </para>
/// <example>
/// <code>
///<![CDATA[
/// using var writer = DotEnv.CreateWriter(File.CreateText(".env"));
/// writer.WriteEntry("HOST", "localhost");
/// writer.WriteEntry("GREETING", "hello world");   // quoted automatically
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class DotEnvWriter
    : IDisposable
{
    private readonly TextWriter _writer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvWriter" /> class.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter" /> to write DotEnv text to. Owned by this instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public DotEnvWriter(TextWriter writer)
    {
        ThrowHelper.ThrowIfNull(writer);

        _writer = writer;
    }

    /// <summary>
    /// Writes a key/value entry, quoting and escaping the value as required.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The entry value.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteEntry(string key, string value)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _writer.Write(FormatEntry(key, value));
    }

    /// <summary>
    /// Writes a comment line using the supplied prefix character (<c>#</c> by default).
    /// </summary>
    /// <param name="text">The comment text, written after the prefix.</param>
    /// <param name="prefix">The comment prefix character. Defaults to <c>#</c>.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteComment(string text, char prefix = '#')
    {
        ThrowHelper.ThrowIfNull(text);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _writer.Write(prefix);
        _writer.Write(text);
        _writer.Write('\n');
    }

    /// <summary>
    /// Asynchronously writes a key/value entry, quoting and escaping the value as required.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The entry value.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
    /// <returns>A task that completes when the entry has been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public Task WriteEntryAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _writer.WriteAsync(FormatEntry(key, value).AsMemory(), cancellationToken);
    }

    /// <summary>
    /// Releases the underlying <see cref="TextWriter" />, flushing any buffered output first.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _writer.Dispose();
    }

    /// <summary>
    /// Builds the <c>KEY=VALUE</c> line (terminated by a line feed) for one entry, reusing the document formatter's
    /// quoting and escaping rules so streamed output matches <see cref="DotEnv.Format(DotEnvDocument)" />.
    /// </summary>
    private static string FormatEntry(string key, string value)
    {
        StringBuilder sb = new();
        sb.Append(key).Append('=');
        DotEnv.AppendFormattedValue(sb, value);
        sb.Append('\n');
        return sb.ToString();
    }
}
