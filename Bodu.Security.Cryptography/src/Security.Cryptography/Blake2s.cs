// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2s.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using Bodu.Extensions;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a hash using the <c>BLAKE2s</c> cryptographic hash algorithm, designed by Jean-Philippe Aumasson, Samuel
/// Neves, Zooko Wilcox-O'Hearn, and Christian Winnerlein. Supports output sizes of 128, 160, 192, 224, or 256 bits.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// BLAKE2s is specified in <see href="https://www.rfc-editor.org/rfc/rfc7693">RFC 7693</see> and is optimized for 8-bit
/// to 32-bit platforms. It operates on 64-byte (512-bit) blocks and maintains eight 32-bit state words, applying 10
/// rounds of the BLAKE2 <c>G</c> mixing function per block.
/// </para>
/// <para>
/// This implementation inherits its residual buffer, byte-counter and lookahead-buffering loop from
/// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}" />: the final message block is not compressed until
/// <see cref="HashAlgorithm.HashFinal" /> is called, at which point the <c>finalization</c> flag is set and the output
/// bytes are serialized in little-endian order then truncated to the configured output length.
/// </para>
/// <para>
/// Supplying a non-empty <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}.Key" /> switches the instance into the
/// keyed <c>BLAKE2s-MAC</c> mode defined in RFC 7693 Section 2.8. The key (1–32 bytes) is zero-padded to 64 bytes and
/// prepended as the first message block, and the key length is encoded into the parameter block so that keyed and
/// unkeyed digests of the same message are always distinct.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// Output size: configurable — 128, 160, 192, 224, or 256 bits.
/// </description>
/// </item>
/// <item>
/// <description>
/// Block size: 64 bytes (512 bits); 8 × 32-bit state words; 10 rounds.
/// </description>
/// </item>
/// <item>
/// <description>
/// Optional key: 1–32 bytes for BLAKE2s-MAC mode (RFC 7693 §2.8).
/// </description>
/// </item>
/// <item>
/// <description>
/// Specification: RFC 7693; optimized for 8/16/32-bit hosts.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose BLAKE2s.</strong> Pick BLAKE2s on 32-bit hosts, embedded targets, or any time the output is
/// at most 32 bytes — the 32-bit-word design beats <see cref="Blake2b" /> on those platforms. On 64-bit hosts and for
/// outputs longer than 32 bytes, <see cref="Blake2b" /> is faster. For very large parallel workloads
/// <see cref="Blake3" /> is faster still and supports tree hashing natively.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Unkeyed hash
/// using var blake2s = new Blake2s(256);
/// byte[] digest = blake2s.ComputeHash(message);
///
/// // Keyed MAC (BLAKE2s-MAC-256)
/// using var mac = new Blake2s(256) { Key = myKey };
/// byte[] tag = mac.ComputeHash(message);
///]]>
/// </code>
/// </example>
/// <seealso cref="Blake2b"/> <seealso cref="Blake3"/>
public sealed partial class Blake2s
    : KeyedDeferredFinalBlockHashAlgorithm<Blake2s>
{
    /// <summary>
    /// The set of output sizes, in bits, accepted by this algorithm.
    /// </summary>
    private static readonly int[] s_permittedHashSizes = [128, 160, 192, 224, 256];

    /// <summary>
    /// Maximum accepted key length for the keyed <c>BLAKE2s-MAC</c> mode is 256 bits (32 bytes).
    /// </summary>
    public const int MaxKeySize = 256;

    /// <summary>
    /// The block size, in bits, processed by each compression call (64 bytes).
    /// </summary>
    private const int BlockSizeValue = 512;

    /// <summary>
    /// Convenience constant for the byte length of <see cref="BlockSizeValue" />, used when slicing or allocating
    /// <see cref="byte" /> buffers from the bit-valued constant.
    /// </summary>
    private const int BlockSizeBytesValue = BlockSizeValue / 8;

    /// <summary>
    /// The SHA-256 initialization constants used as the BLAKE2s IV.
    /// </summary>
    private static readonly uint[] s_iv =
    [
        0x6A09E667U, 0xBB67AE85U,
        0x3C6EF372U, 0xA54FF53AU,
        0x510E527FU, 0x9B05688CU,
        0x1F83D9ABU, 0x5BE0CD19U,
    ];

    /// <summary>
    /// The eight 32-bit internal hash state words.
    /// </summary>
    private readonly uint[] _h = new uint[8];

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2s" /> class with a 256-bit output hash size.
    /// </summary>
    public Blake2s()
        : this(256)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2s" /> class with the specified output size.
    /// </summary>
    /// <param name="hashSize">The desired output size in bits. Must be one of 128, 160, 192, 224, or 256.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported output sizes.
    /// </exception>
    public Blake2s(int hashSize)
        : base(BlockSizeValue, MaxKeySize)
    {
        CryptographyThrowHelper.ThrowIfInvalidHashSize(hashSize, s_permittedHashSizes);

        HashSizeValue = hashSize;
        InitializeHashState();
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

    /// <inheritdoc />
    /// <remarks>
    /// The format is <c>"BLAKE2s-<i>n</i>"</c>, where <i>n</i> is the configured digest size in bits.
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return $"BLAKE2s-{HashSizeValue}";
        }
    }

    /// <summary>
    /// Gets or sets the size, in bits, of the final computed hash output.
    /// </summary>
    /// <value>The output size in bits; must be one of 128, 160, 192, 224, or 256.</value>
    /// <returns>The currently configured output size in bits.</returns>
    /// <remarks>
    /// The full BLAKE2s compression is always run using all 256 bits of internal state. Shorter output lengths are
    /// produced by truncating the serialized state after finalization. The property may only be changed before hashing
    /// has begun; once <see cref="HashAlgorithm.TransformBlock" /> or a <c>ComputeHash</c> overload has been called,
    /// the value is immutable until <see cref="HashAlgorithm.Initialize" /> is called.
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
            ThrowIfDisposed();
            return HashSizeValue;
        }

        set
        {
            ThrowIfDisposed();
            ThrowIfInvalidState();

            CryptographyThrowHelper.ThrowIfInvalidHashSize(value, s_permittedHashSizes);

            HashSizeValue = value;
        }
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="HashAlgorithm" /> and optionally releases the managed
    /// resources.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release
    /// only unmanaged resources.
    /// </param>
    /// <remarks>
    /// <para>
    /// Clears the chaining state, releases the framework <see cref="HashAlgorithm.HashValue" /> array, and zeros
    /// <see cref="HashAlgorithm.HashSizeValue" /> when <paramref name="disposing" /> is <see langword="true" />.
    /// </para>
    /// <para>
    /// Retained key material owned by <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}" /> is cleared by the base
    /// implementation when this method delegates to <c>base.Dispose(disposing)</c>. The inherited residual buffer is
    /// cleared further down the dispose chain.
    /// </para>
    /// </remarks>
    protected override void Dispose(bool disposing)
    {
        if (IsDisposed) return;

        if (disposing)
        {
            CryptographyHelper.Clear(_h);
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Compresses a single 64-byte block using the BLAKE2s <c>F</c> compression function. Invoked by
    /// <see cref="DeferredFinalBlockHashAlgorithm{T}" /> with <paramref name="isFinal" /> set to
    /// <see langword="true" /> for the last call (which inverts the finalization flag word) and to
    /// <see langword="false" /> otherwise.
    /// </summary>
    /// <param name="block">The 64-byte block to compress.</param>
    /// <param name="totalBytesIncludingThisBlock">
    /// The cumulative byte count <em>including</em> the bytes in <paramref name="block" />. Used as the per-block
    /// counter (the BLAKE2 <c>t0</c> / <c>t1</c> input pair).
    /// </param>
    /// <param name="isFinal">
    /// <see langword="true" /> if this is the final block; causes the finalization flag word to be inverted.
    /// </param>
    /// <remarks>
    /// Dispatches to an AVX-512 vectorised implementation when supported by the host, falling back to a scalar
    /// reference implementation otherwise. <see cref="Avx512F.VL.IsSupported" /> is a JIT intrinsic that folds to a
    /// compile-time constant, so the branch carries no runtime cost.
    /// </remarks>
    protected override void ProcessBlock(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal)
    {
        if (Avx512F.VL.IsSupported)
        {
            ProcessBlockAvx512(block, totalBytesIncludingThisBlock, isFinal);
            return;
        }

        ProcessBlockScalar(block, totalBytesIncludingThisBlock, isFinal);
    }

    /// <summary>
    /// Compresses a single 64-byte block using the scalar reference BLAKE2s implementation.
    /// </summary>
    /// <param name="block">The 64-byte block to compress.</param>
    /// <param name="totalBytesIncludingThisBlock">The cumulative byte count including this block.</param>
    /// <param name="isFinal"><see langword="true" /> if this is the final block.</param>
    /// <remarks>
    /// Invoked by <see cref="ProcessBlock" /> on hosts without AVX-512 + VL support. Implements the reference
    /// 16-element working-vector form of the BLAKE2s <c>F</c> compression function directly from RFC 7693.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void ProcessBlockScalar(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal)
    {
        // Read the 16 message words in little-endian order.
        Span<uint> m = stackalloc uint[16];
        for (int i = 0; i < 16; i++)
            m[i] = BinaryPrimitives.ReadUInt32LittleEndian(block.Slice(i * 4, 4));

        // Initialize the 16-element working vector.
        Span<uint> v =
        [
            _h[0],
            _h[1],
            _h[2],
            _h[3],
            _h[4],
            _h[5],
            _h[6],
            _h[7],
            s_iv[0],
            s_iv[1],
            s_iv[2],
            s_iv[3],
            s_iv[4] ^ (uint)(totalBytesIncludingThisBlock & 0xFFFFFFFFUL),   // counter low word
            s_iv[5] ^ (uint)(totalBytesIncludingThisBlock >> 32),            // counter high word
            s_iv[6],
            s_iv[7],
        ];
        if (isFinal)
            v[14] = ~v[14];

        // 10 rounds of G mixing.
        for (int r = 0; r < 10; r++)
        {
            byte[] s = Blake2Constants.Sigma[r % 10];

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
            _h[i] ^= v[i] ^ v[i + 8];
    }

    /// <inheritdoc />
    /// <remarks>
    /// Serializes the eight 32-bit state words in little-endian order and truncates the result to the configured output
    /// length (which need not be a multiple of four bytes).
    /// </remarks>
    protected override byte[] ProcessFinalBlock()
    {
        int outputBytes = HashSizeValue / 8;
        byte[] output = new byte[outputBytes];
        int wordCount = (outputBytes + 3) / 4;

        Span<byte> tmp = stackalloc byte[4];
        for (int i = 0; i < wordCount; i++)
        {
            Span<byte> wordSpan = output.AsSpan(i * 4, Math.Min(4, outputBytes - (i * 4)));
            if (wordSpan.Length == 4)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(wordSpan, _h[i]);
            }
            else
            {
                // Final word may be a partial write when the output size is not a multiple of 4 bytes.
                BinaryPrimitives.WriteUInt32LittleEndian(tmp, _h[i]);
                tmp[..wordSpan.Length].CopyTo(wordSpan);
            }
        }

        return output;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Copies the BLAKE2s IV into the eight internal hash-state words, then applies the parameter block XOR encoding
    /// the digest length and key length per RFC 7693: <c>h[0] ^= 0x01010000 ^ (kk &lt;&lt; 8) ^ nn</c>.
    /// </remarks>
    protected override void InitializeHashState()
    {
        s_iv.CopyTo(_h, 0);

        // Parameter block: fan-out=1, max depth=1, digest length=nn, key length=kk.
        int nn = HashSizeValue / 8;
        int kk = KeyValue?.Length ?? 0;
        _h[0] ^= 0x01010000U ^ ((uint)kk << 8) ^ (uint)nn;
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
        v[d] = (v[d] ^ v[a]).RotateBitsRightUnchecked(16);
        v[c] += v[d];
        v[b] = (v[b] ^ v[c]).RotateBitsRightUnchecked(12);
        v[a] += v[b] + y;
        v[d] = (v[d] ^ v[a]).RotateBitsRightUnchecked(8);
        v[c] += v[d];
        v[b] = (v[b] ^ v[c]).RotateBitsRightUnchecked(7);
    }
}
