// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlElement.MappingEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;

namespace Bodu.Text.Yaml.Document;

/// <summary>
/// Enumerates the key/value pairs of a YAML mapping node by walking its child chain.
/// </summary>
public readonly partial struct YamlElement
{
    /// <summary>
    /// Provides forward-only enumeration over the key/value pairs of a mapping node.
    /// </summary>
    public struct MappingEnumerator : IEnumerable<YamlProperty>, IEnumerator<YamlProperty>
    {
        /// <summary>The owning document.</summary>
        private readonly YamlDocument _document;

        /// <summary>The resolved row index of the mapping being enumerated.</summary>
        private readonly int _mappingIndex;

        /// <summary>The row index of the current pair, or <c>-1</c> when none.</summary>
        private int _current;

        /// <summary>Indicates whether <see cref="MoveNext" /> has been called at least once.</summary>
        private bool _started;

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingEnumerator" /> struct.
        /// </summary>
        /// <param name="document">The owning document.</param>
        /// <param name="mappingIndex">The resolved mapping row index.</param>
        internal MappingEnumerator(YamlDocument document, int mappingIndex)
        {
            _document = document;
            _mappingIndex = mappingIndex;
            _current = -1;
            _started = false;
        }

        /// <summary>
        /// Gets the key/value pair at the current position.
        /// </summary>
        /// <value>The current pair.</value>
        public readonly YamlProperty Current =>
            new(_document.GetKey(_current), new YamlElement(_document, _current));

        /// <summary>
        /// Gets the pair at the current position.
        /// </summary>
        /// <value>The current pair, boxed.</value>
        readonly object IEnumerator.Current => Current;

        /// <summary>
        /// Returns this enumerator.
        /// </summary>
        /// <returns>A fresh enumerator positioned before the first pair.</returns>
        public readonly MappingEnumerator GetEnumerator() => new(_document, _mappingIndex);

        /// <inheritdoc />
        readonly IEnumerator<YamlProperty> IEnumerable<YamlProperty>.GetEnumerator() => GetEnumerator();

        /// <inheritdoc />
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Advances to the next pair.
        /// </summary>
        /// <returns><see langword="true" /> when a pair is available; otherwise <see langword="false" />.</returns>
        public bool MoveNext()
        {
            if (!_started)
            {
                _current = _document.FirstChild(_mappingIndex);
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
