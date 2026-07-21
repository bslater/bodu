// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvElement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Bodu.Text.DotEnv.Document;

/// <summary>
/// Represents a single element — the root object or one of its string values — within a <see cref="DotEnvDocument" />,
/// shaped after <c>System.Text.Json</c>'s <c>JsonElement</c>.
/// </summary>
public readonly struct DotEnvElement
{
    /// <summary>The sentinel index that identifies the root object element.</summary>
    internal const int RootIndex = -1;

    /// <summary>The owning document.</summary>
    private readonly DotEnvDocument _document;

    /// <summary>The entry index, or <see cref="RootIndex" /> for the root object.</summary>
    private readonly int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotEnvElement" /> struct.
    /// </summary>
    /// <param name="document">The owning document.</param>
    /// <param name="index">The entry index, or <see cref="RootIndex" /> for the root object.</param>
    internal DotEnvElement(DotEnvDocument document, int index)
    {
        _document = document;
        _index = index;
    }

    /// <summary>
    /// Gets the value kind of this element.
    /// </summary>
    /// <value>
    /// <see cref="DotEnvValueKind.Object" /> for the root; otherwise <see cref="DotEnvValueKind.String" />.
    /// </value>
    public DotEnvValueKind ValueKind =>
        _index == RootIndex ? DotEnvValueKind.Object : DotEnvValueKind.String;

    /// <summary>
    /// Gets the string value of a <see cref="DotEnvValueKind.String" /> element.
    /// </summary>
    /// <returns>The string value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this element is not a string.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public string GetString()
    {
        if (_index == RootIndex)
            throw new InvalidOperationException();

        return _document.ValueAt(_index);
    }

    /// <summary>
    /// Gets the value element for the specified property name.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <returns>The value element.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="KeyNotFoundException">Thrown when the property is not present.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public DotEnvElement GetProperty(string name)
    {
        if (!TryGetProperty(name, out DotEnvElement value))
            throw new KeyNotFoundException();

        return value;
    }

    /// <summary>
    /// Attempts to get the value element for the specified property name.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the value element; otherwise the default element.
    /// </param>
    /// <returns><see langword="true" /> when the property is present; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public bool TryGetProperty(string name, out DotEnvElement value)
    {
        ThrowHelper.ThrowIfNull(name);

        if (_index != RootIndex)
            throw new InvalidOperationException();

        int found = _document.IndexOf(name);
        if (found < 0)
        {
            value = default;
            return false;
        }

        value = new DotEnvElement(_document, found);
        return true;
    }

    /// <summary>
    /// Enumerates the properties of an object element in source order.
    /// </summary>
    /// <returns>An enumerator over the object's properties.</returns>
    /// <exception cref="InvalidOperationException">Thrown when this element is not an object.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the owning document has been disposed.</exception>
    public ObjectEnumerator EnumerateObject()
    {
        if (_index != RootIndex)
            throw new InvalidOperationException();

        _document.ThrowIfDisposed();

        return new ObjectEnumerator(_document);
    }

    /// <summary>
    /// Enumerates the properties of a <see cref="DotEnvElement" /> object.
    /// </summary>
    public struct ObjectEnumerator
        : IEnumerator<DotEnvProperty>, IEnumerable<DotEnvProperty>
    {
        /// <summary>The owning document.</summary>
        private readonly DotEnvDocument _document;

        /// <summary>The current entry index, starting at <c>-1</c> before the first move.</summary>
        private int _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectEnumerator" /> struct.
        /// </summary>
        /// <param name="document">The owning document.</param>
        internal ObjectEnumerator(DotEnvDocument document)
        {
            _document = document;
            _index = -1;
        }

        /// <inheritdoc />
        public readonly DotEnvProperty Current =>
            new(_document.KeyAt(_index), new DotEnvElement(_document, _index));

        /// <inheritdoc />
        readonly object IEnumerator.Current => Current;

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_index + 1 >= _document.EntryCount)
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
        readonly IEnumerator<DotEnvProperty> IEnumerable<DotEnvProperty>.GetEnumerator() => this;

        /// <inheritdoc />
        readonly IEnumerator IEnumerable.GetEnumerator() => this;
    }
}
