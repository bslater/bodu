// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.1024.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the <c>Threefish-1024</c> block cipher, which operates on 1024-bit (128-byte) blocks using a 1024-bit key and a
/// 128-bit tweak.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is a tweakable block cipher optimized for 64-bit platforms and forms the core primitive of the Skein hash function.
/// The <c>Threefish-1024</c> variant operates on sixteen 64-bit words over 80 rounds using modular addition, bitwise rotation, and XOR.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Threefish1024"/> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> contract. Use <see cref="Threefish1024Cipher"/> directly only
/// when composing the raw block primitive with an <see cref="IBlockCipherModeTransform"/> (for example via
/// <see cref="BlockCipherModeFactory"/>) or with an <see cref="IPaddingStrategy"/>.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs. SymmetricAlgorithm</seealso>
/// <seealso cref="Threefish1024"/>
public sealed class Threefish1024Cipher
    : ThreefishBlockCipher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish1024Cipher"/> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 1024-bit (128-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behavior.</param>
    public Threefish1024Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <summary>
    /// Length of the Threefish-1024 key is 1024 bits (128 bytes).
    /// </summary>
    public const int KeySize = 1024;

    /// <inheritdoc />
    /// <value>Length of the Threefish-1024 block is 1024 bits (128 bytes).</value>
    public override int BlockSize => 1024;

    /// <inheritdoc />
    protected override int BlockWords => 16;

    /// <inheritdoc />
#pragma warning disable SA1137 // Elements should have the same indentation
    protected override int[] RotationSchedule =>
    [
        24, 13,  8, 47,  8, 17, 22, 37,
        38, 19, 10, 55, 49, 18, 23, 52,
        33,  4, 51, 13, 34, 41, 59, 17,
         5, 20, 48, 41, 47, 28, 16, 25,
        41,  9, 37, 31, 12, 47, 44, 30,
        16, 34, 56, 51,  4, 53, 42, 41,
        31, 44, 47, 46, 19, 42, 44, 25,
         9, 48, 35, 52, 23, 31, 37, 20
    ];
#pragma warning restore SA1137 // Elements should have the same indentation

    /// <inheritdoc />
    protected override int Rounds => 80;

    /// <summary>
    /// Decrypts a single 128-byte ciphertext block using the <c>Threefish-1024</c> cipher and writes the result to the specified output buffer.
    /// </summary>
    /// <param name="input">The 128-byte ciphertext block to decrypt.</param>
    /// <param name="output">The 128-byte buffer to receive the decrypted plaintext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input"/> or <paramref name="output"/> is not 128 bytes.</exception>
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
            int dm17 = d % 17, dm3 = d % 3;

            block[0] -= key[dm17 + 1];
            block[1] -= key[dm17 + 2];
            block[2] -= key[dm17 + 3];
            block[3] -= key[dm17 + 4];
            block[4] -= key[dm17 + 5];
            block[5] -= key[dm17 + 6];
            block[6] -= key[dm17 + 7];
            block[7] -= key[dm17 + 8];
            block[8] -= key[dm17 + 9];
            block[9] -= key[dm17 + 10];
            block[10] -= key[dm17 + 11];
            block[11] -= key[dm17 + 12];
            block[12] -= key[dm17 + 13];
            block[13] -= key[dm17 + 14] + tweak[dm3 + 1];
            block[14] -= key[dm17 + 15] + tweak[dm3 + 2];
            block[15] -= key[dm17 + 16] + (uint)d + 1;

            Unmix(ref block[12], ref block[7], rot[63]);
            Unmix(ref block[10], ref block[3], rot[62]);
            Unmix(ref block[8], ref block[5], rot[61]);
            Unmix(ref block[14], ref block[1], rot[60]);
            Unmix(ref block[4], ref block[9], rot[59]);
            Unmix(ref block[6], ref block[13], rot[58]);
            Unmix(ref block[2], ref block[11], rot[57]);
            Unmix(ref block[0], ref block[15], rot[56]);

            Unmix(ref block[10], ref block[9], rot[55]);
            Unmix(ref block[8], ref block[11], rot[54]);
            Unmix(ref block[14], ref block[13], rot[53]);
            Unmix(ref block[12], ref block[15], rot[52]);
            Unmix(ref block[6], ref block[1], rot[51]);
            Unmix(ref block[4], ref block[3], rot[50]);
            Unmix(ref block[2], ref block[5], rot[49]);
            Unmix(ref block[0], ref block[7], rot[48]);

            Unmix(ref block[8], ref block[1], rot[47]);
            Unmix(ref block[14], ref block[5], rot[46]);
            Unmix(ref block[12], ref block[3], rot[45]);
            Unmix(ref block[10], ref block[7], rot[44]);
            Unmix(ref block[4], ref block[15], rot[43]);
            Unmix(ref block[6], ref block[11], rot[42]);
            Unmix(ref block[2], ref block[13], rot[41]);
            Unmix(ref block[0], ref block[9], rot[40]);

            Unmix(ref block[14], ref block[15], rot[39]);
            Unmix(ref block[12], ref block[13], rot[38]);
            Unmix(ref block[10], ref block[11], rot[37]);
            Unmix(ref block[8], ref block[9], rot[36]);
            Unmix(ref block[6], ref block[7], rot[35]);
            Unmix(ref block[4], ref block[5], rot[34]);
            Unmix(ref block[2], ref block[3], rot[33]);
            Unmix(ref block[0], ref block[1], rot[32]);

            block[0] -= key[dm17];
            block[1] -= key[dm17 + 1];
            block[2] -= key[dm17 + 2];
            block[3] -= key[dm17 + 3];
            block[4] -= key[dm17 + 4];
            block[5] -= key[dm17 + 5];
            block[6] -= key[dm17 + 6];
            block[7] -= key[dm17 + 7];
            block[8] -= key[dm17 + 8];
            block[9] -= key[dm17 + 9];
            block[10] -= key[dm17 + 10];
            block[11] -= key[dm17 + 11];
            block[12] -= key[dm17 + 12];
            block[13] -= key[dm17 + 13] + tweak[dm3];
            block[14] -= key[dm17 + 14] + tweak[dm3 + 1];
            block[15] -= key[dm17 + 15] + (uint)d;

            Unmix(ref block[12], ref block[7], rot[31]);
            Unmix(ref block[10], ref block[3], rot[30]);
            Unmix(ref block[8], ref block[5], rot[29]);
            Unmix(ref block[14], ref block[1], rot[28]);
            Unmix(ref block[4], ref block[9], rot[27]);
            Unmix(ref block[6], ref block[13], rot[26]);
            Unmix(ref block[2], ref block[11], rot[25]);
            Unmix(ref block[0], ref block[15], rot[24]);

            Unmix(ref block[10], ref block[9], rot[23]);
            Unmix(ref block[8], ref block[11], rot[22]);
            Unmix(ref block[14], ref block[13], rot[21]);
            Unmix(ref block[12], ref block[15], rot[20]);
            Unmix(ref block[6], ref block[1], rot[19]);
            Unmix(ref block[4], ref block[3], rot[18]);
            Unmix(ref block[2], ref block[5], rot[17]);
            Unmix(ref block[0], ref block[7], rot[16]);

            Unmix(ref block[8], ref block[1], rot[15]);
            Unmix(ref block[14], ref block[5], rot[14]);
            Unmix(ref block[12], ref block[3], rot[13]);
            Unmix(ref block[10], ref block[7], rot[12]);
            Unmix(ref block[4], ref block[15], rot[11]);
            Unmix(ref block[6], ref block[11], rot[10]);
            Unmix(ref block[2], ref block[13], rot[9]);
            Unmix(ref block[0], ref block[9], rot[8]);

            Unmix(ref block[14], ref block[15], rot[7]);
            Unmix(ref block[12], ref block[13], rot[6]);
            Unmix(ref block[10], ref block[11], rot[5]);
            Unmix(ref block[8], ref block[9], rot[4]);
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
        block[5] -= key[5];
        block[6] -= key[6];
        block[7] -= key[7];
        block[8] -= key[8];
        block[9] -= key[9];
        block[10] -= key[10];
        block[11] -= key[11];
        block[12] -= key[12];
        block[13] -= key[13] + tweak[0];
        block[14] -= key[14] + tweak[1];
        block[15] -= key[15];

        MemoryMarshal.Cast<ulong, byte>(block).CopyTo(output);
    }

    /// <summary>
    /// Encrypts a single 128-byte plaintext block using the <c>Threefish-1024</c> cipher and writes the result to the specified output buffer.
    /// </summary>
    /// <param name="input">The 128-byte plaintext block to encrypt.</param>
    /// <param name="output">The 128-byte buffer to receive the encrypted ciphertext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input"/> or <paramref name="output"/> is not 128 bytes.</exception>
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
        block[5] += key[5];
        block[6] += key[6];
        block[7] += key[7];
        block[8] += key[8];
        block[9] += key[9];
        block[10] += key[10];
        block[11] += key[11];
        block[12] += key[12];
        block[13] += key[13] + tweak[0];
        block[14] += key[14] + tweak[1];
        block[15] += key[15];

        for (var d = 1; d < this.Rounds / 4; d += 2)
        {
            int dm17 = d % 17, dm3 = d % 3;

            Mix(ref block[0], ref block[1], rot[0]);
            Mix(ref block[2], ref block[3], rot[1]);
            Mix(ref block[4], ref block[5], rot[2]);
            Mix(ref block[6], ref block[7], rot[3]);
            Mix(ref block[8], ref block[9], rot[4]);
            Mix(ref block[10], ref block[11], rot[5]);
            Mix(ref block[12], ref block[13], rot[6]);
            Mix(ref block[14], ref block[15], rot[7]);
            Mix(ref block[0], ref block[9], rot[8]);
            Mix(ref block[2], ref block[13], rot[9]);
            Mix(ref block[6], ref block[11], rot[10]);
            Mix(ref block[4], ref block[15], rot[11]);
            Mix(ref block[10], ref block[7], rot[12]);
            Mix(ref block[12], ref block[3], rot[13]);
            Mix(ref block[14], ref block[5], rot[14]);
            Mix(ref block[8], ref block[1], rot[15]);
            Mix(ref block[0], ref block[7], rot[16]);
            Mix(ref block[2], ref block[5], rot[17]);
            Mix(ref block[4], ref block[3], rot[18]);
            Mix(ref block[6], ref block[1], rot[19]);
            Mix(ref block[12], ref block[15], rot[20]);
            Mix(ref block[14], ref block[13], rot[21]);
            Mix(ref block[8], ref block[11], rot[22]);
            Mix(ref block[10], ref block[9], rot[23]);
            Mix(ref block[0], ref block[15], rot[24]);
            Mix(ref block[2], ref block[11], rot[25]);
            Mix(ref block[6], ref block[13], rot[26]);
            Mix(ref block[4], ref block[9], rot[27]);
            Mix(ref block[14], ref block[1], rot[28]);
            Mix(ref block[8], ref block[5], rot[29]);
            Mix(ref block[10], ref block[3], rot[30]);
            Mix(ref block[12], ref block[7], rot[31]);

            block[0] += key[dm17];
            block[1] += key[dm17 + 1];
            block[2] += key[dm17 + 2];
            block[3] += key[dm17 + 3];
            block[4] += key[dm17 + 4];
            block[5] += key[dm17 + 5];
            block[6] += key[dm17 + 6];
            block[7] += key[dm17 + 7];
            block[8] += key[dm17 + 8];
            block[9] += key[dm17 + 9];
            block[10] += key[dm17 + 10];
            block[11] += key[dm17 + 11];
            block[12] += key[dm17 + 12];
            block[13] += key[dm17 + 13] + tweak[dm3];
            block[14] += key[dm17 + 14] + tweak[dm3 + 1];
            block[15] += key[dm17 + 15] + (uint)d;

            Mix(ref block[0], ref block[1], rot[32]);
            Mix(ref block[2], ref block[3], rot[33]);
            Mix(ref block[4], ref block[5], rot[34]);
            Mix(ref block[6], ref block[7], rot[35]);
            Mix(ref block[8], ref block[9], rot[36]);
            Mix(ref block[10], ref block[11], rot[37]);
            Mix(ref block[12], ref block[13], rot[38]);
            Mix(ref block[14], ref block[15], rot[39]);
            Mix(ref block[0], ref block[9], rot[40]);
            Mix(ref block[2], ref block[13], rot[41]);
            Mix(ref block[6], ref block[11], rot[42]);
            Mix(ref block[4], ref block[15], rot[43]);
            Mix(ref block[10], ref block[7], rot[44]);
            Mix(ref block[12], ref block[3], rot[45]);
            Mix(ref block[14], ref block[5], rot[46]);
            Mix(ref block[8], ref block[1], rot[47]);
            Mix(ref block[0], ref block[7], rot[48]);
            Mix(ref block[2], ref block[5], rot[49]);
            Mix(ref block[4], ref block[3], rot[50]);
            Mix(ref block[6], ref block[1], rot[51]);
            Mix(ref block[12], ref block[15], rot[52]);
            Mix(ref block[14], ref block[13], rot[53]);
            Mix(ref block[8], ref block[11], rot[54]);
            Mix(ref block[10], ref block[9], rot[55]);
            Mix(ref block[0], ref block[15], rot[56]);
            Mix(ref block[2], ref block[11], rot[57]);
            Mix(ref block[6], ref block[13], rot[58]);
            Mix(ref block[4], ref block[9], rot[59]);
            Mix(ref block[14], ref block[1], rot[60]);
            Mix(ref block[8], ref block[5], rot[61]);
            Mix(ref block[10], ref block[3], rot[62]);
            Mix(ref block[12], ref block[7], rot[63]);

            block[0] += key[dm17 + 1];
            block[1] += key[dm17 + 2];
            block[2] += key[dm17 + 3];
            block[3] += key[dm17 + 4];
            block[4] += key[dm17 + 5];
            block[5] += key[dm17 + 6];
            block[6] += key[dm17 + 7];
            block[7] += key[dm17 + 8];
            block[8] += key[dm17 + 9];
            block[9] += key[dm17 + 10];
            block[10] += key[dm17 + 11];
            block[11] += key[dm17 + 12];
            block[12] += key[dm17 + 13];
            block[13] += key[dm17 + 14] + tweak[dm3 + 1];
            block[14] += key[dm17 + 15] + tweak[dm3 + 2];
            block[15] += key[dm17 + 16] + (uint)d + 1;
        }

        MemoryMarshal.Cast<ulong, byte>(block).CopyTo(output);
    }
}
