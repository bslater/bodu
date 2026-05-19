// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipher.512.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the wide-block tweakable <c>Serpent-512</c> block cipher variant, which operates on 512-bit (64-byte)
/// blocks using a 512-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cipher runs 64 rounds over a sixteen-word (512-bit) state, alternating the canonical Serpent S-boxes with the
/// bitsliced linear transform applied to each four-word lane. A cross-lane rotation between rounds provides diffusion
/// across lanes, and a Threefish-style tweak subkey is XOR-injected every four rounds.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Serpent512" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract. Use <see cref="Serpent512Cipher" />
/// directly only when composing the raw block primitive with an <see cref="IBlockCipherModeTransform" /> or
/// <see cref="IPaddingStrategy" />.
/// </para>
/// <note type="important"> This type is a **non-standard Serpent-derived construction** and is not interoperable with
/// any reference Serpent implementation. Its cryptographic properties have not been externally analyzed. For standard,
/// externally vetted Serpent, use <see cref="Serpent128Cipher" />. </note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Direct single-block use. For most workloads prefer the Serpent512 SymmetricAlgorithm wrapper.
/// byte[] key   = new byte[64];   // 512-bit key
/// byte[] tweak = new byte[16];   // 128-bit tweak
/// RandomNumberGenerator.Fill(key);
/// RandomNumberGenerator.Fill(tweak);
///
/// using var cipher = new Serpent512Cipher(key, tweak);
///
/// byte[] plaintext  = new byte[64];
/// byte[] ciphertext = new byte[64];
/// cipher.Encrypt(plaintext, ciphertext);
///
/// byte[] roundtrip = new byte[64];
/// cipher.Decrypt(ciphertext, roundtrip);
/// // roundtrip equals plaintext
///]]>
/// </example>
/// <seealso cref="Serpent512"/>
public sealed class Serpent512Cipher
    : SerpentBlockCipher
{
    /// <summary>
    /// Length of the Serpent-512 key is 512 bits (64 bytes).
    /// </summary>
    public const int KeySize = 512;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent512Cipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 512-bit (64-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behavior.</param>
    public Serpent512Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <inheritdoc />
    /// <value>Length of the Serpent-512 block is 512 bits (64 bytes).</value>
    public override int BlockSize => 512;

    /// <inheritdoc />
    private protected override int BlockWords => 16;

    /// <inheritdoc />
    private protected override int Rounds => 64;
}
