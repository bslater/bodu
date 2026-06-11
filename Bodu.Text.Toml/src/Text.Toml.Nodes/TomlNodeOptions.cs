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
/// The options govern in-memory behaviour only; they do not affect serialization, which always emits canonical TOML
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
    /// <returns>
    /// <see langword="true" /> when <see cref="TomlObject" /> property-name lookups ignore case; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool PropertyNameCaseInsensitive { get; set; }
}
