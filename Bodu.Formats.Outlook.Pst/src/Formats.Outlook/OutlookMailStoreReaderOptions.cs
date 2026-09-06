// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStoreReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Controls how an <see cref="OutlookMailStore" /> reads its PST container and decodes message content.
/// </summary>
public sealed class OutlookMailStoreReaderOptions
{
    /// <summary>The default embedded-message nesting limit.</summary>
    internal const int DefaultMaxEmbeddedMessageDepth = 16;

    /// <summary>The default decompressed-RTF ceiling: 64 MiB.</summary>
    internal const int DefaultMaxDecompressedRtfBytes = 64 * 1024 * 1024;

    /// <summary>The default <see cref="MaxInlineAttachmentBytes" />: 1 MiB.</summary>
    internal const int DefaultMaxInlineAttachmentBytes = 1024 * 1024;

    /// <summary>The backing field for <see cref="BlockCacheSize" />.</summary>
    private readonly int _blockCacheSize = 256;

    /// <summary>The backing field for <see cref="MaxNodeDataLength" />.</summary>
    private readonly long _maxNodeDataLength = 256L * 1024 * 1024;

    /// <summary>The backing field for <see cref="MaxEmbeddedMessageDepth" />.</summary>
    private readonly int _maxEmbeddedMessageDepth = DefaultMaxEmbeddedMessageDepth;

    /// <summary>The backing field for <see cref="MaxDecompressedRtfBytes" />.</summary>
    private readonly int _maxDecompressedRtfBytes = DefaultMaxDecompressedRtfBytes;

    /// <summary>The backing field for <see cref="MaxInlineAttachmentBytes" />.</summary>
    private readonly int _maxInlineAttachmentBytes = DefaultMaxInlineAttachmentBytes;

    /// <summary>
    /// Gets the shared default options instance.
    /// </summary>
    /// <value>The default options.</value>
    internal static OutlookMailStoreReaderOptions Default { get; } = new();

    /// <summary>
    /// Gets how strictly the container's structures and checksums are validated.
    /// </summary>
    /// <value>The validation level; <see cref="PstValidationLevel.Compatible" /> by default.</value>
    public PstValidationLevel ValidationLevel { get; init; } = PstValidationLevel.Compatible;

    /// <summary>
    /// Gets the decoded page/block cache budget of the underlying container session, in entries.
    /// </summary>
    /// <value>The cache entry budget; <c>256</c> by default. <c>0</c> disables caching entirely.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int BlockCacheSize
    {
        get => _blockCacheSize;
        init
        {
            ThrowHelper.ThrowIfNegative(value);

            _blockCacheSize = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the compressed RTF body (<c>PidTagRtfCompressed</c>) is decompressed when read
    /// through the body conveniences.
    /// </summary>
    /// <value><see langword="true" /> by default; when disabled the RTF body convenience returns <see langword="null" />.</value>
    public bool DecompressRtf { get; init; } = true;

    /// <summary>
    /// Gets the largest node payload, in bytes, the underlying container materializes in memory — the ceiling on any
    /// single property value, attachment payload, or table the store decodes at once.
    /// </summary>
    /// <value>The materialization limit; 256 MiB by default (see <see cref="PstFileOptions.MaxNodeDataLength" />).</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public long MaxNodeDataLength
    {
        get => _maxNodeDataLength;
        init
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);

            _maxNodeDataLength = value;
        }
    }

    /// <summary>
    /// Gets the deepest embedded-message nesting the reader opens: a folder-level message is depth zero, a message
    /// opened from one of its attachments is depth one, and so on.
    /// </summary>
    /// <value>The nesting limit; <c>16</c> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    /// <remarks>
    /// Opening an embedded message past the limit throws <see cref="OutlookPstFormatException" /> at every validation
    /// level, so a crafted subnode graph cannot drive a recursive consumer off its stack.
    /// </remarks>
    public int MaxEmbeddedMessageDepth
    {
        get => _maxEmbeddedMessageDepth;
        init
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);

            _maxEmbeddedMessageDepth = value;
        }
    }

    /// <summary>
    /// Gets the largest decompressed RTF body, in bytes, the reader produces.
    /// </summary>
    /// <value>The ceiling; 64 MiB by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    /// <remarks>
    /// A compressed payload whose declared or produced size exceeds the ceiling is rejected with
    /// <see cref="OutlookPstFormatException" /> at every validation level.
    /// </remarks>
    public int MaxDecompressedRtfBytes
    {
        get => _maxDecompressedRtfBytes;
        init
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);

            _maxDecompressedRtfBytes = value;
        }
    }

    /// <summary>
    /// Gets the largest by-value attachment payload, in bytes, that is decoded into the attachment's
    /// <see cref="OutlookMailAttachment.Properties" />; a larger <c>PidTagAttachDataBinary</c> is left in the store
    /// and served only through <see cref="OutlookMailAttachment.OpenContentStream" />.
    /// </summary>
    /// <value>The inline payload limit; 1 MiB by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    /// <remarks>
    /// Above the limit the property is still present in the collection, with a <see langword="null" /> value, so
    /// <see cref="MapiPropertyCollection.Contains(MapiPropertyTag)" /> and <see cref="OutlookMailAttachment.Method" />
    /// are unaffected; <see cref="MapiPropertyCollection.GetBinary(ushort)" /> returns <see langword="null" /> and the
    /// content streams from the container without being materialized. This is an inline-decoding threshold, not a
    /// validation rule: it does not vary with <see cref="ValidationLevel" />.
    /// </remarks>
    public int MaxInlineAttachmentBytes
    {
        get => _maxInlineAttachmentBytes;
        init
        {
            ThrowHelper.ThrowIfZeroOrNegative(value);

            _maxInlineAttachmentBytes = value;
        }
    }

    /// <summary>
    /// Builds the container options this instance implies.
    /// </summary>
    /// <returns>The equivalent <see cref="PstFileOptions" />.</returns>
    internal PstFileOptions ToPstFileOptions() =>
        new()
        {
            ValidationLevel = ValidationLevel,
            BlockCacheSize = BlockCacheSize,
            MaxNodeDataLength = MaxNodeDataLength,
        };
}
