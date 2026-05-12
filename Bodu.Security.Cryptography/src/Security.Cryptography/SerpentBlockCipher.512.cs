// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipher.512.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the wide-block tweakable <c>Serpent-512</c> block cipher variant, which operates on 512-bit (64-byte) blocks
/// using a 512-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cipher runs 64 rounds over a sixteen-word (512-bit) state, alternating the canonical Serpent S-boxes with the
/// bitsliced linear transform applied to each four-word lane. A cross-lane rotation between rounds provides diffusion across
/// lanes, and a Threefish-style tweak subkey is XOR-injected every four rounds.
/// </para>
/// <note type="important">
/// This type is a **non-standard Serpent-derived construction** and is not interoperable with any reference Serpent
/// implementation. Its cryptographic properties have not been externally analysed. For standard, externally vetted Serpent,
/// use <see cref="Serpent128Cipher"/>.
/// </note>
/// </remarks>
/// <seealso cref="Serpent512"/>
public sealed class Serpent512Cipher
    : SerpentBlockCipher
{
    /// <summary>
    /// The Serpent-512 key size, in bits.
    /// </summary>
    public const int KeySizeBits = 512;

    /// <summary>
    /// The Serpent-512 key size, in bytes; equal to <see cref="KeySizeBits"/> / 8.
    /// </summary>
    public const int KeySizeBytes = KeySizeBits / 8;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent512Cipher"/> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 512-bit (64-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behaviour.</param>
    public Serpent512Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <inheritdoc />
    public override int BlockSize => 512;

    /// <inheritdoc />
    private protected override int BlockWords => 16;

    /// <inheritdoc />
    private protected override int Rounds => 64;
}
