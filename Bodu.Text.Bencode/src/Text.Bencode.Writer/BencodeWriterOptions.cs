// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Writer;

/// <summary>
/// Defines the customizations applied when creating a <see cref="Utf8BencodeWriter" />.
/// </summary>
/// <remarks>
/// Bencode output is always canonical — dictionary keys are emitted in ascending bytewise order and there is no
/// insignificant whitespace — so the options define no indentation or encoder settings.
/// </remarks>
public struct BencodeWriterOptions
{
    /// <summary>
    /// Gets or sets the maximum container nesting depth the writer will permit.
    /// </summary>
    /// <value>The maximum container nesting depth; <c>0</c> selects the default of 64.</value>
    /// <remarks>
    /// The effective depth is clamped to the hard ceiling <see cref="BencodeLimits.AbsoluteMaxDepth" />; a larger value
    /// has no effect beyond it. Opening a list or dictionary nested past the effective limit throws
    /// <see cref="BencodeSerializationException" /> rather than risking a <see cref="StackOverflowException" />.
    /// </remarks>
    public int MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the writer accepts more than one value at the top level, concatenating
    /// the values in the output.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to permit multiple root values; the default of <see langword="false" /> rejects a second
    /// root value with <see cref="InvalidOperationException" />.
    /// </value>
    /// <remarks>
    /// A BEP 3 document is a single value, and the reader rejects trailing bytes after the root, so output produced
    /// with this option set cannot be re-read as one document. Enable it only for protocol framings that concatenate
    /// independent Bencode values on one stream.
    /// </remarks>
    public bool AllowMultipleRootValues { get; set; }
}
