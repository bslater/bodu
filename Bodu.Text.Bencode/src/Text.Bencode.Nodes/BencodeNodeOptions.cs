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
    /// <remarks>
    /// Bencode dictionary keys are byte strings compared exactly, so a dictionary can hold two keys that differ only in
    /// case, such as <c>A</c> and <c>a</c>. An object that ignores case cannot hold both, so
    /// <see cref="BencodeNode.Parse(ReadOnlySpan{byte}, BencodeNodeOptions)" /> throws
    /// <see cref="BencodeFormatException" /> at the second of the two keys rather than keep only one of their values.
    /// </remarks>
    public bool PropertyNameCaseInsensitive { get; set; }
}
