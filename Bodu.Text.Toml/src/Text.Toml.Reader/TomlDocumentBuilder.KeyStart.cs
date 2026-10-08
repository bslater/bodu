// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentBuilder.KeyStart.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

internal sealed partial class TomlDocumentBuilder
{
    /// <summary>
    /// Represents the position at which a key/value pair's key starts.
    /// </summary>
    /// <param name="Line">The 1-based line number of the key's first token.</param>
    /// <param name="Column">The 1-based column number of the key's first token.</param>
    /// <param name="Offset">The zero-based byte offset of the key's first token.</param>
    private readonly record struct KeyStart(int Line, int Column, int Offset)
    {
        /// <summary>
        /// Creates a <see cref="TomlFormatException" /> positioned at the start of the key.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>The exception to throw.</returns>
        public TomlFormatException Error(string message) =>
            new(message, Line, Column, Offset);
    }
}
