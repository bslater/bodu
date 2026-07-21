// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlElement.SequenceEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Yaml.Document;

/// <summary>
/// Enumerates the elements of a YAML sequence node by walking its child chain.
/// </summary>
public readonly partial struct YamlElement
{
    /// <summary>
    /// Provides forward-only enumeration over the elements of a sequence node.
    /// </summary>
    public struct SequenceEnumerator
        : IEnumerable<YamlElement>
        , IEnumerator<YamlElement>
    {
        /// <summary>The owning document.</summary>
        private readonly YamlDocument _document;

        /// <summary>The resolved row index of the sequence being enumerated.</summary>
        private readonly int _sequenceIndex;

        /// <summary>The row index of the current element, or <c>-1</c> when none.</summary>
        private int _current;

        /// <summary>Indicates whether <see cref="MoveNext" /> has been called at least once.</summary>
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceEnumerator" /> struct.
        /// </summary>
        /// <param name="document">The owning document.</param>
        /// <param name="sequenceIndex">The resolved sequence row index.</param>
        internal SequenceEnumerator(YamlDocument document, int sequenceIndex)
        {
            _document = document;
            _sequenceIndex = sequenceIndex;
            _current = -1;
            _started = false;
        }

        /// <summary>
        /// Gets the element at the current position.
        /// </summary>
        /// <value>The current element.</value>
        public readonly YamlElement Current => new(_document, _current);

        /// <summary>
        /// Gets the element at the current position.
        /// </summary>
        /// <value>The current element, boxed.</value>
        readonly object IEnumerator.Current => Current;

        /// <summary>
        /// Returns this enumerator.
        /// </summary>
        /// <returns>A fresh enumerator positioned before the first element.</returns>
        public readonly SequenceEnumerator GetEnumerator() => new(_document, _sequenceIndex);

        /// <inheritdoc />
        readonly IEnumerator<YamlElement> IEnumerable<YamlElement>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Advances to the next element.
        /// </summary>
        /// <returns><see langword="true" /> when an element is available; otherwise <see langword="false" />.</returns>
        public bool MoveNext()
        {
            if (!_started)
            {
                _current = _document.FirstChild(_sequenceIndex);
                _started = true;
            }
            else if (_current >= 0)
            {
                _current = _document.NextSibling(_current);
            }

            return _current >= 0;
        }

        /// <summary>
        /// Resets the enumerator to its initial position.
        /// </summary>
        public void Reset()
        {
            _current = -1;
            _started = false;
        }

        /// <summary>
        /// Releases resources held by the enumerator.
        /// </summary>
        public readonly void Dispose()
        {
        }
    }
}
