// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipher.1024.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the wide-block tweakable <c>Serpent-1024</c> block cipher variant, which operates on 1024-bit (128-byte)
/// blocks using a 1024-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cipher runs 80 rounds over a thirty-two-word (1024-bit) state, alternating the canonical Serpent S-boxes with the
/// bitsliced linear transform applied to each four-word lane. A cross-lane rotation between rounds provides diffusion across
/// lanes, and a Threefish-style tweak subkey is XOR-injected every four rounds.
/// </para>
/// <note type="important">
/// This type is a **non-standard Serpent-derived construction** and is not interoperable with any reference Serpent
/// implementation. Its cryptographic properties have not been externally analysed. For standard, externally vetted Serpent,
/// use <see cref="Serpent128Cipher"/>.
/// </note>
/// </remarks>
/// <seealso cref="Serpent1024"/>
public sealed class Serpent1024Cipher
    : SerpentBlockCipher
{
    /// <summary>
    /// The Serpent-1024 key size, in bits (1024 bits / 128 bytes).
    /// </summary>
    public const int KeySize = 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent1024Cipher"/> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 1024-bit (128-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behaviour.</param>
    public Serpent1024Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <inheritdoc />
    public override int BlockSize => 1024;

    /// <inheritdoc />
    private protected override int BlockWords => 32;

    /// <inheritdoc />
    private protected override int Rounds => 80;
}
