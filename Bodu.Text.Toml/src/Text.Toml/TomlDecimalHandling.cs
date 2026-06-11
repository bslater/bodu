// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDecimalHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Specifies how a <see cref="decimal" /> value is represented in TOML, which has no native decimal type.
/// </summary>
/// <remarks>
/// The setting controls only how a decimal is written; on read the serializer accepts a TOML float, integer, or string
/// regardless, so a document produced under one setting can be read under either.
/// </remarks>
public enum TomlDecimalHandling
{
    /// <summary>
    /// The decimal is written as a TOML float. Because TOML floats are IEEE 754 binary64 values, precision beyond
    /// roughly fifteen to seventeen significant digits is lost.
    /// </summary>
    Float = 0,

    /// <summary>
    /// The decimal is written as an invariant-culture TOML basic string, preserving its full precision and scale.
    /// </summary>
    String = 1,
}
