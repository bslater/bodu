// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salsa20XorDigestVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single ECRYPT Salsa20 "Set 6" XOR-digest test vector: a key and IV, the total keystream length in
/// bytes, and the expected 64-byte digest formed by XOR-folding every 64-byte keystream block over that length.
/// </summary>
/// <param name="Number">The 1-based position of this vector in the source array.</param>
/// <param name="Key">The Salsa20 key (32 bytes for the Set 6 vectors).</param>
/// <param name="Iv">The 8-byte Salsa20 IV.</param>
/// <param name="NumBytes">The total keystream length, in bytes, folded into the digest.</param>
/// <param name="XorDigest">The expected 64-byte XOR digest.</param>
public sealed record Salsa20XorDigestVector(
    int Number,
    byte[] Key,
    byte[] Iv,
    int NumBytes,
    byte[] XorDigest) : IKat
{
    /// <inheritdoc />
    public string Name => $"ECRYPT Salsa20 Set 6 #{Number} ({Key.Length * 8}-bit key, {NumBytes} bytes)";
}
