// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashStreamingKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Hashing.Contracts;

/// <summary>
/// Represents a streaming-parity known-answer test row: a raw byte payload fed to the algorithm via a series of
/// segmented <c>Append</c> calls of the specified sizes, asserting that the result matches the canonical one-shot
/// digest.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The full raw byte payload.</param>
/// <param name="SegmentSizes">
/// The segment sizes, in order. The sum must equal <see cref="Input" />.Length; each segment is fed via a separate
/// <c>Append</c> call.
/// </param>
/// <param name="ExpectedHex">The expected hex-encoded digest. Case is irrelevant for the assertion.</param>
public sealed record HashStreamingKat(
    string Name,
    byte[] Input,
    int[] SegmentSizes,
    string ExpectedHex) : IKat;
