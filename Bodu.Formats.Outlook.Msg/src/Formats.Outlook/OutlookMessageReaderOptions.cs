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
