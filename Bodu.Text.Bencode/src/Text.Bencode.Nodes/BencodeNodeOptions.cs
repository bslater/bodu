// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNodeOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Nodes;

/// <summary>
/// Defines the customizations applied to a mutable Bencode (BEP 3) node tree.
/// </summary>
/// <remarks>
/// The options govern in-memory behaviour only; they do not affect serialization, which always emits canonical Bencode
/// with dictionary keys in ascending bytewise order regardless of the configured comparison.
/// </remarks>
public struct BencodeNodeOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether <see cref="BencodeObject" /> property-name lookups ignore case.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when property-name lookups ignore case; otherwise <see langword="false" />.
    /// </value>
    /// <returns>
    /// <see langword="true" /> when <see cref="BencodeObject" /> property-name lookups ignore case; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool PropertyNameCaseInsensitive { get; set; }
}
