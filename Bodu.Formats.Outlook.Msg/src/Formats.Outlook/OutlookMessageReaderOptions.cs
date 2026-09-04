// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessageReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Controls how an <see cref="OutlookMessage" /> opens its container and handles malformed content.
/// </summary>
/// <remarks>
/// The defaults mirror the underlying <see cref="CompoundFileOptions" />: the container is buffered whole and validated
/// at <see cref="CompoundValidationLevel.Compatible" />, under which a malformed property is omitted rather than
/// failing the open. <see cref="CompoundValidationLevel.Strict" /> makes structural problems throw
/// <see cref="OutlookMsgFormatException" /> instead.
/// </remarks>
public sealed class OutlookMessageReaderOptions
{
    /// <summary>The default embedded-message nesting limit.</summary>
    internal const int DefaultMaxEmbeddedMessageDepth = 16;

    /// <summary>The default decompressed-RTF ceiling: 64 MiB.</summary>
    internal const int DefaultMaxDecompressedRtfBytes = 64 * 1024 * 1024;

    /// <summary>The default <see cref="MaxInlineAttachmentBytes" />: 1 MiB.</summary>
    internal const int DefaultMaxInlineAttachmentBytes = 1024 * 1024;

    /// <summary>The backing field for <see cref="MaxEmbeddedMessageDepth" />.</summary>
    private readonly int _maxEmbeddedMessageDepth = DefaultMaxEmbeddedMessageDepth;

    /// <summary>The backing field for <see cref="MaxDecompressedRtfBytes" />.</summary>
    private readonly int _maxDecompressedRtfBytes = DefaultMaxDecompressedRtfBytes;

    /// <summary>The backing field for <see cref="MaxInlineAttachmentBytes" />.</summary>
    private readonly int _maxInlineAttachmentBytes = DefaultMaxInlineAttachmentBytes;

    /// <summary>
    /// Gets the shared default options instance.
    /// </summary>
    /// <value>The default reader options.</value>
    internal static OutlookMessageReaderOptions Default { get; } = new();

    /// <summary>
    /// Gets how strictly the container and the message structures are validated.
    /// </summary>
    /// <value>The validation level; <see cref="CompoundValidationLevel.Compatible" /> by default.</value>
    public CompoundValidationLevel ValidationLevel { get; init; } = CompoundValidationLevel.Compatible;

    /// <summary>
    /// Gets the strategy used to source the container's bytes.
    /// </summary>
    /// <value>The read strategy; <see cref="CompoundReadStrategy.Buffered" /> by default.</value>
    public CompoundReadStrategy ReadStrategy { get; init; } = CompoundReadStrategy.Buffered;

    /// <summary>
    /// Gets a value indicating whether the compressed RTF body is decompressed when read through the body conveniences.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to decompress <c>PidTagRtfCompressed</c> on access; the default. When
    /// <see langword="false" /> the RTF body convenience returns <see langword="null" /> and the raw payload remains
    /// available through the property collection.
    /// </value>
    public bool DecompressRtf { get; init; } = true;

    /// <summary>
    /// Gets the deepest embedded-message nesting the reader opens: the root message is depth zero, a message opened
    /// from one of its attachments is depth one, and so on.
    /// </summary>
    /// <value>The nesting limit; <c>16</c> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    /// <remarks>
    /// Opening an embedded message past the limit throws <see cref="OutlookMsgFormatException" /> at every validation
    /// level. The limit exists because a crafted container can nest attachments thousands of levels deep in a few
    /// kilobytes, and a consumer walking the tree recursively would otherwise exhaust its stack.
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
    /// <see cref="OutlookMsgFormatException" /> at every validation level: the declared size sits outside the payload's
    /// checksum, so it must be bounded rather than trusted.
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
    /// <see cref="OutlookAttachment.Properties" />; a larger <c>PidTagAttachDataBinary</c> stream is left in the
    /// container and served only through <see cref="OutlookAttachment.OpenContentStream" />.
    /// </summary>
    /// <value>The inline payload limit; 1 MiB by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    /// <remarks>
    /// Above the limit the property is still present in the collection, with a <see langword="null" /> value, so
    /// <see cref="MapiPropertyCollection.Contains(MapiPropertyTag)" /> and <see cref="OutlookAttachment.Method" /> are
    /// unaffected; <see cref="MapiPropertyCollection.GetBinary(ushort)" /> returns <see langword="null" /> and the
    /// content is read from the container when the stream is opened. Combined with
    /// <see cref="CompoundReadStrategy.Streaming" /> the payload is never held in memory in full. This is an
    /// inline-decoding threshold, not a validation rule: it does not vary with <see cref="ValidationLevel" />.
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
    /// Converts the reader options to the underlying container options.
    /// </summary>
    /// <returns>The equivalent <see cref="CompoundFileOptions" />.</returns>
    internal CompoundFileOptions ToCompoundFileOptions() =>
        new()
        {
            ValidationLevel = ValidationLevel,
            ReadStrategy = ReadStrategy,
        };
}
