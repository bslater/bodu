// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.256.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Implements the <c>Threefish-256</c> block cipher, which operates on 256-bit (32-byte) blocks using a 256-bit key and a 128-bit
/// tweak.
/// </summary>
/// <remarks>
/// <para>
/// Threefish is a tweakable block cipher optimised for 64-bit platforms and forms the core primitive of the Skein hash function.
/// The <c>Threefish-256</c> variant operates on four 64-bit words over 72 rounds using modular addition, bitwise rotation, and XOR.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Threefish256"/> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> contract. Use <see cref="Threefish256Cipher"/> directly only
/// when composing the raw block primitive with an <see cref="IBlockCipherModeTransform"/> (for example via
/// <see cref="BlockCipherModeFactory"/>) or with an <see cref="IPaddingStrategy"/>.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs. SymmetricAlgorithm</seealso>
/// <seealso cref="Threefish256"/>
public sealed class Threefish256Cipher
    : ThreefishBlockCipher
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Threefish256Cipher"/> class using the specified key and tweak.
    /// </summary>
    /// <param name="key">The 256-bit (32-byte) key used for encryption and decryption.</param>
    /// <param name="tweak">The 128-bit (16-byte) tweak value used to modify the block cipher behaviour.</param>
    public Threefish256Cipher(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : base(key, tweak) { }

    /// <summary>
    /// Length of a Threefish-256 key (bytes).
    /// </summary>
    public const int KeySize = 32;

    /// <inheritdoc />
    public override int BlockSize => 256;

    /// <inheritdoc />
    protected override int BlockWords => 4;

    /// <inheritdoc />
    protected override int[] RotationSchedule =>
    [
        14, 16, 52, 57, 23, 40, 5, 37,
        25, 33, 46, 12, 58, 22, 32, 32
    ];

    /// <inheritdoc />
    protected override int Rounds => 72;

    /// <summary>
    /// Decrypts a single 32-byte ciphertext block using the <c>Threefish-256</c> cipher and writes the result to the specified output buffer.
    /// </summary>
    /// <param name="input">The 32-byte ciphertext block to decrypt.</param>
    /// <param name="output">The 32-byte buffer to receive the decrypted plaintext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input"/> or <paramref name="output"/> is not 32 bytes.</exception>
    public override void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != this.BlockSize / 8 || output.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidBlockLength, this.BlockSize / 8));

        Span<ulong> block = stackalloc ulong[this.BlockWords];
        MemoryMarshal.Cast<byte, ulong>(input).CopyTo(block);

        var key = this._keySchedule;
        var tweak = this._tweakSchedule;
        var rot = this.RotationSchedule; // Use indexed rotation constants

        for (var d = (this.Rounds / 4) - 1; d >= 1; d -= 2)
        {
            var dm5 = d % 5;
            var dm3 = d % 3;

            // Reverse post-round subkey injection
            block[0] -= key[dm5 + 1];
            block[1] -= key[dm5 + 2] + tweak[dm3 + 1];
            block[2] -= key[dm5 + 3] + tweak[dm3 + 2];
            block[3] -= key[dm5 + 4] + (uint)(d + 1);

            // Reverse second 4 rounds
            Unmix(ref block[2], ref block[1], rot[15]);
            Unmix(ref block[0], ref block[3], rot[14]);
            Unmix(ref block[2], ref block[3], rot[13]);
            Unmix(ref block[0], ref block[1], rot[12]);
            Unmix(ref block[2], ref block[1], rot[11]);
            Unmix(ref block[0], ref block[3], rot[10]);
            Unmix(ref block[2], ref block[3], rot[9]);
            Unmix(ref block[0], ref block[1], rot[8]);

            // Reverse mid-round subkey injection
            block[0] -= key[dm5];
            block[1] -= key[dm5 + 1] + tweak[dm3];
            block[2] -= key[dm5 + 2] + tweak[dm3 + 1];
            block[3] -= key[dm5 + 3] + (uint)d;

            // Reverse first 4 rounds
            Unmix(ref block[2], ref block[1], rot[7]);
            Unmix(ref block[0], ref block[3], rot[6]);
            Unmix(ref block[2], ref block[3], rot[5]);
            Unmix(ref block[0], ref block[1], rot[4]);
            Unmix(ref block[2], ref block[1], rot[3]);
            Unmix(ref block[0], ref block[3], rot[2]);
            Unmix(ref block[2], ref block[3], rot[1]);
            Unmix(ref block[0], ref block[1], rot[0]);
        }

        // Final subkey removal (round 0)
        block[0] -= key[0];
        block[1] -= key[1] + tweak[0];
        block[2] -= key[2] + tweak[1];
        block[3] -= key[3];

        MemoryMarshal.Cast<ulong, byte>(block).CopyTo(output);
    }

    /// <summary>
    /// Encrypts a single 32-byte plaintext block using the <c>Threefish-256</c> cipher and writes the result to the specified output buffer.
    /// </summary>
    /// <param name="input">The 32-byte plaintext block to encrypt.</param>
    /// <param name="output">The 32-byte buffer to receive the encrypted ciphertext block.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the cipher has been disposed.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="input"/> or <paramref name="output"/> is not 32 bytes.</exception>
    public override void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        if (input.Length != this.BlockSize / 8 || output.Length != this.BlockSize / 8)
            throw new ArgumentException(
                string.Format(CryptoResourceStrings.CryptographicException_InvalidBlockLength, this.BlockSize / 8));

        Span<ulong> block = stackalloc ulong[this.BlockWords];
        MemoryMarshal.Cast<byte, ulong>(input).CopyTo(block);

        var key = this._keySchedule;
        var tweak = this._tweakSchedule;
        var rot = this.RotationSchedule;

        // Initial key injection (round 0)
        block[0] += key[0x00];
        block[1] += key[0x01] + tweak[0x00];
        block[2] += key[0x02] + tweak[0x01];
        block[3] += key[0x03];

        for (var d = 1; d < this.Rounds / 4; d += 2)
        {
            var dm5 = d % 5;
            var dm3 = d % 3;

            // First 4 MIX rounds
            Mix(ref block[0], ref block[1], rot[0]);
            Mix(ref block[2], ref block[3], rot[1]);
            Mix(ref block[0], ref block[3], rot[2]);
            Mix(ref block[2], ref block[1], rot[3]);
            Mix(ref block[0], ref block[1], rot[4]);
            Mix(ref block[2], ref block[3], rot[5]);
            Mix(ref block[0], ref block[3], rot[6]);
            Mix(ref block[2], ref block[1], rot[7]);

            // Mid-round subkey injection
            block[0] += key[dm5];
            block[1] += key[dm5 + 1] + tweak[dm3];
            block[2] += key[dm5 + 2] + tweak[dm3 + 1];
            block[3] += key[dm5 + 3] + (ulong)d;

            // Second 4 MIX rounds
            Mix(ref block[0], ref block[1], rot[8]);
            Mix(ref block[2], ref block[3], rot[9]);
            Mix(ref block[0], ref block[3], rot[10]);
            Mix(ref block[2], ref block[1], rot[11]);
            Mix(ref block[0], ref block[1], rot[12]);
            Mix(ref block[2], ref block[3], rot[13]);
            Mix(ref block[0], ref block[3], rot[14]);
            Mix(ref block[2], ref block[1], rot[15]);

            // Post-round subkey injection
            block[0] += key[dm5 + 1];
            block[1] += key[dm5 + 2] + tweak[dm3 + 1];
            block[2] += key[dm5 + 3] + tweak[dm3 + 2];
            block[3] += key[dm5 + 4] + (ulong)d + 1;
        }

        MemoryMarshal.Cast<ulong, byte>(block).CopyTo(output);
    }
}
