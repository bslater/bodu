// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Represents any value in a TOML document. Serves as the abstract base for the concrete value kinds — strings,
/// integers, floats, booleans, the four date-time types, arrays, and tables.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/toml-value-model.svg" alt="The ten TOML value kinds in three groups. The four scalars are TomlString (string), TomlInteger (long), TomlFloat (double), and TomlBoolean (bool). The four RFC 3339 date-time kinds are TomlOffsetDateTime (DateTimeOffset), TomlLocalDateTime (DateTime with Unspecified kind), TomlLocalDate (DateOnly), and TomlLocalTime (TimeOnly). The two containers are TomlArray (an ordered list of TomlValue) and TomlTable (a string-keyed map to TomlValue). Every value derives from TomlValue and reports a TomlValueKind; scalars and date-times are immutable while containers are mutable."/>
/// </para>
/// <para>
/// <see cref="Kind" /> identifies the concrete subclass without forcing a cast, so callers can switch on the kind
/// before narrowing to a specific type, or pattern-match directly against a concrete subclass such as
/// <see cref="TomlString" /> or <see cref="TomlTable" />.
/// </para>
/// <para>
/// Scalar values are immutable. <see cref="TomlArray" /> and <see cref="TomlTable" /> are mutable containers so that a
/// document can be authored programmatically before being rendered by the TOML writer.
/// </para>
/// <para>
/// The hierarchy is closed: the constructor is inaccessible outside this assembly, so the only value kinds are the
/// built-in ones. This guarantees the writer can render every value it is given.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Dispatch on Kind, then narrow to the concrete type to read its Value.
/// TomlValue value = config["title"];
/// string title = value.Kind switch
/// {
///     TomlValueKind.String  => ((TomlString)value).Value,
///     TomlValueKind.Integer => ((TomlInteger)value).Value.ToString(),
///     _                     => value.ToString(),
/// };
///
/// // Or pattern-match directly against a concrete subclass.
/// if (config["ports"] is TomlArray ports)
///     Console.WriteLine($"{ports.Count} ports configured");
///]]>
/// </example>
public abstract class TomlValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlValue" /> class. The constructor is inaccessible outside this
    /// assembly, preventing external subclassing.
    /// </summary>
    private protected TomlValue()
    {
    }

    /// <summary>
    /// Gets the concrete kind of the value.
    /// </summary>
    /// <returns>The <see cref="TomlValueKind" /> of this value.</returns>
    public abstract TomlValueKind Kind { get; }
}
