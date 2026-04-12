// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RandomizationMode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

/// <summary>
/// Specifies the available strategies for randomising a sequence.
/// </summary>
/// <remarks>Use <see cref="RandomizationMode" /> values with <c>Randomize</c> overloads to control how the source sequence is processed and shuffled.</remarks>
public enum RandomizationMode
{
    /// <summary>
    /// Buffers all elements into memory and applies an in-place Fisher-Yates shuffle before yielding.
    /// </summary>
    BufferAll,

    /// <summary>
    /// Selects a fixed-size random subset using reservoir sampling, without buffering the full sequence.
    /// </summary>
    ReservoirSample,

    /// <summary>
    /// Shuffles elements using a sliding window whilst streaming, limiting memory use to the window size.
    /// </summary>
    StreamWindowed,

    /// <summary>
    /// Lazily builds a fixed-size reservoir sample and then shuffles and yields its contents.
    /// </summary>
    LazyShuffle,
}
