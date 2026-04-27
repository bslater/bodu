// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2s.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>BLAKE2s</c> cryptographic hash algorithm, designed by Jean-Philippe Aumasson,
/// Samuel Neves, Zooko Wilcox-O'Hearn, and Christian Winnerlein. Supports output sizes of 128, 160, 192, 224,
/// or 256 bits. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// BLAKE2s is specified in <see href="https://www.rfc-editor.org/rfc/rfc7693">RFC 7693</see> and is optimised for
/// 8-bit to 32-bit platforms. It operates on 64-byte (512-bit) blocks and maintains eight 32-bit state words,
/// applying 10 rounds of the BLAKE2 <c>G</c> mixing function per block.
/// </para>
/// <para>
/// This implementation inherits its residual buffer, byte-counter and lookahead-buffering loop from
/// <see cref="DeferredFinalBlockHashAlgorithm{T}" />: the final message block is not compressed until
/// <see cref="HashAlgorithm.HashFinal" /> is called, at which point the <c>finalization</c> flag is set and the
/// output bytes are serialised in little-endian order then truncated to the configured output length.
/// </para>
/// <para>
/// Keyed and tree-hashing modes are not currently exposed; this implementation targets the sequential,
/// unkeyed digest profile.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var blake2s = new Blake2s(256);
/// byte[] digest = blake2s.ComputeHash(message);
/// </code>
/// </example>
public sealed class Blake2s : DeferredFinalBlockHashAlgorithm<Blake2s>
{
    /// <summary>
    /// The set of output sizes, in bits, accepted by this algorithm.
    /// </summary>
    public static readonly int[] ValidHashSizes = { 128, 160, 192, 224, 256 };

    /// <summary>
    /// The block size, in bytes, processed by each compression call.
    /// </summary>
    private const int BlockSizeBytesValue = 64;

    /// <summary>
    /// The SHA-256 initialisation constants used as the BLAKE2s IV.
    /// </summary>
    private static readonly uint[] s_iv = new uint[8]
    {
        0x6A09E667U, 0xBB67AE85U,
        0x3C6EF372U, 0xA54FF53AU,
        0x510E527FU, 0x9B05688CU,
        0x1F83D9ABU, 0x5BE0CD19U,
    };

    /// <summary>
    /// The BLAKE2 sigma permutation table (10 rows; rounds are taken modulo 10).
    /// </summary>
    private static readonly byte[][] s_sigma = new byte[10][]
    {
        new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 },
        new byte[] { 14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3 },
        new byte[] { 11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4 },
        new byte[] { 7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8 },
        new byte[] { 9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13 },
        new byte[] { 2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9 },
        new byte[] { 12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11 },
        new byte[] { 13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10 },
        new byte[] { 6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5 },
        new byte[] { 10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0 },
    };

    /// <summary>
    /// The eight 32-bit internal hash state words.
    /// </summary>
    private readonly uint[] _h = new uint[8];

    /// <summary>
    /// Initialises a new instance of the <see cref="Blake2s" /> class with a 256-bit output hash size.
    /// </summary>
    public Blake2s()
        : this(256)
    { }

    /// <summary>
    /// Initialises a new instance of the <see cref="Blake2s" /> class with the specified output size.
    /// </summary>
    /// <param name="hashSize">
    /// The desired output size in bits. Must be one of 128, 160, 192, 224, or 256.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported output sizes.
    /// </exception>
    public Blake2s(int hashSize)
        : base(BlockSizeBytesValue)
    {
        if (Array.IndexOf(ValidHashSizes, hashSize) < 0)
            throw new ArgumentOutOfRangeException(
                nameof(hashSize),
                string.Format(ResourceStrings.CryptographicException_InvalidHashSize, hashSize, string.Join(", ", ValidHashSizes)));

        this.HashSizeValue = hashSize;
        this.InitializeState();
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    public override int InputBlockSize => BlockSizeBytesValue;

    /// <inheritdoc />
    public override int OutputBlockSize => this.HashSizeValue / 8;

    /// <summary>
    /// Gets or sets the size, in bits, of the final computed hash output.
    /// </summary>
    /// <value>The output size in bits; must be one of 128, 160, 192, 224, or 256.</value>
    /// <returns>The currently configured output size in bits.</returns>
    /// <remarks>
    /// The full BLAKE2s compression is always run using all 256 bits of internal state. Shorter output lengths
    /// are produced by truncating the serialised state after finalisation. The property may only be changed
    /// before hashing has begun; once <see cref="HashAlgorithm.TransformBlock" /> or a <c>ComputeHash</c>
    /// overload has been called, the value is immutable until <see cref="Initialize" /> is called.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The assigned value is not one of 128, 160, 192, 224, or 256.
    /// </exception>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// A hash computation is already in progress.
    /// </exception>
    public new int HashSize
    {
        get
        {
            this.ThrowIfDisposed();
            return this.HashSizeValue;
        }

        set
        {
            this.ThrowIfDisposed();
            this.ThrowIfInvalidState();

            if (Array.IndexOf(ValidHashSizes, value) < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    string.Format(ResourceStrings.CryptographicException_InvalidHashSize, value, string.Join(", ", ValidHashSizes)));

            this.HashSizeValue = value;
        }
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        this.ThrowIfDisposed();
        base.Initialize();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Restores the internal hash-state words to the BLAKE2s initialisation values for the configured output size.
    /// Invoked from <see cref="DeferredFinalBlockHashAlgorithm{T}.Initialize" /> after the inherited residual buffer
    /// and counter have been cleared.
    /// </remarks>
    protected override void OnInitialize() =>
        this.InitializeState();

    /// <inheritdoc />
    /// <remarks>
    /// Clears the chaining state, releases the framework <see cref="HashAlgorithm.HashValue" /> array, and zeros
    /// <see cref="HashAlgorithm.HashSizeValue" />. The inherited residual buffer is cleared by the grandparent
    /// before this hook runs.
    /// </remarks>
    protected override void OnDispose(bool disposing)
    {
        if (disposing)
        {
            Array.Clear(this._h, 0, this._h.Length);
            CryptoHelpers.ClearAndNullify(ref this.HashValue);
            this.HashSizeValue = 0;
        }
    }

    /// <summary>
    /// Compresses a single 64-byte block using the BLAKE2s <c>F</c> compression function. Invoked by
    /// <see cref="DeferredFinalBlockHashAlgorithm{T}" /> with <paramref name="isFinal" /> set to <see langword="true" />
    /// for the last call (which inverts the finalisation flag word) and to <see langword="false" /> otherwise.
    /// </summary>
    /// <param name="block">The 64-byte block to compress.</param>
    /// <param name="totalBytesIncludingThisBlock">
    /// The cumulative byte count <em>including</em> the bytes in <paramref name="block" />. Used as the per-block
    /// counter (the BLAKE2 <c>t0</c> / <c>t1</c> input pair).
    /// </param>
    /// <param name="isFinal">
    /// <see langword="true" /> if this is the final block; causes the finalisation flag word to be inverted.
    /// </param>
    protected override void ProcessBlock(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal)
    {
        // Read the 16 message words in little-endian order.
        Span<uint> m = stackalloc uint[16];
        for (int i = 0; i < 16; i++)
            m[i] = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(i * 4, 4));

        // Initialise the 16-element working vector.
        Span<uint> v = stackalloc uint[16];
        v[0] = this._h[0];
        v[1] = this._h[1];
        v[2] = this._h[2];
        v[3] = this._h[3];
        v[4] = this._h[4];
        v[5] = this._h[5];
        v[6] = this._h[6];
        v[7] = this._h[7];
        v[8] = s_iv[0];
        v[9] = s_iv[1];
        v[10] = s_iv[2];
        v[11] = s_iv[3];
        v[12] = s_iv[4] ^ (uint)(totalBytesIncludingThisBlock & 0xFFFFFFFFUL);   // counter low word
        v[13] = s_iv[5] ^ (uint)(totalBytesIncludingThisBlock >> 32);            // counter high word
        v[14] = s_iv[6];
        v[15] = s_iv[7];

        if (isFinal)
            v[14] = ~v[14];

        // 10 rounds of G mixing.
        for (int r = 0; r < 10; r++)
        {
            byte[] s = s_sigma[r % 10];

            G(v, 0, 4, 8, 12, m[s[0]], m[s[1]]);
            G(v, 1, 5, 9, 13, m[s[2]], m[s[3]]);
            G(v, 2, 6, 10, 14, m[s[4]], m[s[5]]);
            G(v, 3, 7, 11, 15, m[s[6]], m[s[7]]);
            G(v, 0, 5, 10, 15, m[s[8]], m[s[9]]);
            G(v, 1, 6, 11, 12, m[s[10]], m[s[11]]);
            G(v, 2, 7, 8, 13, m[s[12]], m[s[13]]);
            G(v, 3, 4, 9, 14, m[s[14]], m[s[15]]);
        }

        // Fold the working vector back into the hash state.
        for (int i = 0; i < 8; i++)
            this._h[i] ^= v[i] ^ v[i + 8];
    }

    /// <inheritdoc />
    /// <remarks>
    /// Serialises the eight 32-bit state words in little-endian order and truncates the result to the configured
    /// output length (which need not be a multiple of four bytes).
    /// </remarks>
    protected override byte[] ProcessFinalBlock()
    {
        int outputBytes = this.HashSizeValue / 8;
        byte[] output = new byte[outputBytes];
        int wordCount = (outputBytes + 3) / 4;

        for (int i = 0; i < wordCount; i++)
        {
            Span<byte> wordSpan = output.AsSpan(i * 4, Math.Min(4, outputBytes - i * 4));
            if (wordSpan.Length == 4)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(wordSpan, this._h[i]);
            }
            else
            {
                // Final word may be a partial write when the output size is not a multiple of 4 bytes.
                Span<byte> tmp = stackalloc byte[4];
                BinaryPrimitives.WriteUInt32LittleEndian(tmp, this._h[i]);
                tmp.Slice(0, wordSpan.Length).CopyTo(wordSpan);
            }
        }

        return output;
    }

    /// <summary>
    /// Sets the eight internal hash-state words to the BLAKE2s initialisation values, then applies the
    /// parameter block XOR for an unkeyed digest of <see cref="HashAlgorithm.HashSizeValue" /> / 8 bytes.
    /// </summary>
    private void InitializeState()
    {
        s_iv.CopyTo(this._h, 0);

        // Parameter block: fan-out=1, max depth=1, digest length=nn, no key (kk=0).
        // h[0] ^= 0x01010000 ^ (kk << 8) ^ nn
        int nn = this.HashSizeValue / 8;
        this._h[0] ^= 0x01010000U ^ (uint)nn;
    }

    /// <summary>
    /// Applies the BLAKE2s <c>G</c> mixing function to four elements of the working vector.
    /// </summary>
    /// <param name="v">The 16-element working vector.</param>
    /// <param name="a">Index of the first element.</param>
    /// <param name="b">Index of the second element.</param>
    /// <param name="c">Index of the third element.</param>
    /// <param name="d">Index of the fourth element.</param>
    /// <param name="x">The first message word for this mix.</param>
    /// <param name="y">The second message word for this mix.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void G(Span<uint> v, int a, int b, int c, int d, uint x, uint y)
    {
        v[a] += v[b] + x;
        v[d] = RotateRight(v[d] ^ v[a], 16);
        v[c] += v[d];
        v[b] = RotateRight(v[b] ^ v[c], 12);
        v[a] += v[b] + y;
        v[d] = RotateRight(v[d] ^ v[a], 8);
        v[c] += v[d];
        v[b] = RotateRight(v[b] ^ v[c], 7);
    }

    /// <summary>
    /// Rotates a 32-bit unsigned integer right by the specified number of bits.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="bits">The number of positions to rotate right.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateRight(uint value, int bits) =>
        (value >> bits) | (value << (32 - bits));
}
