// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single known-answer test row for a symmetric block cipher in its raw block-encryption shape (no mode,
/// no padding): the key, the plaintext block, and the ciphertext the cipher must produce.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Algorithm">The algorithm identifier (for example <c>"Threefish-256"</c>, <c>"Skipjack"</c>).</param>
/// <param name="Key">The encryption key, length determined by the algorithm.</param>
/// <param name="Plaintext">The plaintext block.</param>
/// <param name="Ciphertext">The expected ciphertext block from <c>Encrypt(Key, Plaintext)</c>.</param>
/// <param name="BlockSizeBits">
/// The cipher block size in bits, used by tests that assert the algorithm's declared block size.
/// </param>
/// <param name="Tweak">
/// The optional tweak input for tweakable block ciphers such as Threefish; <see langword="null" /> for ciphers without
/// a tweak.
/// </param>
public sealed record BlockCipherKat(
    string Name,
    string Algorithm,
    byte[] Key,
    byte[] Plaintext,
    byte[] Ciphertext,
    int BlockSizeBits,
    byte[]? Tweak = null) : IKat;
