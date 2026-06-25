// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeDocumentOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Document;

/// <summary>
/// Defines the customizations applied when parsing a <see cref="BencodeDocument" />.
/// </summary>
/// <remarks>
/// Bencode has no comment syntax and no trailing-comma concept; the options relax the two dictionary-key rules that
/// older real-world encoders are known to violate: <see cref="AllowUnsortedKeys" /> and
/// <see cref="AllowDuplicateKeys" />. Both default to the strict canonical behaviour.
/// </remarks>
public struct BencodeDocumentOptions
{
    /// <summary>
    /// Gets or sets the maximum container nesting depth the parser will accept.
    /// </summary>
    /// <value>The maximum container nesting depth; <c>0</c> selects the default of 64.</value>
    /// <remarks>
    /// The effective depth is clamped to the hard ceiling <see cref="BencodeLimits.AbsoluteMaxDepth" />; a document
    /// nested past the effective limit throws <see cref="BencodeFormatException" /> rather than risking a
    /// <see cref="StackOverflowException" />.
    /// </remarks>
    public int MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether dictionary keys may appear out of ascending bytewise order.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to accept unsorted keys; the default of <see langword="false" /> rejects them with
    /// <see cref="BencodeFormatException" />.
    /// </value>
    /// <remarks>
    /// Object properties are exposed in stored order regardless of this option; lookups compare raw key bytes, so an
    /// unsorted document resolves properties identically to a canonical one.
    /// </remarks>
    public bool AllowUnsortedKeys { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a dictionary may contain more than one entry for the same key.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to accept duplicate keys; the default of <see langword="false" /> rejects them with
    /// <see cref="BencodeFormatException" />.
    /// </value>
    /// <remarks>
    /// Every entry of a duplicated key is retained and visible through object enumeration; name lookups such as
    /// <see cref="BencodeElement.GetProperty(string)" /> return the first match in document order.
    /// </remarks>
    public bool AllowDuplicateKeys { get; set; }
}
