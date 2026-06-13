// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializationException.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// The exception thrown when a value cannot be bound to or from a TOML document during serialization — for example a
/// type mismatch, a missing required member, or a value TOML cannot represent.
/// </summary>
/// <remarks>
/// This exception reports a binding-level failure and is distinct from <see cref="TomlFormatException" />, which
/// reports that the source text was not well-formed TOML. When the failure can be traced to a member, the
/// <see cref="Path" /> carries the dotted path to it; when it can be traced to a position in the source, the
/// <see cref="Offset" /> carries the byte offset.
/// </remarks>
public sealed class TomlSerializationException
    : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializationException" /> class.
    /// </summary>
    public TomlSerializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializationException" /> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public TomlSerializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializationException" /> class with the specified message and
    /// a reference to the inner exception that caused it.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public TomlSerializationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlSerializationException" /> class with the specified message and
    /// source offset.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="offset">The byte offset into the source at which the error was detected.</param>
    public TomlSerializationException(string message, int offset)
        : base(message)
    {
        Offset = offset;
    }

    /// <summary>
    /// Gets the byte offset into the source at which the error was detected, when available.
    /// </summary>
    /// <returns>The byte offset, or <see langword="null" /> when the failure has no associated position.</returns>
    public int? Offset { get; internal set; }

    /// <summary>
    /// Gets the dotted path to the member whose binding failed, for example <c>server.endpoints[0].timeout</c>, when
    /// available.
    /// </summary>
    /// <returns>The member path, or <see langword="null" /> when the failure is not associated with a member.</returns>
    public string? Path { get; internal set; }

    /// <summary>
    /// Prepends a path segment to an existing member path, joining a key segment with a dot and an array-index segment
    /// (which already begins with <c>[</c>) directly.
    /// </summary>
    /// <param name="segment">The parent segment to prepend, a member or dictionary key, or an array index of the form <c>[i]</c>.</param>
    /// <param name="childPath">The already-accumulated child path, or <see langword="null" /> when none.</param>
    /// <returns>The combined path.</returns>
    internal static string CombinePath(string segment, string? childPath)
    {
        if (string.IsNullOrEmpty(childPath))
            return segment;

        return childPath[0] == '[' ? segment + childPath : segment + "." + childPath;
    }
}
