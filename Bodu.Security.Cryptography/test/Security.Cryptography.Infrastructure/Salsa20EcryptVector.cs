// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20EcryptVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single ECRYPT Salsa20 verified test vector: the key (128- or 256-bit) and 64-bit IV, the sampled
/// keystream fragments at their absolute offsets, and the XOR digest folded over the full generated keystream.
/// </summary>
/// <param name="KeyBits">The key size in bits (128 or 256), taken from the enclosing profile.</param>
/// <param name="Set">The ECRYPT test-vector set number (1-6).</param>
/// <param name="Vector">The vector number within the set.</param>
/// <param name="Key">The Salsa20 key.</param>
/// <param name="Iv">The 8-byte Salsa20 IV.</param>
/// <param name="Fragments">
/// The sampled keystream fragments, each an absolute byte offset paired with its expected bytes.
/// </param>
/// <param name="XorDigest">The expected 64-byte XOR digest of the full keystream.</param>
/// <param name="StreamLength">The total keystream length, in bytes, the digest and fragments describe.</param>
public sealed record Salsa20EcryptVector(
    int KeyBits,
    int Set,
    int Vector,
    byte[] Key,
    byte[] Iv,
    IReadOnlyList<(int Offset, byte[] Bytes)> Fragments,
    byte[] XorDigest,
    int StreamLength) : IKat
{
    /// <inheritdoc />
    public string Name => $"ECRYPT Salsa20/{KeyBits} Set {Set} vector#{Vector}";
}
