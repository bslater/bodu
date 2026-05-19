// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.512.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the <c>Threefish-512</c> block cipher, which operates on 512-bit (64-byte) blocks using a 512-bit key and
/// a 128-bit tweak.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is a tweakable block cipher optimized for 64-bit platforms and forms the core primitive of the Skein hash
/// function. The <c>Threefish-512</c> variant operates on eight 64-bit words over 72 rounds using modular addition,
/// bitwise rotation, and XOR.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Threefish512" /> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm" /> contract. Use <see cref="Threefish512Cipher" />
/// directly only when composing the raw block primitive with an <see cref="IBlockCipherModeTransform" /> (for example
/// via <see cref="BlockCipherModeFactory" />) or with an <see cref="IPaddingStrategy" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Direct single-block use — most callers should prefer the Threefish512 SymmetricAlgorithm.
/// byte[] key   = new byte[64];   // 512-bit key
/// byte[] tweak = new byte[16];   // 128-bit tweak
/// RandomNumberGenerator.Fill(key);
/// RandomNumberGenerator.Fill(tweak);
///
/// using var cipher = new Threefish512Cipher(key, tweak);
///
/// byte[] plaintext  = new byte[64];   // one 512-bit block
/// byte[] ciphertext = new byte[64];
/// cipher.Encrypt(plaintext, ciphertext);
///
/// byte[] roundtrip = new byte[64];
/// cipher.Decrypt(ciphertext, roundtrip);
/// // roundtrip equals plaintext
///]]>
/// </example>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs.
/// SymmetricAlgorithm</seealso> <seealso cref="Threefish512"/>
public sealed class Threefish512Cipher
    : ThreefishBlockCipher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish512Cipher" /> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 512-bit (64-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behavior.</param>
    public Threefish512Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <summary>
    /// Length of the Threefish-512 key is 512 bits (64 bytes).
    /// </summary>
    public const int KeySize = 512;

    /// <inheritdoc />
    /// <value>Length of the Threefish-512 block is 512 bits (64 bytes).</value>
    public override int BlockSize => 512;

    /// <inheritdoc />
    protected override int BlockWords => 8;

    /// <inheritdoc />
    protected override int[] RotationSchedule =>
    [
        46,
        36,
        19,
        37,
        33,
        27,
        14,
        42,
        17,
        49,
        36,
        39,
        44,
        9,
        54,
        56,
        39,
        30,
        34,
        24,
        13,
        50,
        10,
        17,
        25,
        29,
        39,
        43,
        8,
        35,
        56,
        22
    ];

    /// <inheritdoc />
    protected override int Rounds => 72;

    /// <summary>
    /// Decrypts a single 64-byte ciphertext block using the <c>Threefish-512</c> cipher and writes the result to the
    /// specified output buffer.
    /// </summary>
    /// <param name="input">The 64-byte ciphertext block to decrypt.</param>
    /// <param name="output">The 64-byte buffer to receive the decrypted plaintext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> or <paramref name="output" /> is not 64 bytes.
    /// </exception>
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != this.BlockSize / 8 || output.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_BlockLength, this.BlockSize / 8));

        Span<ulong> block = stackalloc ulong[this.BlockWords];
        MemoryMarshal.Cast<byte, ulong>(input).CopyTo(block);

        var key = this._keySchedule;
        var tweak = this._tweakSchedule;
        var rot = this.RotationSchedule;

        for (var d = (this.Rounds / 4) - 1; d >= 1; d -= 2)
        {
            int dm9 = d % 9, dm3 = d % 3;

            block[0] -= key[dm9 + 1];
            block[1] -= key[dm9 + 2];
            block[2] -= key[dm9 + 3];
            block[3] -= key[dm9 + 4];
            block[4] -= key[dm9 + 5];
            block[5] -= key[dm9 + 6] + tweak[dm3 + 1];
            block[6] -= key[dm9 + 7] + tweak[dm3 + 2];
            block[7] -= key[dm9 + 8] + (ulong)(d + 1);

            Unmix(ref block[4], ref block[3], rot[31]);
            Unmix(ref block[2], ref block[5], rot[30]);
            Unmix(ref block[0], ref block[7], rot[29]);
            Unmix(ref block[6], ref block[1], rot[28]);
            Unmix(ref block[2], ref block[7], rot[27]);
            Unmix(ref block[0], ref block[5], rot[26]);
            Unmix(ref block[6], ref block[3], rot[25]);
            Unmix(ref block[4], ref block[1], rot[24]);
            Unmix(ref block[0], ref block[3], rot[23]);
            Unmix(ref block[6], ref block[5], rot[22]);
            Unmix(ref block[4], ref block[7], rot[21]);
            Unmix(ref block[2], ref block[1], rot[20]);
            Unmix(ref block[6], ref block[7], rot[19]);
            Unmix(ref block[4], ref block[5], rot[18]);
            Unmix(ref block[2], ref block[3], rot[17]);
            Unmix(ref block[0], ref block[1], rot[16]);

            block[0] -= key[dm9];
            block[1] -= key[dm9 + 1];
            block[2] -= key[dm9 + 2];
            block[3] -= key[dm9 + 3];
            block[4] -= key[dm9 + 4];
            block[5] -= key[dm9 + 5] + tweak[dm3];
            block[6] -= key[dm9 + 6] + tweak[dm3 + 1];
            block[7] -= key[dm9 + 7] + (ulong)d;

            Unmix(ref block[4], ref block[3], rot[15]);
            Unmix(ref block[2], ref block[5], rot[14]);
            Unmix(ref block[0], ref block[7], rot[13]);
            Unmix(ref block[6], ref block[1], rot[12]);
            Unmix(ref block[2], ref block[7], rot[11]);
            Unmix(ref block[0], ref block[5], rot[10]);
            Unmix(ref block[6], ref block[3], rot[9]);
            Unmix(ref block[4], ref block[1], rot[8]);
            Unmix(ref block[0], ref block[3], rot[7]);
            Unmix(ref block[6], ref block[5], rot[6]);
            Unmix(ref block[4], ref block[7], rot[5]);
            Unmix(ref block[2], ref block[1], rot[4]);
            Unmix(ref block[6], ref block[7], rot[3]);
            Unmix(ref block[4], ref block[5], rot[2]);
            Unmix(ref block[2], ref block[3], rot[1]);
            Unmix(ref block[0], ref block[1], rot[0]);
        }

        block[0] -= key[0];
        block[1] -= key[1];
        block[2] -= key[2];
        block[3] -= key[3];
        block[4] -= key[4];
        block[5] -= key[5] + tweak[0];
        block[6] -= key[6] + tweak[1];
        block[7] -= key[7];

        MemoryMarshal.Cast<ulong, byte>(block).CopyTo(output);
    }

    /// <summary>
    /// Encrypts a single 64-byte plaintext block using the <c>Threefish-512</c> cipher and writes the result to the
    /// specified output buffer.
    /// </summary>
    /// <param name="input">The 64-byte plaintext block to encrypt.</param>
    /// <param name="output">The 64-byte buffer to receive the encrypted ciphertext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown if <paramref name="input" /> or <paramref name="output" /> is not 64 bytes.
    /// </exception>
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != this.BlockSize / 8 || output.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.Crypt_Invalid_BlockLength, this.BlockSize / 8));

        Span<ulong> block = stackalloc ulong[this.BlockWords];
        MemoryMarshal.Cast<byte, ulong>(input).CopyTo(block);

        var key = this._keySchedule;
        var tweak = this._tweakSchedule;
        var rot = this.RotationSchedule;

        block[0] += key[0];
        block[1] += key[1];
        block[2] += key[2];
        block[3] += key[3];
        block[4] += key[4];
        block[5] += key[5] + tweak[0];
        block[6] += key[6] + tweak[1];
        block[7] += key[7];

        for (var d = 1; d < this.Rounds / 4; d += 2)
        {
            int dm9 = d % 9, dm3 = d % 3;

            Mix(ref block[0], ref block[1], rot[0]);
            Mix(ref block[2], ref block[3], rot[1]);
            Mix(ref block[4], ref block[5], rot[2]);
            Mix(ref block[6], ref block[7], rot[3]);
            Mix(ref block[2], ref block[1], rot[4]);
            Mix(ref block[4], ref block[7], rot[5]);
            Mix(ref block[6], ref block[5], rot[6]);
            Mix(ref block[0], ref block[3], rot[7]);
            Mix(ref block[4], ref block[1], rot[8]);
            Mix(ref block[6], ref block[3], rot[9]);
            Mix(ref block[0], ref block[5], rot[10]);
            Mix(ref block[2], ref block[7], rot[11]);
            Mix(ref block[6], ref block[1], rot[12]);
            Mix(ref block[0], ref block[7], rot[13]);
            Mix(ref block[2], ref block[5], rot[14]);
            Mix(ref block[4], ref block[3], rot[15]);

            block[0] += key[dm9];
            block[1] += key[dm9 + 1];
            block[2] += key[dm9 + 2];
            block[3] += key[dm9 + 3];
            block[4] += key[dm9 + 4];
            block[5] += key[dm9 + 5] + tweak[dm3];
            block[6] += key[dm9 + 6] + tweak[dm3 + 1];
            block[7] += key[dm9 + 7] + (ulong)d;

            Mix(ref block[0], ref block[1], rot[16]);
            Mix(ref block[2], ref block[3], rot[17]);
            Mix(ref block[4], ref block[5], rot[18]);
            Mix(ref block[6], ref block[7], rot[19]);
            Mix(ref block[2], ref block[1], rot[20]);
            Mix(ref block[4], ref block[7], rot[21]);
            Mix(ref block[6], ref block[5], rot[22]);
            Mix(ref block[0], ref block[3], rot[23]);
            Mix(ref block[4], ref block[1], rot[24]);
            Mix(ref block[6], ref block[3], rot[25]);
            Mix(ref block[0], ref block[5], rot[26]);
            Mix(ref block[2], ref block[7], rot[27]);
            Mix(ref block[6], ref block[1], rot[28]);
            Mix(ref block[0], ref block[7], rot[29]);
            Mix(ref block[2], ref block[5], rot[30]);
            Mix(ref block[4], ref block[3], rot[31]);

            block[0] += key[dm9 + 1];
            block[1] += key[dm9 + 2];
            block[2] += key[dm9 + 3];
            block[3] += key[dm9 + 4];
            block[4] += key[dm9 + 5];
            block[5] += key[dm9 + 6] + tweak[dm3 + 1];
            block[6] += key[dm9 + 7] + tweak[dm3 + 2];
            block[7] += key[dm9 + 8] + (ulong)(d + 1);
        }

        MemoryMarshal.Cast<ulong, byte>(block).CopyTo(output);
    }
}
