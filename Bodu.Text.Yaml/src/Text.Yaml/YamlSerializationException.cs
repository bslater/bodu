// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml;

/// <summary>
/// The exception thrown when a value cannot be bound to or from a YAML document during serialization — for example a
/// type mismatch, a missing required member, or a value YAML cannot represent.
/// </summary>
/// <remarks>
/// This exception reports a binding-level failure and is distinct from <see cref="YamlFormatException" />, which
/// reports that the source text was not well-formed YAML. When the failure can be traced to a member, the
/// <see cref="Path" /> carries the dotted path to it; when it can be traced to a position in the source, the
/// <see cref="Offset" />, <see cref="LineNumber" />, and <see cref="ColumnNumber" /> carry the position.
/// </remarks>
public sealed class YamlSerializationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializationException" /> class.
    /// </summary>
    public YamlSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializationException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public YamlSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializationException" /> class with the specified message and
    /// a reference to the inner exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public YamlSerializationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlSerializationException" /> class with the specified message and
    /// source offset.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="offset">The byte offset into the source at which the error was detected.</param>
    public YamlSerializationException(string message, int offset)
        : base(message)
    {
        Offset = offset;
    }

    /// <summary>
    /// Gets the byte offset into the source at which the error was detected, when available.
    /// </summary>
    /// <value>The byte offset, or <see langword="null" /> when the failure has no associated position.</value>
    public int? Offset { get; internal set; }

    /// <summary>
    /// Gets the 1-based line number in the source at which the error was detected, when available.
    /// </summary>
    /// <value>The line number, or <see langword="null" /> when the failure has no associated position.</value>
    public int? LineNumber { get; internal set; }

    /// <summary>
    /// Gets the 1-based column number, in UTF-8 bytes from the start of the line, at which the error was detected, when
    /// available.
    /// </summary>
    /// <value>The column number, or <see langword="null" /> when the failure has no associated position.</value>
    public int? ColumnNumber { get; internal set; }

    /// <summary>
    /// Gets the dotted path to the member whose binding failed, for example <c>server.endpoints[0].timeout</c>, when
    /// available.
    /// </summary>
    /// <value>The member path, or <see langword="null" /> when the failure is not associated with a member.</value>
    public string? Path { get; internal set; }

    /// <summary>
    /// Prepends a path segment to an existing member path, joining a key segment with a dot and a sequence-index
    /// segment (which already begins with <c>[</c>) directly.
    /// </summary>
    /// <param name="segment">
    /// The parent segment to prepend, a member or mapping key, or a sequence index of the form <c>[i]</c>.
    /// </param>
    /// <param name="childPath">The already-accumulated child path, or <see langword="null" /> when none.</param>
    /// <returns>The combined path.</returns>
    internal static string CombinePath(string segment, string? childPath)
    {
        if (string.IsNullOrEmpty(childPath))
            return segment;

        return childPath[0] == '[' ? segment + childPath : segment + "." + childPath;
    }
}
