// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishBlockCipher.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the core Blowfish block cipher engine, implementing low-level encryption and decryption of individual 64-bit blocks.
/// </summary>
/// <remarks>
/// <para>
/// This class implements the Blowfish symmetric block cipher designed by Bruce Schneier. It operates on 64-bit (8-byte) blocks and
/// accepts a variable-length key of between 32 and 448 bits (4 to 56 bytes). The cipher uses a 16-round Feistel network with four
/// 256-entry, 32-bit S-boxes and an 18-entry 32-bit P-array, all initialised from the hexadecimal digits of pi (π).
/// </para>
/// <para>
/// The block operation follows the Blowfish specification directly: split the 64-bit block into two big-endian 32-bit halves,
/// apply 16 Feistel rounds using the expanded P-array and the four key-dependent S-boxes, undo the final Feistel swap, and apply
/// the final two P-array words as output whitening. Decryption walks the same Feistel structure in reverse P-array order.
/// </para>
/// <para>
/// Key schedule expansion is performed in full during construction. The pi-derived P-array is first XORed with cyclic 32-bit
/// key words, then the all-zero block is repeatedly encrypted to replace every P-array entry and every S-box entry with
/// key-dependent values. All sensitive expanded state is zeroed securely on disposal.
/// </para>
/// <para>
/// Most callers should prefer the higher-level <see cref="Blowfish"/> class, which exposes the standard
/// <see cref="System.Security.Cryptography.SymmetricAlgorithm"/> contract. Use <see cref="BlowfishBlockCipher"/> directly only
/// when composing the raw block primitive with an <see cref="IBlockCipherModeTransform"/> (for example via
/// <see cref="BlockCipherModeFactory"/>) or with an <see cref="IPaddingStrategy"/>.
/// </para>
/// </remarks>
/// <seealso href="../guides/cryptography/composing-primitives.html">Composing primitives — direct use vs. SymmetricAlgorithm</seealso>
/// <seealso cref="Blowfish"/>
public sealed partial class BlowfishBlockCipher
    : IBlockCipher
{
    /// <summary>
    /// Length of the Blowfish block in bytes. Blowfish always operates on one 64-bit block at a time.
    /// </summary>
    private const int BlockSizeInBytes = 8;

    /// <summary>
    /// Number of Feistel rounds defined by Blowfish.
    /// </summary>
    private const int FeistelRounds = 16;

    /// <summary>
    /// Number of 32-bit words in the Blowfish P-array. The first 16 entries are round subkeys and the final two entries are
    /// used after the final swap as whitening words.
    /// </summary>
    private const int PArrayLength = 18;

    /// <summary>
    /// Number of 32-bit entries in each Blowfish S-box.
    /// </summary>
    private const int SBoxLength = 256;

    // Mutable per-instance P-array. This starts as s_initP, is XORed with cyclic key words, and is then
    // replaced pair-by-pair during the all-zero-block expansion phase.
    private readonly uint[] _p = new uint[PArrayLength];

    // Mutable per-instance S-boxes. These start as the pi-derived constants and become key-dependent
    // lookup tables during key schedule expansion. They are secret-derived state and are cleared on disposal.
    private readonly uint[] _s0 = new uint[SBoxLength];
    private readonly uint[] _s1 = new uint[SBoxLength];
    private readonly uint[] _s2 = new uint[SBoxLength];
    private readonly uint[] _s3 = new uint[SBoxLength];

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlowfishBlockCipher"/> class using the specified key.
    /// </summary>
    /// <param name="key">
    /// The encryption key. Must be between 4 and 56 bytes (32 to 448 bits) in length.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is fewer than 4 bytes or more than 56 bytes in length.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The full Blowfish key schedule — including XOR of the P-array with the key bytes and repeated encryption of the all-zeros block
    /// to expand the P-array and all four S-boxes — is performed in full during construction.
    /// </para>
    /// </remarks>
    public BlowfishBlockCipher(ReadOnlySpan<byte> key)
    {
        ThrowHelper.ThrowIfSpanLengthOutOfRange(key, Blowfish.MinKeySize / 8, Blowfish.MaxKeySize / 8);

        // Blowfish has an intentionally expensive key schedule. Construction completes the full expansion so
        // Encrypt/Decrypt only execute the 16-round block transform.
        this.InitialiseKeySchedule(key);
    }

    /// <inheritdoc />
    /// <value>Length of the Blowfish block is 64 bits (8 bytes).</value>
    public int BlockSize => Blowfish.BlowFishBlockSize;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!this._disposed)
        {
            // Securely zero all sensitive key material.
            CryptoHelpers.Clear(this._p);
            CryptoHelpers.Clear(this._s0);
            CryptoHelpers.Clear(this._s1);
            CryptoHelpers.Clear(this._s2);
            CryptoHelpers.Clear(this._s3);

            this._disposed = true;
        }
    }

    /// <summary>
    /// Decrypts a single 64-bit block using the Blowfish cipher and writes the plaintext to <paramref name="output"/>.
    /// </summary>
    /// <param name="input">
    /// A read-only span containing the ciphertext block to decrypt. Must be at least <see cref="BlockSize"/> / 8 bytes in length.
    /// </param>
    /// <param name="output">
    /// A writable span to receive the decrypted plaintext block. Must be at least <see cref="BlockSize"/> / 8 bytes in length.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> or <paramref name="output"/> is shorter than <see cref="BlockSize"/> / 8 bytes.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The instance has been disposed.
    /// </exception>
    public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeInBytes);

        // Blowfish specifies a 64-bit block as two big-endian 32-bit halves.
        var xl = BinaryPrimitives.ReadUInt32BigEndian(input);
        var xr = BinaryPrimitives.ReadUInt32BigEndian(input[4..]);

        // Walk the Feistel network in reverse P-array order.
        this.DecipherBlock(ref xl, ref xr);

        BinaryPrimitives.WriteUInt32BigEndian(output, xl);
        BinaryPrimitives.WriteUInt32BigEndian(output[4..], xr);
    }

    /// <summary>
    /// Encrypts a single 64-bit block using the Blowfish cipher and writes the ciphertext to <paramref name="output"/>.
    /// </summary>
    /// <param name="input">
    /// A read-only span containing the plaintext block to encrypt. Must be at least <see cref="BlockSize"/> / 8 bytes in length.
    /// </param>
    /// <param name="output">
    /// A writable span to receive the encrypted ciphertext block. Must be at least <see cref="BlockSize"/> / 8 bytes in length.
    /// </param>
    /// <exception cref="ArgumentException">
    /// <paramref name="input"/> or <paramref name="output"/> is shorter than <see cref="BlockSize"/> / 8 bytes.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// The instance has been disposed.
    /// </exception>
    public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
    {
        this.ThrowIfDisposed();
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(input, BlockSizeInBytes);
        ThrowHelper.ThrowIfSpanLengthIsNotEqualTo(output, BlockSizeInBytes);

        // Blowfish specifies a 64-bit block as two big-endian 32-bit halves.
        var xl = BinaryPrimitives.ReadUInt32BigEndian(input);
        var xr = BinaryPrimitives.ReadUInt32BigEndian(input[4..]);

        // Apply the 16-round Feistel network using the fully expanded P-array and S-boxes.
        this.EncipherBlock(ref xl, ref xr);

        BinaryPrimitives.WriteUInt32BigEndian(output, xl);
        BinaryPrimitives.WriteUInt32BigEndian(output[4..], xr);
    }

    /// <summary>
    /// Applies the Blowfish F function to a 32-bit word.
    /// </summary>
    /// <param name="x">
    /// The 32-bit input word.
    /// </param>
    /// <returns>
    /// The transformed 32-bit output word used as the nonlinear Feistel contribution.
    /// </returns>
    /// <remarks>
    /// Splits <paramref name="x"/> into four bytes, looks up each byte in its corresponding key-dependent S-box, then combines
    /// the four 32-bit values using the Blowfish expression <c>((S0[a] + S1[b]) ^ S2[c]) + S3[d]</c>. The additions wrap
    /// modulo 2^32 through unsigned integer overflow semantics.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint F(uint x)
    {
        // Interpret the input as four most-significant-byte-first S-box indices.
        var a = this._s0[(int)(x >> 24)];
        var b = this._s1[(int)((x >> 16) & 0xFF)];
        var c = this._s2[(int)((x >> 8) & 0xFF)];
        var d = this._s3[(int)(x & 0xFF)];

        // Blowfish F: ((S0[a] + S1[b]) xor S2[c]) + S3[d].
        return ((a + b) ^ c) + d;
    }

    /// <summary>
    /// Performs the 16-round Blowfish Feistel encipher operation on two 32-bit halves.
    /// </summary>
    /// <param name="xl">The left half of the 64-bit block; updated in place.</param>
    /// <param name="xr">The right half of the 64-bit block; updated in place.</param>
    /// <remarks>
    /// Each round applies <c>XL ^= P[i]</c>, mixes the right half with <c>F(XL)</c>, and swaps the halves. After the
    /// sixteenth round the extra Feistel swap is undone, then <c>P[16]</c> and <c>P[17]</c> are applied as the final
    /// whitening words. This method is also used internally during key expansion while the P-array and S-boxes are still
    /// being generated.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EncipherBlock(ref uint xl, ref uint xr)
    {
        for (var i = 0; i < FeistelRounds; i++)
        {
            // Round i: XOR the left half with P[i], feed it through F, XOR into the right half, then swap.
            xl ^= this._p[i];
            xr ^= this.F(xl);
            (xl, xr) = (xr, xl);
        }

        // Undo the final swap and apply output whitening with P[16] and P[17].
        (xl, xr) = (xr, xl);
        xr ^= this._p[16];
        xl ^= this._p[17];
    }

    /// <summary>
    /// Performs the 16-round Blowfish Feistel decipher operation on two 32-bit halves.
    /// </summary>
    /// <param name="xl">The left half of the 64-bit block; updated in place.</param>
    /// <param name="xr">The right half of the 64-bit block; updated in place.</param>
    /// <remarks>
    /// Decryption uses the same Feistel step as encryption but consumes the expanded P-array in reverse order. The loop
    /// starts with <c>P[17]</c> and ends with <c>P[2]</c>; after the final swap is undone, <c>P[1]</c> and <c>P[0]</c>
    /// remove the original input whitening.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DecipherBlock(ref uint xl, ref uint xr)
    {
        for (var i = 17; i >= 2; i--)
        {
            // Reverse round i: same F-based Feistel step, with P-array entries consumed backwards.
            xl ^= this._p[i];
            xr ^= this.F(xl);
            (xl, xr) = (xr, xl);
        }

        // Undo the final swap and strip the original input whitening with P[1] and P[0].
        (xl, xr) = (xr, xl);
        xr ^= this._p[1];
        xl ^= this._p[0];
    }

    /// <summary>
    /// Performs the full Blowfish key schedule expansion.
    /// </summary>
    /// <param name="key">The raw Blowfish key (between 4 and 56 bytes).</param>
    /// <remarks>
    /// <para>
    /// Blowfish key expansion has three specification-defined phases. First, the mutable P-array and S-boxes are initialised
    /// from the hexadecimal digits of π. Second, each P-array word is XORed with a 32-bit big-endian word assembled from the
    /// supplied key, cycling through key bytes as required. Third, the all-zero block is repeatedly encrypted with the evolving
    /// cipher state to replace the P-array and all four S-boxes in pairs.
    /// </para>
    /// <para>
    /// After this method returns, all encryption and decryption operations use only the expanded, key-dependent P-array and
    /// S-boxes. The original key span is not retained.
    /// </para>
    /// </remarks>
    private void InitialiseKeySchedule(ReadOnlySpan<byte> key)
    {
        // Phase 1: copy the pi-derived initialisers into the mutable working arrays.
        s_initP.CopyTo(this._p, 0);
        s_initS0.CopyTo(this._s0, 0);
        s_initS1.CopyTo(this._s1, 0);
        s_initS2.CopyTo(this._s2, 0);
        s_initS3.CopyTo(this._s3, 0);

        // Phase 2: XOR the P-array entries with successive 32-bit big-endian words derived from the key.
        // If the key is shorter than the required 72 bytes, cycle through it exactly as the Blowfish specification requires.
        var keyLen = key.Length;
        var keyIndex = 0;

        for (var i = 0; i < PArrayLength; i++)
        {
            uint word = 0;
            for (var b = 0; b < 4; b++)
            {
                // Assemble the key material most-significant byte first before XORing it into P[i].
                word = (word << 8) | key[keyIndex];
                keyIndex = (keyIndex + 1) % keyLen;
            }

            this._p[i] ^= word;
        }

        // Phase 3: repeatedly encrypt the evolving all-zero block and replace P/S entries in pairs.
        // xl/xr retain the previous encryption result, so every replacement depends on all previous replacements.
        uint xl = 0;
        uint xr = 0;

        for (var i = 0; i < PArrayLength; i += 2)
        {
            this.EncipherBlock(ref xl, ref xr);
            this._p[i] = xl;
            this._p[i + 1] = xr;
        }

        // Expand all four S-boxes in P-array order. Each call uses the P-array and any S-box entries already replaced.
        for (var i = 0; i < SBoxLength; i += 2)
        {
            this.EncipherBlock(ref xl, ref xr);
            this._s0[i] = xl;
            this._s0[i + 1] = xr;
        }

        for (var i = 0; i < SBoxLength; i += 2)
        {
            this.EncipherBlock(ref xl, ref xr);
            this._s1[i] = xl;
            this._s1[i + 1] = xr;
        }

        for (var i = 0; i < SBoxLength; i += 2)
        {
            this.EncipherBlock(ref xl, ref xr);
            this._s2[i] = xl;
            this._s2[i + 1] = xr;
        }

        for (var i = 0; i < SBoxLength; i += 2)
        {
            this.EncipherBlock(ref xl, ref xr);
            this._s3[i] = xl;
            this._s3[i + 1] = xr;
        }
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this._disposed, this);
#else
        if (this._disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }
}
