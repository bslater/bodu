// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2b.cs" company="Bodu Pty. Ltd.">
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
/// Computes a hash using the <c>BLAKE2b</c> cryptographic hash algorithm, designed by Jean-Philippe Aumasson, Samuel
/// Neves, Zooko Wilcox-O'Hearn, and Christian Winnerlein. Supports output sizes of 128, 160, 192, 224, 256, 384, or 512
/// bits. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// BLAKE2b is specified in <see href="https://www.rfc-editor.org/rfc/rfc7693">RFC 7693</see> and is optimized for
/// 64-bit platforms. It operates on 128-byte (1024-bit) blocks and maintains eight 64-bit state words, applying 12
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
/// <c>BLAKE2b-MAC</c> mode defined in RFC 7693 Section 2.8. The key (1–64 bytes) is zero-padded to 128 bytes and
/// prepended as the first message block, and the key length is encoded into the parameter block so that keyed and
/// unkeyed digests of the same message are always distinct.
/// </para>
/// <para>
/// <strong>Parameters at a glance.</strong>
/// </para>
/// <list type="bullet">
/// <item>
/// <description>Output size: configurable — 128, 160, 192, 224, 256, 384, or 512 bits.</description>
/// </item>
/// <item>
/// <description>Block size: 128 bytes (1024 bits); 8 × 64-bit state words; 12 rounds.</description>
/// </item>
/// <item>
/// <description>Optional key: 1–64 bytes for BLAKE2b-MAC mode (RFC 7693 §2.8).</description>
/// </item>
/// <item>
/// <description>Specification: RFC 7693; optimized for 64-bit hosts.</description>
/// </item>
/// </list>
/// <para>
/// <strong>When to choose BLAKE2b.</strong> The right pick on 64-bit platforms when SHA-2 / SHA-3 throughput matters
/// but compatibility with those standards is not required — BLAKE2b is faster than SHA-512 in software while offering
/// the same security level. Use <see cref="Blake2s" /> on 32-bit hosts or for output sizes up to 32 bytes. For
/// tree-hashing or genuinely large parallel workloads <see cref="Blake3" /> is faster again. As a keyed MAC,
/// BLAKE2b-MAC is competitive with HMAC-SHA-256 / Poly1305 and avoids the double-hash overhead of HMAC.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Unkeyed hash
/// using var blake2b = new Blake2b(512);
/// byte[] digest = blake2b.ComputeHash(message);
///
/// // Keyed MAC (BLAKE2b-MAC-512)
/// using var mac = new Blake2b(512) { Key = myKey };
/// byte[] tag = mac.ComputeHash(message);
///]]>
/// </code>
/// </example>
/// <seealso cref="Blake2s"/> <seealso cref="Blake3"/>
public sealed partial class Blake2b
    : KeyedDeferredFinalBlockHashAlgorithm
{
    /// <summary>The set of output sizes, in bits, accepted by this algorithm.</summary>
    private static readonly int[] s_permittedHashSizes = [128, 160, 192, 224, 256, 384, 512];

    /// <summary>Maximum accepted key length for the keyed <c>BLAKE2b-MAC</c> mode is 512 bits (64 bytes).</summary>
    public const int MaxKeySize = 512;

    /// <summary>The block size, in bits, processed by each compression call (128 bytes).</summary>
    private const int BlockSizeValue = 1024;

    /// <summary>Convenience constant for the byte length of <see cref="BlockSizeValue" />, used when slicing or allocating <see cref="byte" /> buffers from the bit-valued constant.</summary>
    private const int BlockSizeBytesValue = BlockSizeValue / 8;

    /// <summary>The eight 64-bit internal hash state words.</summary>
    private readonly ulong[] _h = new ulong[8];

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2b" /> class with a 512-bit output hash size.
    /// </summary>
    public Blake2b()
        : this(512)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Blake2b" /> class with the specified output size.
    /// </summary>
    /// <param name="hashSize">
    /// The desired output size in bits. Must be one of 128, 160, 192, 224, 256, 384, or 512.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported output sizes.
    /// </exception>
    public Blake2b(int hashSize)
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
    /// The format is <c>"BLAKE2b-<i>n</i>"</c>, where <i>n</i> is the configured digest size in bits.
    /// </remarks>
    public override string AlgorithmName
    {
        get
        {
            ThrowIfDisposed();
            return $"BLAKE2b-{HashSizeValue}";
        }
    }

    /// <summary>
    /// Gets or sets the size, in bits, of the final computed hash output.
    /// </summary>
    /// <value>The output size in bits; must be one of 128, 160, 192, 224, 256, 384, or 512.</value>
    /// <remarks>
    /// The full BLAKE2b compression is always run using all 512 bits of internal state. Shorter output lengths are
    /// produced by truncating the serialized state after finalization. The property may only be changed before hashing
    /// has begun; once <see cref="HashAlgorithm.TransformBlock" /> or a <c>ComputeHash</c> overload has been called,
    /// the value is immutable until <see cref="HashAlgorithm.Initialize" /> is called.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The assigned value is not one of 128, 160, 192, 224, 256, 384, or 512.
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
            // performs at construction and after each completed hash — but not on a HashSize change. Rebuild the
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
    /// Compresses a single 128-byte block using the BLAKE2b <c>F</c> compression function. Invoked by
    /// <see cref="DeferredFinalBlockHashAlgorithm" /> with <paramref name="isFinal" /> set to <see langword="true" />
    /// for the last call (which inverts the finalization flag word) and to <see langword="false" /> otherwise.
    /// </summary>
    /// <param name="block">The 128-byte block to compress.</param>
    /// <param name="totalBytesIncludingThisBlock">
    /// The cumulative byte count <em>including</em> the bytes in <paramref name="block" />. Used as the per-block
    /// counter (the BLAKE2 <c>t0</c> input).
    /// </param>
    /// <param name="isFinal">
    /// <see langword="true" /> if this is the final block; causes the finalization flag word to be inverted.
    /// </param>
    /// <remarks>
    /// Compression runs on the kernel <see cref="Blake2bCore" /> selects: AVX-512, then AVX2, then SSSE3 on x64, and
    /// the scalar kernel on ARM64, where it ran faster than the AdvSimd kernel, and everywhere else; each gate honors
    /// the process-wide SIMD opt-out.
    /// </remarks>
    protected override void ProcessBlock(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal) =>
        Blake2bCore.Compress(_h, block, totalBytesIncludingThisBlock, isFinal);

    /// <inheritdoc />
    /// <remarks>
    /// Serializes the eight 64-bit state words in little-endian order and truncates the result to the configured output
    /// length (which need not be a multiple of eight bytes).
    /// </remarks>
    protected override byte[] ProcessFinalBlock()
    {
        int outputBytes = HashSizeValue / 8;
        byte[] output = new byte[outputBytes];
        int wordCount = (outputBytes + 7) / 8;

        Span<byte> tmp = stackalloc byte[8];
        for (int i = 0; i < wordCount; i++)
        {
            Span<byte> wordSpan = output.AsSpan(i * 8, Math.Min(8, outputBytes - (i * 8)));
            if (wordSpan.Length == 8)
            {
                BinaryPrimitives.WriteUInt64LittleEndian(wordSpan, _h[i]);
            }
            else
            {
                // Final word may be a partial write when the output size is not a multiple of 8 bytes.
                BinaryPrimitives.WriteUInt64LittleEndian(tmp, _h[i]);
                tmp[..wordSpan.Length].CopyTo(wordSpan);
            }
        }

        return output;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Copies the BLAKE2b IV into the eight internal hash-state words, then applies the parameter block XOR encoding
    /// the digest length and key length per RFC 7693: <c>h[0] ^= 0x01010000 ^ (kk &lt;&lt; 8) ^ nn</c>.
    /// </remarks>
    protected override void InitializeHashState()
    {
        Blake2bCore.InitializationVector.CopyTo(_h);

        // Parameter block: fan-out=1, max depth=1, digest length=nn, key length=kk.
        int nn = HashSizeValue / 8;
        int kk = KeyValue?.Length ?? 0;
        _h[0] ^= 0x01010000UL ^ ((ulong)kk << 8) ^ (ulong)nn;
    }
}
