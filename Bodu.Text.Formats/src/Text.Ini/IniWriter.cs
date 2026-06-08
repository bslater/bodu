// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniWriter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Provides a forward-only, streaming writer for INI-format output, writing section headers, key/value entries, and
/// comments to a <see cref="TextWriter" /> without accumulating an in-memory <see cref="IniDocument" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IniWriter" /> is the streaming counterpart to <see cref="Ini.Format(IniDocument)" />. It is suited for
/// generating large files or piping output to a network stream. Entries are written with a <c> = </c> separator and
/// section headers in <c>[name]</c> form, matching the output of <see cref="Ini.Format(IniDocument)" />.
/// </para>
/// <para>
/// The caller controls ordering: write any global entries first, then a <see cref="WriteSection" /> call followed by
/// that section's entries. The writer takes ownership of the supplied <see cref="TextWriter" /> and disposes it when
/// <see cref="Dispose" /> is called.
/// </para>
/// <example>
/// <code>
///<![CDATA[
/// using var writer = Ini.CreateWriter(File.CreateText("config.ini"));
/// writer.WriteSection("database");
/// writer.WriteEntry("host", "localhost");
/// writer.WriteEntry("port", "5432");
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class IniWriter : IDisposable
{
    private readonly TextWriter _writer;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniWriter" /> class.
    /// </summary>
    /// <param name="writer">The <see cref="TextWriter" /> to write INI text to. Owned by this instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public IniWriter(TextWriter writer)
    {
        ThrowHelper.ThrowIfNull(writer);

        _writer = writer;
    }

    /// <summary>
    /// Writes a section header in <c>[name]</c> form.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> is <see langword="null" />.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteSection(string name)
    {
        ThrowHelper.ThrowIfNull(name);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _writer.Write('[');
        _writer.Write(name);
        _writer.Write(']');
        _writer.Write('\n');
    }

    /// <summary>
    /// Writes a key/value entry in <c>key = value</c> form.
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

        _writer.Write(key);
        _writer.Write(" = ");
        _writer.Write(value);
        _writer.Write('\n');
    }

    /// <summary>
    /// Writes a comment line using the supplied prefix character (<c>;</c> or <c>#</c>).
    /// </summary>
    /// <param name="text">The comment text, written after the prefix.</param>
    /// <param name="prefix">The comment prefix character. Defaults to <c>;</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public void WriteComment(string text, char prefix = ';')
    {
        ThrowHelper.ThrowIfNull(text);
        ObjectDisposedException.ThrowIf(_disposed, this);

        _writer.Write(prefix);
        _writer.Write(text);
        _writer.Write('\n');
    }

    /// <summary>
    /// Asynchronously writes a section header in <c>[name]</c> form.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
    /// <returns>A task that completes when the header has been written.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="name" /> is <see langword="null" />.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public async Task WriteSectionAsync(string name, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(name);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _writer.WriteAsync($"[{name}]\n".AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously writes a key/value entry in <c>key = value</c> form.
    /// </summary>
    /// <param name="key">The entry key.</param>
    /// <param name="value">The entry value.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the write operation.</param>
    /// <returns>A task that completes when the entry has been written.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key" /> or <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the writer has been disposed.</exception>
    public async Task WriteEntryAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(value);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _writer.WriteAsync($"{key} = {value}\n".AsMemory(), cancellationToken).ConfigureAwait(false);
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
}
