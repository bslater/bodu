// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipher.256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the wide-block tweakable <c>Serpent-256</c> block cipher variant, which operates on 256-bit (32-byte) blocks
/// using a 256-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cipher runs 48 rounds over an eight-word (256-bit) state, alternating the canonical Serpent S-boxes with the
/// bitsliced linear transform applied to each four-word lane. A cross-lane rotation between rounds provides diffusion across
/// lanes, and a Threefish-style tweak subkey is XOR-injected every four rounds.
/// </para>
/// <note type="important">
/// This type is a **non-standard Serpent-derived construction** and is not interoperable with any reference Serpent
/// implementation. Its cryptographic properties have not been externally analyzed. For standard, externally vetted Serpent,
/// use <see cref="Serpent128Cipher"/>.
/// </note>
/// </remarks>
/// <seealso cref="Serpent256"/>
public sealed class Serpent256Cipher
    : SerpentBlockCipher
{
    /// <summary>
    /// Length of the Serpent-256 key is 256 bits (32 bytes).
    /// </summary>
    public const int KeySize = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent256Cipher"/> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behavior.</param>
    public Serpent256Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <inheritdoc />
    /// <value>Length of the Serpent-256 block is 256 bits (32 bytes).</value>
    public override int BlockSize => 256;

    /// <inheritdoc />
    private protected override int BlockWords => 8;

    /// <inheritdoc />
    private protected override int Rounds => 48;
}
