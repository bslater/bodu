// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Specifies options that control how a <see cref="CompoundFile" /> is opened for reading.
/// </summary>
/// <remarks>
/// The default instance buffers the whole file into memory and validates it at
/// <see cref="CompoundValidationLevel.Compatible" />, matching the behavior of the parameterless read overloads.
/// </remarks>
public sealed class CompoundFileOptions
{
    /// <summary>The default maximum buffered size, in bytes, applied when <see cref="ReadStrategy" /> is <see cref="CompoundReadStrategy.Auto" />.</summary>
    private const long DefaultMaxBufferedBytes = 64L * 1024 * 1024;

    /// <summary>
    /// Gets a shared <see cref="CompoundFileOptions" /> instance carrying the default values.
    /// </summary>
    /// <value>The default options.</value>
    public static CompoundFileOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets the strategy used to source the file's bytes.
    /// </summary>
    /// <value>
    /// The configured <see cref="CompoundReadStrategy" />; <see cref="CompoundReadStrategy.Buffered" /> by default.
    /// </value>
    public CompoundReadStrategy ReadStrategy { get; set; } = CompoundReadStrategy.Buffered;

    /// <summary>
    /// Gets or sets the size threshold, in bytes, above which a seekable source is read on demand.
    /// </summary>
    /// <value>Only consulted when <see cref="ReadStrategy" /> is <see cref="CompoundReadStrategy.Auto" />.</value>
    public long MaxBufferedBytes { get; set; } = DefaultMaxBufferedBytes;

    /// <summary>
    /// Gets or sets how strictly the container's structure is validated.
    /// </summary>
    /// <value>
    /// The configured <see cref="CompoundValidationLevel" />; <see cref="CompoundValidationLevel.Compatible" /> by
    /// default.
    /// </value>
    public CompoundValidationLevel ValidationLevel { get; set; } = CompoundValidationLevel.Compatible;

    /// <summary>
    /// Resolves whether the file should be buffered whole for the supplied source.
    /// </summary>
    /// <param name="stream">The source stream the file is opened over.</param>
    /// <returns>
    /// <see langword="true" /> to buffer the whole file; <see langword="false" /> to read sectors on demand.
    /// </returns>
    internal bool ShouldBuffer(Stream stream) =>
        ReadStrategy switch
        {
            CompoundReadStrategy.Streaming => false,
            CompoundReadStrategy.Auto => !(stream.CanSeek && stream.Length > MaxBufferedBytes),
            _ => true,
        };
}
