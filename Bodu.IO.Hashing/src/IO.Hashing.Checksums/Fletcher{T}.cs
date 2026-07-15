// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a base class for the Fletcher checksum family (Fletcher-16, Fletcher-32, Fletcher-64).
/// </summary>
/// <typeparam name="TSelf">The concrete derived type (CRTP) used for block-hash reuse.</typeparam>
/// <remarks>
/// <para>
/// Fletcher is a non-cryptographic position-dependent checksum that maintains two running accumulators (A and B) and
/// combines them into the final hash. Derived types <see cref="Fletcher16" />, <see cref="Fletcher32" />, and
/// <see cref="Fletcher64" /> select the output width.
/// </para>
/// <para>
/// <strong>When to choose Fletcher.</strong> Fletcher was designed as a cheaper alternative to CRC for detecting
/// accidental corruption in network protocols and file formats — TCP, Modbus ASCII, and ZFS all use a Fletcher variant.
/// It catches single-bit errors and many burst errors at a fraction of CRC's per-byte cost, making it attractive on
/// resource-constrained microcontrollers and in tight inner loops. Pick <see cref="Fletcher16" /> for embedded
/// protocols where 16 bits is enough, <see cref="Fletcher32" /> as the workhorse for general file-integrity work, and
/// <see cref="Fletcher64" /> when a wider checksum reduces collision pressure on large datasets. For stronger
/// error-detection guarantees prefer <see cref="Crc" />; for hash-table keying prefer
/// <see cref="Bodu.IO.Hashing.MurmurHash3{T}" /> or <see cref="Bodu.IO.Hashing.CityHash{T}" />, which give better
/// avalanche than any positional-sum scheme.
/// </para>
/// <para>
/// <strong>Lifecycle and threading.</strong> Inherits the standard
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" /> /
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset" /> /
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> shape via
/// <see cref="BlockNonCryptographicHashAlgorithm{T}" />. Snapshotting is non-destructive — call <c>GetCurrentHash</c>
/// as often as needed. Instances are not thread-safe; share behind explicit synchronization, or allocate one per
/// consumer.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// // 32-bit Fletcher checksum of a packet payload.
/// var fletcher = new Fletcher32();
/// byte[] checksum = fletcher.ComputeHash(payload);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Fletcher16"/> <seealso cref="Fletcher32"/> <seealso cref="Fletcher64"/> <seealso cref="Crc"/>
public abstract class Fletcher<TSelf>
    : BlockNonCryptographicHashAlgorithm<TSelf>
    where TSelf : Fletcher<TSelf>, new()
{
    /// <summary>The set of output widths, in bits, that the Fletcher family supports (16, 32, and 64).</summary>
    private static readonly int[] s_validHashSizes = [16, 32, 64];

    /// <summary>
    /// The number of input bytes accumulated in <see cref="Append" /> before the two accumulators are reduced modulo
    /// <see cref="_modulus" />. Chosen well below the point at which the running <c>B</c> accumulator could overflow a
    /// 64-bit value for the widest variant (Fletcher-64, modulus 2^32−1: <c>B</c> grows by at most <c>N·2^32</c>, so any
    /// <c>N</c> below ~2^27 is safe), while amortizing the two modulo operations over a whole cache-friendly run
    /// instead of paying them per byte.
    /// </summary>
    private const int ReductionBatch = 4096;

    /// <summary>The configured output width, in bits, of this instance.</summary>
    private readonly int _hashSizeBits;

    /// <summary>The modulus applied to each accumulator, equal to <c>2^(hashSize/2) - 1</c> for the configured width.</summary>
    private readonly ulong _modulus;

    /// <summary>The A accumulator holding the running sum of input bytes, reduced modulo <see cref="_modulus" />.</summary>
    private ulong _partA;

    /// <summary>The B accumulator holding the running sum of <see cref="_partA" />, reduced modulo <see cref="_modulus" />.</summary>
    private ulong _partB;

    /// <summary>
    /// Initializes a new instance of the <see cref="Fletcher{TSelf}" /> class with the specified hash size.
    /// </summary>
    /// <param name="hashSize">The hash size in bits. Valid values are 16, 32, or 64.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="hashSize" /> is not 16, 32, or 64.</exception>
    protected Fletcher(int hashSize)
        : base(
            hashLengthInBytes: s_validHashSizes.Contains(hashSize)
                ? hashSize / 8
                : throw new ArgumentException(
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        HashingResourceStrings.Arg_OutOfRange_HashSize,
                        hashSize,
                        string.Join(", ", s_validHashSizes)),
                    nameof(hashSize)),
            blockSize: 1)
    {
        _hashSizeBits = hashSize;
        _modulus = (1UL << (hashSize / 2)) - 1;
        AlgorithmName = $"Fletcher-{hashSize}";
    }

    /// <summary>
    /// Gets the algorithm name in the form <c>Fletcher-N</c>, where <c>N</c> is the output width in bits.
    /// </summary>
    /// <value>A string such as <c>Fletcher-16</c>, <c>Fletcher-32</c>, or <c>Fletcher-64</c>.</value>
    public string AlgorithmName { get; }

    /// <inheritdoc />
    protected override TSelf Clone()
    {
        var clone = new TSelf
        {
            _partA = _partA,
            _partB = _partB,
        };
        clone.CopyResidualStateFrom(this);
        return clone;
    }

    /// <inheritdoc />
    protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
    {
        byte[] buffer = new byte[BlockSizeBytes];
        block.CopyTo(buffer);
        return buffer;
    }

    /// <summary>
    /// Consumes input with deferred modular reduction, amortizing the two modulo operations over a whole
    /// <see cref="ReductionBatch" />-byte run instead of paying them per byte.
    /// </summary>
    /// <param name="source">The input bytes to fold into the running accumulators.</param>
    /// <remarks>
    /// <para>
    /// Both accumulators enter reduced (below <see cref="_modulus" />), and the batch length is bounded so the running
    /// <c>B</c> accumulator cannot overflow a 64-bit value. Reducing per batch is congruent to — and therefore produces
    /// the identical result as — the per-byte <c>A = (A + b) mod m; B = (B + A) mod m</c> recurrence, regardless of how
    /// the input is split across calls.
    /// </para>
    /// <para>
    /// This overrides the base per-block driver directly: with a one-byte block size the residual buffer is never
    /// populated, so the base <see cref="BlockNonCryptographicHashAlgorithm{TSelf}.ProcessBlock" /> path is used only
    /// by the padding branch that <see cref="ShouldPadFinalBlock" /> disables for production Fletcher variants.
    /// </para>
    /// </remarks>
    public override void Append(ReadOnlySpan<byte> source)
    {
        ulong a = _partA;
        ulong b = _partB;

        int pos = 0;
        while (pos < source.Length)
        {
            int end = Math.Min(pos + ReductionBatch, source.Length);
            for (; pos < end; pos++)
            {
                a += source[pos];
                b += a;
            }

            a %= _modulus;
            b %= _modulus;
        }

        _partA = a;
        _partB = b;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void ProcessBlock(ReadOnlySpan<byte> block)
    {
        _partA = (_partA + block[0]) % _modulus;
        _partB = (_partB + _partA) % _modulus;
    }

    /// <inheritdoc />
    protected override byte[] ProcessFinalBlock()
    {
        byte[] result = new byte[_hashSizeBits / 8];
        int halfLength = result.Length / 2;

        WriteBigEndian(_partB, result.AsSpan(0, halfLength));
        WriteBigEndian(_partA, result.AsSpan(halfLength, halfLength));

        return result;
    }

    /// <inheritdoc />
    protected override void ResetState()
    {
        _partA = 0;
        _partB = 0;
    }

    /// <inheritdoc />
    protected override bool ShouldPadFinalBlock() => false;

    /// <summary>
    /// Writes the least-significant bytes of <paramref name="value" /> into <paramref name="destination" /> in
    /// big-endian order, emitting exactly as many bytes as the span can hold.
    /// </summary>
    /// <param name="value">The value whose low-order bytes are written.</param>
    /// <param name="destination">The span that receives the big-endian bytes.</param>
    private static void WriteBigEndian(ulong value, Span<byte> destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            destination[i] = (byte)(value >> ((destination.Length - i - 1) << 3));
        }
    }
}
