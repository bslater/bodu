// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RandomizationMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

/// <summary>
/// Specifies the available strategies for randomizing a sequence.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="RandomizationMode" /> values with <c>Randomize</c> overloads to control how the source sequence is
/// processed and shuffled.
/// </para>
/// <para>
/// There is no buffer-everything mode: to shuffle a fully materialized collection in place, use the BCL
/// <see cref="Random.Shuffle{T}(Span{T})" /> (or <c>Random.Shared.Shuffle</c>) instead.
/// </para>
/// </remarks>
public enum RandomizationMode
{
    /// <summary>
    /// Selects a fixed-size random subset using reservoir sampling, without buffering the full sequence.
    /// </summary>
    ReservoirSample,

    /// <summary>
    /// Shuffles elements using a sliding window while streaming, limiting memory use to the window size.
    /// </summary>
    StreamWindowed,

    /// <summary>
    /// Lazily builds a fixed-size reservoir sample and then shuffles and yields its contents.
    /// </summary>
    LazyShuffle,
}
