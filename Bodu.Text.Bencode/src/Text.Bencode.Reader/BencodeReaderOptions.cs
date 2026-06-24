// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Reader;

/// <summary>
/// Defines the customizations applied when creating a <see cref="Utf8BencodeReader" />.
/// </summary>
/// <remarks>
/// Bencode has no comment syntax and no trailing-comma concept; the options relax the two dictionary-key rules that
/// older real-world encoders are known to violate: <see cref="AllowUnsortedKeys" /> and
/// <see cref="AllowDuplicateKeys" />. Both default to the strict canonical behaviour.
/// </remarks>
public struct BencodeReaderOptions
{
    /// <summary>
    /// Gets or sets the maximum container nesting depth the reader will accept.
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
    /// BEP 3 requires keys sorted by raw byte order, but documents produced by older encoders occasionally violate
    /// this. Unless <see cref="AllowDuplicateKeys" /> is also set, duplicate keys are still rejected.
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
    /// The reader reports every entry it reads regardless of this option; which occurrence wins is decided by the
    /// consuming surface (the document model returns the first match from lookups, while the node tree and the
    /// serializer bind last-wins).
    /// </remarks>
    public bool AllowDuplicateKeys { get; set; }
}
