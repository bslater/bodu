// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a one-shot known-answer test row for a hash or checksum algorithm: a raw byte payload paired
/// with the canonical hex-encoded digest the algorithm must produce.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Input">The raw byte payload fed to the algorithm.</param>
/// <param name="ExpectedHex">The expected hex-encoded digest. Case is irrelevant for the assertion.</param>
/// <param name="ExpectedHashSizeBits">The digest size in bits, used by tests that assert on the algorithm's declared <c>HashLengthInBytes</c>.</param>
/// <param name="Seed">An optional seed or initialisation vector consumed by algorithms that accept one (CityHash, MurmurHash3, etc.). <see langword="null" /> uses the algorithm's default seed.</param>
/// <param name="Variant">An optional variant tag consumed by algorithms that expose multiple parameter sets (CRC standards, BLAKE2 personalisation, Skein output sizes).</param>
public sealed record HashKat(
    string Name,
    byte[] Input,
    string ExpectedHex,
    int ExpectedHashSizeBits,
    object? Seed = null,
    string? Variant = null) : IKat;
