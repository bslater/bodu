// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Provides a forward-only, streaming reader for INI-format text, surfacing one key/value entry at a time from a
/// <see cref="TextReader" /> without loading the entire source into memory.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IniReader" /> is the streaming counterpart to <see cref="Ini.Parse(ReadOnlySpan{char})" />. It is suited
/// for large files or network streams where materialising a complete <see cref="IniDocument" /> is impractical. For
/// smaller, in-memory inputs, <see cref="Ini.Parse(ReadOnlySpan{char})" /> is simpler.
/// </para>
/// <para>
/// Call <see cref="Read" /> (or <see cref="ReadAsync" />) repeatedly to advance through key/value entries. When it
/// returns <see langword="true" />, <see cref="Section" />, <see cref="Key" />, and <see cref="Value" /> reflect the
/// current entry; when it returns <see langword="false" />, the input is exhausted.
/// </para>
/// <para>
/// Because the reader is forward-only, it surfaces entries in source order and does <b>not</b> apply the document-level
/// policies of <see cref="Ini.Parse(ReadOnlySpan{char})" /> — duplicate-key resolution (
/// <see cref="IniParseOptions.DuplicateKeyBehavior" />) and section merging (
/// <see cref="IniParseOptions.DuplicateSectionBehavior" />) require the whole document and are therefore the parser's
/// responsibility, not the reader's. Comments and blank lines are skipped. Malformed section headers, empty keys, and
/// disallowed global keys are still reported as <see cref="IniFormatException" /> exactly as during parsing.
/// </para>
/// <example>
/// <code>
///<![CDATA[
/// using var reader = Ini.CreateReader(File.OpenText("config.ini"));
/// while (reader.Read())
/// {
///     Console.WriteLine($"[{reader.Section}] {reader.Key} = {reader.Value}");
/// }
///]]>
/// </code>
/// </example>
/// <para>
/// The reader takes ownership of the supplied <see cref="TextReader" /> and disposes it when <see cref="Dispose" /> is
/// called.
/// </para>
/// </remarks>
public sealed class IniReader : IDisposable
{
    private readonly TextReader _reader;
    private readonly IniParseOptions _options;

    private string _section = string.Empty;
    private string _key = string.Empty;
    private string _value = string.Empty;
    private int _lineNumber;
    private bool _inGlobal = true;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniReader" /> class with default options.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader" /> to read INI text from. Owned by this instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public IniReader(TextReader reader)
        : this(reader, IniParseOptions.Default) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniReader" /> class with the specified options.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader" /> to read INI text from. Owned by this instance.</param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public IniReader(TextReader reader, IniParseOptions options)
    {
        ThrowHelper.ThrowIfNull(reader);

        _reader = reader;
        _options = options;
    }

    /// <summary>
    /// Gets the name of the section that the current entry belongs to, or <see cref="string.Empty" /> for a global
    /// entry that precedes the first section header.
    /// </summary>
    /// <returns>The current section name, or an empty string for the global section.</returns>
    public string Section => _section;

    /// <summary>
    /// Gets the key of the current entry. Valid only when the most recent call to <see cref="Read" /> or
    /// <see cref="ReadAsync" /> returned <see langword="true" />.
    /// </summary>
    /// <returns>The current entry key.</returns>
    public string Key => _key;

    /// <summary>
    /// Gets the value of the current entry. Valid only when the most recent call to <see cref="Read" /> or
    /// <see cref="ReadAsync" /> returned <see langword="true" />.
    /// </summary>
    /// <returns>The current entry value, or an empty string for a key-only line.</returns>
    public string Value => _value;

    /// <summary>
    /// Gets the 1-based line number of the line that produced the current entry. Increments as lines are consumed.
    /// </summary>
    /// <returns>The current line number, starting at 1.</returns>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Advances the reader to the next key/value entry, skipping comments and blank lines.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when an entry was read and <see cref="Key" />/<see cref="Value" /> reflect it;
    /// <see langword="false" /> when the input is exhausted.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <exception cref="IniFormatException">
    /// Thrown when a section header is malformed, a key is empty, or a global key appears while
    /// <see cref="IniParseOptions.AllowGlobalSection" /> is <see langword="false" />.
    /// </exception>
    public bool Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string? line;
        while ((line = _reader.ReadLine()) is not null)
        {
            _lineNumber++;
            if (ProcessLine(line.AsSpan()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Asynchronously advances the reader to the next key/value entry, skipping comments and blank lines.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the read operation.</param>
    /// <returns>
    /// A task that completes with <see langword="true" /> when an entry was read and <see cref="Key" />/
    /// <see cref="Value" /> reflect it; <see langword="false" /> when the input is exhausted.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the reader has been disposed.</exception>
    /// <exception cref="IniFormatException">
    /// Thrown when a section header is malformed, a key is empty, or a global key appears while
    /// <see cref="IniParseOptions.AllowGlobalSection" /> is <see langword="false" />.
    /// </exception>
    public async ValueTask<bool> ReadAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string? line;
        while ((line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            _lineNumber++;
            if (ProcessLine(line.AsSpan()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Releases the underlying <see cref="TextReader" />.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _reader.Dispose();
    }

    /// <summary>
    /// Processes a single raw source line, updating the reader's state. Comments, blank lines, and section headers do
    /// not produce an entry; key/value lines populate <see cref="_key" /> and <see cref="_value" />.
    /// </summary>
    /// <param name="rawLine">The raw line text, excluding the line terminator.</param>
    /// <returns>
    /// <see langword="true" /> when the line produced a key/value entry; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="IniFormatException">
    /// Thrown when a section header is malformed, a key is empty, or a global key is disallowed.
    /// </exception>
    private bool ProcessLine(ReadOnlySpan<char> rawLine)
    {
        ReadOnlySpan<char> line = rawLine.Trim();

        if (line.IsEmpty)
            return false;

        if (line[0] is ';' or '#')
            return false;

        if (line[0] == '[')
        {
            var closeBracket = line.IndexOf(']');

            if (closeBracket < 0)
                Ini.ThrowMalformedSectionHeader(_lineNumber);

            ReadOnlySpan<char> nameSpan = line[1..closeBracket].Trim();

            if (nameSpan.IsEmpty)
                Ini.ThrowMalformedSectionHeader(_lineNumber);

            _section = nameSpan.ToString();
            _inGlobal = false;
            return false;
        }

        if (_inGlobal && !_options.AllowGlobalSection)
            Ini.ThrowGlobalKeyDisallowed(_lineNumber);

        var sepIdx = -1;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] is '=' or ':')
            {
                sepIdx = i;
                break;
            }
        }

        if (sepIdx < 0)
        {
            _key = line.ToString();
            _value = string.Empty;
        }
        else
        {
            _key = line[..sepIdx].TrimEnd().ToString();
            _value = line[(sepIdx + 1)..].TrimStart().ToString();
        }

        if (_key.Length == 0)
            Ini.ThrowMissingKey(_lineNumber);

        return true;
    }
}
