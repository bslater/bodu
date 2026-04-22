// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Applies the EAX-mode encryption/decryption transformation to an underlying <see cref="IBlockCipher" />,
/// using CTR mode seeded from the supplied nonce.
/// </summary>
/// <remarks>
/// <para>
/// <img src="../images/diagrams/aead-mode.svg" alt="Generic AEAD data flow — EAX instantiates the top pipeline as CTR mode and the bottom pipeline as three OMAC invocations over the nonce, associated data, and ciphertext." />
/// </para>
/// <para>
/// EAX is the <b>CTR + OMAC(N ‖ A ‖ C)</b> instantiation of the generic AEAD shape above. The top
/// pipeline is the keystream generator shown in the diagram; the bottom pipeline is the MAC, specialised
/// to three independent OMAC invocations whose outputs are XOR-combined to form the tag.
/// </para>
/// <para>
/// EAX (Bellare, Rogaway, Wagner) is a two-pass authenticated encryption mode. The encryption component
/// is CTR mode, where successive counter values are encrypted and XORed with the plaintext or ciphertext:
/// <list type="bullet">
/// <item><description>Keystream_i = E(counter_i), counter incremented (big-endian) each block.</description></item>
/// <item><description>Both encrypt and decrypt: output_i = input_i ⊕ Keystream_i.</description></item>
/// </list>
/// </para>
/// <para>
/// In a full EAX implementation the initial counter is derived from OMAC_0(nonce) rather than the nonce
/// directly. This implementation accepts the nonce as the initial counter value, corresponding to the CTR
/// encryption component of EAX with the OMAC pre-processing step elided.
/// </para>
/// <para>
/// The authentication component — OMAC over the nonce, ciphertext, and optional associated data — requires
/// the <c>IAeadBlockCipherModeTransform</c> interface extension.
/// </para>
/// </remarks>
public sealed class EaxModeTransform : IBlockCipherModeTransform
{
    private readonly IBlockCipher cipher;
    private readonly byte[] counter; // current counter value, incremented big-endian each block

    /// <summary>
    /// Initialises a new instance of the <see cref="EaxModeTransform" /> class.
    /// </summary>
    /// <param name="cipher">The block cipher used to generate keystream blocks.</param>
    /// <param name="iv">
    /// The nonce used as the initial counter value. Must equal the cipher block size. A defensive copy
    /// is taken; the caller's array is not modified.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cipher" /> or <paramref name="iv" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="iv" /> length does not equal the cipher block size.
    /// </exception>
    public EaxModeTransform(IBlockCipher cipher, byte[] iv)
    {
        this.cipher = cipher ?? throw new ArgumentNullException(nameof(cipher));
        if (iv is null) throw new ArgumentNullException(nameof(iv));
        if (iv.Length != cipher.BlockSize)
            throw new ArgumentException(
                $"IV length ({iv.Length}) must equal the cipher block size ({cipher.BlockSize}).",
                nameof(iv));

        this.counter = (byte[])iv.Clone();
    }

    /// <inheritdoc />
    public int Transform(ReadOnlySpan<byte> input, Span<byte> output, bool encrypt)
    {
        int blockSize = this.cipher.BlockSize;
        ThrowHelper.ThrowIfSpanLengthNotPositiveMultipleOf(input, blockSize);
        ThrowHelper.ThrowIfSpanLengthIsInsufficient(output, 0, input.Length);

        Span<byte> keystream = stackalloc byte[blockSize];

        for (int offset = 0; offset < input.Length; offset += blockSize)
        {
            // Generate keystream block from the current counter (always uses encrypt primitive).
            this.cipher.Encrypt(this.counter, keystream);

            // XOR with input — identical operation for both encrypt and decrypt (CTR property).
            ReadOnlySpan<byte> inBlock = input.Slice(offset, blockSize);
            Span<byte> outBlock = output.Slice(offset, blockSize);
            for (int i = 0; i < blockSize; i++)
                outBlock[i] = (byte)(inBlock[i] ^ keystream[i]);

            // Advance counter (big-endian increment, rightmost byte first).
            IncrementBigEndian(this.counter);
        }

        return input.Length;
    }

    /// <summary>
    /// Increments <paramref name="counter" /> as an unsigned big-endian integer, wrapping on overflow.
    /// </summary>
    private static void IncrementBigEndian(byte[] counter)
    {
        for (int i = counter.Length - 1; i >= 0; i--)
            if (++counter[i] != 0) break;
    }
}
