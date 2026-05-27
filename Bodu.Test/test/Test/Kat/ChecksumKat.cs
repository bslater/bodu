// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChecksumKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a fixed-width integer checksum (CRC, Fletcher, Adler, etc.).
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Algorithm">The algorithm identifier (for example <c>"CRC-32"</c>, <c>"Fletcher-16"</c>).</param>
/// <param name="Input">The raw byte payload fed to the algorithm.</param>
/// <param name="ExpectedValue">
/// The expected checksum value, widened to <see cref="ulong" /> so the same record fits 8/16/32/64-bit checksums.
/// </param>
/// <param name="Width">The checksum width in bits.</param>
/// <param name="Parameters">
/// An optional opaque payload describing variant parameters (polynomial, initial value, refIn/refOut flags) consumed by
/// the test runner.
/// </param>
public sealed record ChecksumKat(
    string Name,
    string Algorithm,
    byte[] Input,
    ulong ExpectedValue,
    int Width,
    object? Parameters = null) : IKat;
