// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Carries the state a <see cref="BiffReader" /> needs to continue a stream across buffers: the established BIFF
/// version and the active code page.
/// </summary>
/// <remarks>
/// <see cref="BiffReader" /> is a <see langword="ref struct" /> and cannot be stored in a field. A consumer that reads
/// incrementally — a class that walks a substream one record per call, or a loop feeding chunks of a
/// <see cref="Stream" /> — captures <see cref="BiffReader.CurrentState" /> after each pass, slices its buffer at
/// <see cref="BiffReader.BytesConsumed" />, and constructs the next reader from the remaining bytes and the captured
/// state. The record position itself is not part of the state: it is expressed by where the caller slices the buffer.
/// </remarks>
public struct BiffReaderState
{
    /// <summary>The options the reader was created with.</summary>
    private readonly BiffReaderOptions _options;

    /// <summary>The established version, or <see cref="BiffVersion.Unknown" />.</summary>
    internal BiffVersion _version;

    /// <summary>The active code page, or zero when none has been seen or supplied.</summary>
    internal int _codePage;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffReaderState" /> struct describing the start of a stream.
    /// </summary>
    /// <param name="options">The options that seed the version and code page.</param>
    public BiffReaderState(BiffReaderOptions options = default)
    {
        _options = options;
        _version = options.Version;
        _codePage = options.CodePage;
    }

    /// <summary>
    /// Gets the options the reader was created with.
    /// </summary>
    /// <value>The options.</value>
    public readonly BiffReaderOptions Options => _options;

    /// <summary>
    /// Gets the BIFF version established so far.
    /// </summary>
    /// <value>The version, or <see cref="BiffVersion.Unknown" /> before a <c>BOF</c> record has been read.</value>
    public readonly BiffVersion Version => _version;

    /// <summary>
    /// Gets the code page in effect for byte strings.
    /// </summary>
    /// <value>
    /// The Windows code page number from the most recent <c>CODEPAGE</c> record or the options, or
    /// <see cref="BiffLimits.DefaultCodePage" /> when neither has supplied one.
    /// </value>
    public readonly int CodePage => _codePage == 0 ? BiffLimits.DefaultCodePage : _codePage;
}
