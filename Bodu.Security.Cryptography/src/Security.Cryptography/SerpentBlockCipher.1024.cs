// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentBlockCipher.1024.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the wide-block tweakable <c>Serpent-1024</c> block cipher variant, which operates on 1024-bit (128-byte)
/// blocks using a 1024-bit key and a 128-bit tweak. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// The cipher runs 80 rounds over a thirty-two-word (1024-bit) state, alternating the canonical Serpent S-boxes with
/// the bitsliced linear transform applied to each four-word lane. A cross-lane rotation between rounds provides
/// diffusion across lanes, and a Threefish-style tweak subkey is XOR-injected every four rounds.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Serpent1024" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract. Use <see cref="Serpent1024Cipher" />
/// directly only when composing the raw block primitive with an <see cref="IBlockCipherModeTransform" /> or
/// <see cref="IPaddingStrategy" />.
/// </para>
/// <note type="important"> This type is a **non-standard Serpent-derived construction** and is not interoperable with
/// any reference Serpent implementation. Its cryptographic properties have not been externally analyzed. For standard,
/// externally vetted Serpent, use <see cref="Serpent128Cipher" />. </note>
/// </remarks>
/// <example>
///<![CDATA[
/// Direct single-block use. For most workloads prefer the Serpent1024 SymmetricAlgorithm wrapper.
/// byte[] key   = new byte[128];   // 1024-bit key
/// byte[] tweak = new byte[16];    // 128-bit tweak
/// RandomNumberGenerator.Fill(key);
/// RandomNumberGenerator.Fill(tweak);
///
/// using var cipher = new Serpent1024Cipher(key, tweak);
///
/// byte[] plaintext  = new byte[128];
/// byte[] ciphertext = new byte[128];
/// cipher.Encrypt(plaintext, ciphertext);
///
/// byte[] roundtrip = new byte[128];
/// cipher.Decrypt(ciphertext, roundtrip);
/// roundtrip equals plaintext
///]]>
/// </example>
/// <seealso cref="Serpent1024"/>
public sealed class Serpent1024Cipher
    : SerpentBlockCipher
{
    /// <summary>
    /// Length of the Serpent-1024 key is 1024 bits (128 bytes).
    /// </summary>
    public const int KeySize = 1024;

    /// <summary>
    /// Initializes a new instance of the <see cref="Serpent1024Cipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 1024-bit (128-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behavior.</param>
    public Serpent1024Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <inheritdoc />
    /// <value>Length of the Serpent-1024 block is 1024 bits (128 bytes).</value>
    public override int BlockSize => 1024;

    /// <inheritdoc />
    private protected override int BlockWords => 32;

    /// <inheritdoc />
    private protected override int Rounds => 80;
}
