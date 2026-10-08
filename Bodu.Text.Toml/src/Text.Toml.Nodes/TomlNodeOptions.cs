// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNodeOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Nodes;

/// <summary>
/// Defines the customizations applied to a mutable TOML node tree.
/// </summary>
/// <remarks>
/// The options govern in-memory behaviour only; they do not affect serialization, which always emits normalized TOML
/// with table members in insertion order regardless of the configured comparison.
/// </remarks>
public struct TomlNodeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="TomlObject" /> property-name lookups ignore case.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when property-name lookups ignore case; otherwise <see langword="false" />.
    /// </value>
    /// <remarks>
    /// TOML keys are case-sensitive, so a valid document can hold two keys in one table that differ only in case, such
    /// as <c>a</c> and <c>A</c>. A table that ignores case cannot hold both, so
    /// <see cref="TomlNode.Parse(ReadOnlySpan{byte}, TomlNodeOptions)" /> throws <see cref="TomlFormatException" />,
    /// positioned where the second of the two keys starts, rather than keep only one of their values.
    /// </remarks>
    public bool PropertyNameCaseInsensitive { get; set; }
}
