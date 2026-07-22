// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Ini.Document;

/// <summary>
/// Represents a single element within an <see cref="IniDocument" /> — the root object, a section object, or a string
/// value — shaped after <c>System.Text.Json</c>'s <c>JsonElement</c>.
/// </summary>
/// <remarks>
/// The root object's properties are the document's hoisted global keys (string values) followed by its sections (object
/// values); a section object's properties are its key/value entries.
/// </remarks>
public readonly struct IniElement
{
    /// <summary>The sentinel section index that identifies the root scope.</summary>
    internal const int RootSectionIndex = -1;

    /// <summary>The sentinel entry index that identifies an object element rather than a value.</summary>
    internal const int ObjectEntryIndex = -1;

    /// <summary>The owning document.</summary>
    private readonly IniDocument _document;

    /// <summary>The section index, or <see cref="RootSectionIndex" /> for the root scope.</summary>
    private readonly int _section;

    /// <summary>The entry index within the scope, or <see cref="ObjectEntryIndex" /> for an object element.</summary>
    private readonly int _entry;

    /// <summary>
    /// Initializes a new instance of the <see cref="IniElement" /> struct.
    /// </summary>
    /// <param name="document">The owning document.</param>
    /// <param name="section">The section index, or <see cref="RootSectionIndex" /> for the root scope.</param>
    /// <param name="entry">The entry index, or <see cref="ObjectEntryIndex" /> for an object element.</param>
    internal IniElement(IniDocument document, int section, int entry)
    {
        _document = document;
        _section = section;
        _entry = entry;
    }

    /// <summary>
    /// Gets the value kind of this element.
    /// </summary>
    /// <value>
    /// <see cref="IniValueKind.Object" /> for the root or a section; otherwise <see cref="IniValueKind.String" />.
    /// </value>
    public IniValueKind ValueKind =>
        _entry == ObjectEntryIndex ? IniValueKind.Object : IniValueKind.String;

    /// <summary>
    /// Gets the string value of an <see cref="IniValueKind.String" /> element.
    /// </summary>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this element is not a string.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public string GetString()
    {
        if (_entry == ObjectEntryIndex)
            throw new InvalidOperationException();

        return _section == RootSectionIndex
            ? _document.GlobalValueAt(_entry)
            : _document.SectionValueAt(_section, _entry);
    }

    /// <summary>
    /// Gets the value element for the specified property name.
    /// </summary>
    /// <param name="name">
    /// The property name — a global key or section name on the root, or an entry key on a section.
    /// </param>
    /// <returns>The value element.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the property is not present.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public IniElement GetProperty(string name)
    {
        if (!TryGetProperty(name, out IniElement value))
            throw new KeyNotFoundException();

        return value;
    }

    /// <summary>
    /// Attempts to get the value element for the specified property name.
    /// </summary>
    /// <param name="name">
    /// The property name — a global key or section name on the root, or an entry key on a section.
    /// </param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the value element; otherwise the default element.
    /// </param>
    /// <returns><see langword="true" /> when the property is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public bool TryGetProperty(string name, out IniElement value)
    {
        ThrowHelper.ThrowIfNull(name);

        if (_entry != ObjectEntryIndex)
            throw new InvalidOperationException();

        if (_section == RootSectionIndex)
        {
            int global = _document.IndexOfGlobal(name);
            if (global >= 0)
            {
                value = new IniElement(_document, RootSectionIndex, global);
                return true;
            }

            int section = _document.IndexOfSection(name);
            if (section >= 0)
            {
                value = new IniElement(_document, section, ObjectEntryIndex);
                return true;
            }
        }
        else
        {
            int entry = _document.IndexOfEntry(_section, name);
            if (entry >= 0)
            {
                value = new IniElement(_document, _section, entry);
                return true;
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Enumerates the properties of an object element: for the root, the hoisted global keys followed by the sections;
    /// for a section, its key/value entries.
    /// </summary>
    /// <returns>An enumerator over the object's properties.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public ObjectEnumerator EnumerateObject()
    {
        if (_entry != ObjectEntryIndex)
            throw new InvalidOperationException();

        _document.ThrowIfDisposed();

        return new ObjectEnumerator(_document, _section);
    }

    /// <summary>
    /// Enumerates the properties of an <see cref="IniElement" /> object.
    /// </summary>
    public struct ObjectEnumerator
        : IEnumerator<IniProperty>, IEnumerable<IniProperty>
    {
        /// <summary>The owning document.</summary>
        private readonly IniDocument _document;

        /// <summary>The scope being enumerated: a section index, or <see cref="RootSectionIndex" /> for the root.</summary>
        private readonly int _scope;

        /// <summary>The current property index, starting at <c>-1</c> before the first move.</summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectEnumerator" /> struct.
        /// </summary>
        /// <param name="document">The owning document.</param>
        /// <param name="scope">The scope to enumerate: a section index, or <see cref="RootSectionIndex" />.</param>
        internal ObjectEnumerator(IniDocument document, int scope)
        {
            _document = document;
            _scope = scope;
            _index = -1;
        }

        /// <inheritdoc />
        public readonly IniProperty Current
        {
            get
            {
                if (_scope != RootSectionIndex)
                {
                    return new IniProperty(
                        _document.SectionKeyAt(_scope, _index),
                        new IniElement(_document, _scope, _index));
                }

                int globals = _document.GlobalCount;
                return _index < globals
                    ? new IniProperty(
                        _document.GlobalKeyAt(_index),
                        new IniElement(_document, RootSectionIndex, _index))
                    : new IniProperty(
                        _document.SectionNameAt(_index - globals),
                        new IniElement(_document, _index - globals, ObjectEntryIndex));
            }
        }

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            int count = _scope == RootSectionIndex
                ? _document.GlobalCount + _document.SectionCount
                : _document.SectionEntryCount(_scope);

            if (_index + 1 >= count)
                return false;

            _index++;
            return true;
        }

        /// <inheritdoc />
        public void Reset() => _index = -1;

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }

        /// <summary>
        /// Returns this enumerator.
        /// </summary>
        /// <returns>This enumerator.</returns>
        public readonly ObjectEnumerator GetEnumerator() => this;

        /// <inheritdoc />
        readonly IEnumerator<IniProperty> IEnumerable<IniProperty>.GetEnumerator() => this;

        /// <inheritdoc />
        readonly IEnumerator IEnumerable.GetEnumerator() => this;
    }
}
