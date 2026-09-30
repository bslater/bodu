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
/// <see cref="KeyedDeferredFinalBlockHashAlgorithm" />: the final message block is not compressed until
/// <see cref="HashAlgorithm.HashFinal" /> is called, at which point the <c>finalization</c> flag is set and the output
/// bytes are serialized in little-endian order then truncated to the configured output length.
/// </para>
/// <para>
/// Supplying a non-empty <see cref="KeyedDeferredFinalBlockHashAlgorithm.Key" /> switches the instance into the keyed
/// <c>BLAKE2s-MAC</c> mode defined in RFC 7693 Section 2.8. The key (1-32 bytes) is zero-padded to 64 bytes and
/// prepended as the first message block, and the key length is encoded into the parameter block so that keyed and
/// unkeyed digests of the same message are always distinct.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: configurable - 128, 160, 192, 224, or 256 bits.</description>
/// </item>
/// <item>
/// <description>Block size: 64 bytes (512 bits); 8 × 32-bit state words; 10 rounds.</description>
/// </item>
/// <item>
/// <description>Optional key: 1-32 bytes for BLAKE2s-MAC mode (RFC 7693 §2.8).</description>
/// </item>
/// <item>
/// <description>Specification: RFC 7693; optimized for 8/16/32-bit hosts.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose BLAKE2s.</strong> Pick BLAKE2s on 32-bit hosts, embedded targets, or any time the output is
/// at most 32 bytes - the 32-bit-word design beats <see cref="Blake2b" /> on those platforms. On 64-bit hosts and for
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
    : KeyedDeferredFinalBlockHashAlgorithm
{
    /// <summary>The set of output sizes, in bits, accepted by this algorithm.</summary>
    private static readonly int[] s_permittedHashSizes = [128, 160, 192, 224, 256];

    /// <summary>Maximum accepted key length for the keyed <c>BLAKE2s-MAC</c> mode is 256 bits (32 bytes).</summary>
    public const int MaxKeySize = 256;

    /// <summary>The block size, in bits, processed by each compression call (64 bytes).</summary>
    private const int BlockSizeValue = 512;

    /// <summary>Convenience constant for the byte length of <see cref="BlockSizeValue" />, used when slicing or allocating <see cref="byte" /> buffers from the bit-valued constant.</summary>
    private const int BlockSizeBytesValue = BlockSizeValue / 8;

    /// <summary>The eight 32-bit internal hash state words.</summary>
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

            // The digest length is encoded into the parameter block during state initialization, which the base
            // performs at construction and after each completed hash - but not on a HashSize change. Rebuild the
            // state now so the next hash uses the new digest length; otherwise it would run with the stale parameter
            // block baked at construction and produce a wrong digest.
            Initialize();
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
    /// Retained key material owned by <see cref="KeyedDeferredFinalBlockHashAlgorithm" /> is cleared by the base
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
    /// <see cref="DeferredFinalBlockHashAlgorithm" /> with <paramref name="isFinal" /> set to <see langword="true" />
    /// for the last call (which inverts the finalization flag word) and to <see langword="false" /> otherwise.
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
    /// Compression runs on the kernel <see cref="Blake2sCore" /> selects: AVX-512, then SSSE3 on x64, and the scalar
    /// kernel on ARM64, where it ran faster than the AdvSimd kernel, and everywhere else; each gate honors the
    /// process-wide SIMD opt-out.
    /// </remarks>
    protected override void ProcessBlock(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal) =>
        Blake2sCore.Compress(_h, block, totalBytesIncludingThisBlock, isFinal);

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
        Blake2sCore.InitializationVector.CopyTo(_h);

        // Parameter block: fan-out=1, max depth=1, digest length=nn, key length=kk.
        int nn = HashSizeValue / 8;
        int kk = KeyValue?.Length ?? 0;
        _h[0] ^= 0x01010000U ^ ((uint)kk << 8) ^ (uint)nn;
    }
}
