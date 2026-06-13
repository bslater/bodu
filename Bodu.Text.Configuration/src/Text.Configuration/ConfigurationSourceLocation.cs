// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationSourceLocation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;

namespace Bodu.Text.Configuration;

/// <summary>
/// Identifies a specific position in a configuration source document so that diagnostics, exceptions, and model
/// elements can point back to the originating line and column.
/// </summary>
/// <remarks>
/// <para>
/// Locations are 1-based for both <see cref="LineNumber" /> and <see cref="LinePosition" />, matching the convention
/// used by most editors and diagnostic UIs. A <see cref="Length" /> of zero indicates an unsized point; non-zero
/// indicates a span that begins at <see cref="LinePosition" /> and runs for <see cref="Length" /> characters within the
/// same line.
/// </para>
/// <para>
/// <see cref="Path" /> is set only when the configuration document was loaded from a file. Documents parsed from
/// strings expose <see langword="null" /> here.
/// </para>
/// <para>
/// <see cref="None" /> is the canonical "unknown" location and compares equal to a default-constructed instance. The
/// type is a <see langword="readonly struct" /> and is safe to pass by value across diagnostic and exception
/// boundaries; <see cref="ToString" /> renders the location in <c>line N, column M</c> form suitable for log output and
/// IDE error lists.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Construct a location pointing at a specific span on line 12.
/// var loc = new ConfigurationSourceLocation(
///     lineNumber:   12,
///     linePosition: 5,
///     length:       8,
///     path:         "app.ini");
/// Console.WriteLine(loc);   // "app.ini(12,5): length 8"
///
/// // Surface from a parse exception in editor-style form.
/// try { ConfigurationDocument.Parse(text); }
/// catch (ConfigurationParseException ex)
/// {
///     ConfigurationSourceLocation where = ex.Location;
///     if (!where.Equals(ConfigurationSourceLocation.None))
///         Console.WriteLine($"{where}: {ex.Message}");
/// }
///]]>
/// </example>
[DebuggerDisplay("Line {LineNumber}, Col {LinePosition}, Len {Length}")]
public readonly struct ConfigurationSourceLocation
    : IEquatable<ConfigurationSourceLocation>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSourceLocation" /> struct with the specified line
    /// number, column, span length, and optional file path.
    /// </summary>
    /// <param name="lineNumber">The 1-based line number.</param>
    /// <param name="linePosition">The 1-based column within the line.</param>
    /// <param name="length">The length of the span in characters.</param>
    /// <param name="path">The optional source file path.</param>
    public ConfigurationSourceLocation(int lineNumber, int linePosition, int length, string? path = null)
    {
        LineNumber = lineNumber;
        LinePosition = linePosition;
        Length = length;
        Path = path;
    }

    /// <summary>
    /// Gets the 1-based line number that identifies the line in the source document.
    /// </summary>
    /// <returns>A positive line number, or zero when the location is unknown.</returns>
    public int LineNumber { get; }

    /// <summary>
    /// Gets the 1-based column within <see cref="LineNumber" /> at which the span starts.
    /// </summary>
    /// <returns>A positive column number, or zero when the column is unknown.</returns>
    public int LinePosition { get; }

    /// <summary>
    /// Gets the length of the span, measured in characters within the same line.
    /// </summary>
    /// <returns>A non-negative count; zero indicates a point location.</returns>
    public int Length { get; }

    /// <summary>
    /// Gets the source file path that produced this location, or <see langword="null" /> when the document was parsed
    /// from an in-memory string.
    /// </summary>
    /// <returns>The optional source path.</returns>
    public string? Path { get; }

    /// <summary>
    /// Gets a location that represents an unknown position.
    /// </summary>
    /// <returns>An empty location whose numeric fields are all zero.</returns>
    public static ConfigurationSourceLocation None => default;

    /// <summary>
    /// Determines whether two locations refer to the same position in the same source.
    /// </summary>
    /// <param name="other">The location to compare with this instance.</param>
    /// <returns><see langword="true" /> if every field matches; otherwise, <see langword="false" />.</returns>
    public bool Equals(ConfigurationSourceLocation other) =>
        LineNumber == other.LineNumber
        && LinePosition == other.LinePosition
        && Length == other.Length
        && string.Equals(Path, other.Path, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is ConfigurationSourceLocation other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(LineNumber, LinePosition, Length, Path);

    /// <summary>
    /// Returns a human-readable rendering of the location, suitable for inclusion in diagnostic messages.
    /// </summary>
    /// <returns>A string of the form <c>line N, column M</c>, optionally prefixed with the path.</returns>
    public override string ToString()
    {
        if (LineNumber == 0 && LinePosition == 0)
            return "<unknown>";

        var core = string.Format(
            CultureInfo.InvariantCulture,
            "line {0}, column {1}",
            LineNumber,
            LinePosition);

        return Path is null ? core : string.Concat(Path, ": ", core);
    }

    /// <summary>
    /// Determines whether two locations are equal.
    /// </summary>
    /// <param name="left">The first location to compare.</param>
    /// <param name="right">The second location to compare.</param>
    /// <returns><see langword="true" /> if the locations are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(ConfigurationSourceLocation left, ConfigurationSourceLocation right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two locations are not equal.
    /// </summary>
    /// <param name="left">The first location to compare.</param>
    /// <param name="right">The second location to compare.</param>
    /// <returns><see langword="true" /> if the locations differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(ConfigurationSourceLocation left, ConfigurationSourceLocation right)
    {
        return !left.Equals(right);
    }
}
