// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipher.256.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the wide-block tweakable <c>Serpent-256</c> block cipher variant, which operates on 256-bit (32-byte)
/// blocks using a 256-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cipher runs 48 rounds over an eight-word (256-bit) state, alternating the canonical Serpent S-boxes with the
/// bitsliced linear transform applied to each four-word lane. A cross-lane rotation between rounds provides diffusion
/// across lanes, and a Threefish-style tweak subkey is XOR-injected every four rounds.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Serpent256" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract. Use <see cref="Serpent256Cipher" />
/// directly only when composing the raw block primitive with an <see cref="IBlockCipherModeTransform" /> or
/// <see cref="IPaddingStrategy" />.
/// </para>
/// <note type="important"> This type is a **non-standard Serpent-derived construction** and is not interoperable with
/// any reference Serpent implementation. Its cryptographic properties have not been externally analyzed. For standard,
/// externally vetted Serpent, use <see cref="Serpent128Cipher" />. </note>
/// </remarks>
/// <example>
///<![CDATA[
/// Direct single-block use. For most workloads prefer the Serpent256 SymmetricAlgorithm wrapper.
/// byte[] key   = new byte[32];   // 256-bit key
/// byte[] tweak = new byte[16];   // 128-bit tweak
/// RandomNumberGenerator.Fill(key);
/// RandomNumberGenerator.Fill(tweak);
///
/// using var cipher = new Serpent256Cipher(key, tweak);
///
/// byte[] plaintext  = new byte[32];
/// byte[] ciphertext = new byte[32];
/// cipher.Encrypt(plaintext, ciphertext);
///
/// byte[] roundtrip = new byte[32];
/// cipher.Decrypt(ciphertext, roundtrip);
/// roundtrip equals plaintext
///]]>
/// </example>
/// <seealso cref="Serpent256"/>
public sealed class Serpent256Cipher
    : SerpentBlockCipher
{
    /// <summary>
    /// Length of the Serpent-256 key is 256 bits (32 bytes).
    /// </summary>
    public const int KeySize = 256;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent256Cipher" /> class using the specified key and tweak.
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
