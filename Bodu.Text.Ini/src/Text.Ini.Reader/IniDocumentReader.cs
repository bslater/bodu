// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniDocumentReader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini.Reader;

/// <summary>
/// Provides a forward-only cursor over the normalized token stream of a parsed INI document: a root object whose
/// properties are the hoisted global keys followed by the sections, each section an object of key/value entries. The
/// reader is a <see langword="ref struct" />, so it cannot be boxed or captured.
/// </summary>
/// <remarks>
/// <para>
/// INI cannot be normalized in a single forward pass when duplicate sections merge — a later <c>[section]</c> appends
/// to an earlier one, declaring structure out of source order. The constructor therefore parses the entire document up
/// front, applying the <see cref="IniDocumentOptions" /> duplicate policies, and <see cref="Read" /> walks the
/// materialized store. A malformed document or policy violation raises <see cref="IniFormatException" /> from the
/// constructor rather than from <see cref="Read" />. This type walks a normalized document;
/// <see cref="Utf8IniReader" /> reads the UTF-8 source in document order.
/// </para>
/// <para>
/// The stream is <see cref="IniTokenType.StartObject" />, the global <see cref="IniTokenType.PropertyName" /> /
/// <see cref="IniTokenType.String" /> pairs, then for each section a <see cref="IniTokenType.PropertyName" /> followed
/// by a nested <see cref="IniTokenType.StartObject" /> … <see cref="IniTokenType.EndObject" />, and a closing
/// <see cref="IniTokenType.EndObject" />. Comments are not surfaced.
/// </para>
/// </remarks>
public ref struct IniDocumentReader
{
    /// <summary>The normalized store the cursor walks.</summary>
    private readonly IniNormalizedDocument _document;

    /// <summary>The cursor's position in the token stream.</summary>
    private State _state;

    /// <summary>The section index the cursor is positioned in, when walking sections.</summary>
    private int _section;

    /// <summary>The entry index within the current scope.</summary>
    private int _entry;

    /// <summary>The kind of the current token.</summary>
    private IniTokenType _tokenType;

    /// <summary>The text of the current token, or <see langword="null" /> for a structural token.</summary>
    private string? _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniDocumentReader" /> struct over the supplied bytes using the
    /// default reader and document options.
    /// </summary>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <exception cref="IniFormatException">Thrown when the bytes are not valid INI.</exception>
    public IniDocumentReader(ReadOnlySpan<byte> utf8Ini)
        : this(utf8Ini, IniReaderOptions.Default, IniDocumentOptions.Default)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IniDocumentReader" /> struct over the supplied bytes using the
    /// supplied reader and document options.
    /// </summary>
    /// <param name="utf8Ini">The INI source bytes.</param>
    /// <param name="readerOptions">The source-order reader options.</param>
    /// <param name="documentOptions">The document-model duplicate policies.</param>
    /// <exception cref="IniFormatException">
    /// Thrown when the bytes are not valid INI, or when a duplicate section, duplicate key, or global-key/section-name
    /// collision violates the configured policies.
    /// </exception>
    public IniDocumentReader(ReadOnlySpan<byte> utf8Ini, IniReaderOptions readerOptions, IniDocumentOptions documentOptions)
    {
        _document = IniNormalizedDocument.Parse(utf8Ini, readerOptions, documentOptions);
        _state = State.Start;
        _section = 0;
        _entry = 0;
        _tokenType = IniTokenType.None;
        _current = null;
    }

    /// <summary>
    /// Gets the kind of the current token.
    /// </summary>
    /// <value>
    /// The current token kind, or <see cref="IniTokenType.None" /> before the first or after the last token.
    /// </value>
    public readonly IniTokenType TokenType => _tokenType;

    /// <summary>
    /// Gets the current container nesting depth.
    /// </summary>
    /// <value>The depth: zero at the document root, one inside a section object.</value>
    public readonly int CurrentDepth =>
        _state is State.EntryKey or State.EntryValue ? 1 : 0;

    /// <summary>
    /// Gets the text of the current token — a key name, section name, or string value.
    /// </summary>
    /// <returns>The token text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current token has no text.</exception>
    public readonly string GetString() =>
        _current ?? throw new InvalidOperationException();

    /// <summary>
    /// Advances the reader to the next token.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when a token was read; <see langword="false" /> at the end of the document.
    /// </returns>
    public bool Read()
    {
        while (true)
        {
            switch (_state)
            {
                case State.Start:
                    _state = State.GlobalKey;
                    _entry = 0;
                    return SetToken(IniTokenType.StartObject, null);

                case State.GlobalKey:
                    if (_entry < _document.GlobalEntries.Count)
                    {
                        _state = State.GlobalValue;
                        return SetToken(IniTokenType.PropertyName, _document.GlobalEntries[_entry].Key);
                    }

                    _section = 0;
                    _state = State.SectionKey;
                    continue;

                case State.GlobalValue:
                    string globalValue = _document.GlobalEntries[_entry].Value;
                    _entry++;
                    _state = State.GlobalKey;
                    return SetToken(IniTokenType.String, globalValue);

                case State.SectionKey:
                    if (_section < _document.Sections.Count)
                    {
                        _state = State.SectionStart;
                        return SetToken(IniTokenType.PropertyName, _document.Sections[_section].Name);
                    }

                    _state = State.End;
                    return SetToken(IniTokenType.EndObject, null);

                case State.SectionStart:
                    _entry = 0;
                    _state = State.EntryKey;
                    return SetToken(IniTokenType.StartObject, null);

                case State.EntryKey:
                    if (_entry < _document.Sections[_section].Entries.Count)
                    {
                        _state = State.EntryValue;
                        return SetToken(IniTokenType.PropertyName, _document.Sections[_section].Entries[_entry].Key);
                    }

                    _section++;
                    _state = State.SectionKey;
                    return SetToken(IniTokenType.EndObject, null);

                case State.EntryValue:
                    string entryValue = _document.Sections[_section].Entries[_entry].Value;
                    _entry++;
                    _state = State.EntryKey;
                    return SetToken(IniTokenType.String, entryValue);

                default:
                    _tokenType = IniTokenType.None;
                    _current = null;
                    return false;
            }
        }
    }

    /// <summary>
    /// Sets the current token and reports that a token was produced.
    /// </summary>
    /// <param name="tokenType">The kind of the token.</param>
    /// <param name="text">The token text, or <see langword="null" /> for a structural token.</param>
    /// <returns>Always <see langword="true" />.</returns>
    private bool SetToken(IniTokenType tokenType, string? text)
    {
        _tokenType = tokenType;
        _current = text;
        return true;
    }

    /// <summary>
    /// Identifies the cursor's position in the normalized token stream.
    /// </summary>
    private enum State
    {
        /// <summary>
        /// Before the first token; the next token is the root <see cref="IniTokenType.StartObject" />.
        /// </summary>
        Start,

        /// <summary>
        /// Positioned to emit the next global key, or move on to the sections.
        /// </summary>
        GlobalKey,

        /// <summary>
        /// Positioned to emit the value of the current global key.
        /// </summary>
        GlobalValue,

        /// <summary>
        /// Positioned to emit the next section's name, or the closing root <see cref="IniTokenType.EndObject" />.
        /// </summary>
        SectionKey,

        /// <summary>
        /// Positioned to emit the current section's <see cref="IniTokenType.StartObject" />.
        /// </summary>
        SectionStart,

        /// <summary>
        /// Positioned to emit the next entry key of the current section, or its <see cref="IniTokenType.EndObject" />.
        /// </summary>
        EntryKey,

        /// <summary>
        /// Positioned to emit the value of the current entry key.
        /// </summary>
        EntryValue,

        /// <summary>
        /// After the last token.
        /// </summary>
        End,
    }
}
