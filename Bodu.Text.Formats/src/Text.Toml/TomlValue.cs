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
