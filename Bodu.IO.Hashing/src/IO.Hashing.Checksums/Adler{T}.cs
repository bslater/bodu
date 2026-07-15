// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Numerics;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a generic base class for Adler-style checksum algorithms parameterized by the accumulator type.
/// </summary>
/// <typeparam name="T">
/// The unsigned numeric type used for internal state accumulation, either <see cref="uint" /> or <see cref="ulong" />.
/// </typeparam>
/// <remarks>
/// <para>
/// Adler checksums maintain two accumulators (A and B) and combine them to form the final checksum. Derived classes
/// supply the modulus appropriate to the desired bit width (for example 65521 for Adler-32, or 4294967291 for
/// Adler-64). The core hashing loop provides both a SIMD-accelerated path and a scalar fallback; the SIMD path applies
/// the canonical positionally weighted block recurrence so that both paths produce identical digests for any input.
/// </para>
/// <para>
/// <strong>When to choose Adler.</strong> Adler-32 is the canonical checksum used by zlib (RFC 1950) — pick
/// <see cref="Adler32" /> any time interoperability with zlib, deflate, or PNG's chunk integrity is required. It is
/// faster than CRC at the cost of weaker error-detection guarantees, and is therefore preferred where throughput
/// matters more than rigorous coverage of burst errors. <see cref="Adler64" /> generalizes the construction to 64 bits
/// for very large inputs where the Adler-32 collision floor becomes a concern; <see cref="Adler32C" /> swaps the prime
/// modulus 65521 for the power-of-two 65536 to enable cheaper modular reductions in vectorized paths — its outputs are
/// <em>not</em> interchangeable with standard Adler-32. For stronger error detection prefer <see cref="Crc" />; for
/// hash-table keying prefer <see cref="Bodu.IO.Hashing.MurmurHash3{T}" /> or <see cref="Bodu.IO.Hashing.CityHash{T}" />
/// .
/// </para>
/// <para>
/// <strong>Lifecycle and threading.</strong> Inherits the standard
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" /> /
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Reset" /> /
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> shape; snapshotting is
/// non-destructive. Instances are not thread-safe; share behind explicit synchronization.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.IO.Hashing.Checksums;
/// using Bodu.IO.Hashing.Extensions;
///
/// // zlib-compatible Adler-32 of a deflate payload.
/// var adler = new Adler32();
/// byte[] checksum = adler.ComputeHash(deflateBlock);
///]]>
/// </code>
/// </example>
/// </remarks>
/// <seealso cref="Adler32"/> <seealso cref="Adler32C"/> <seealso cref="Adler64"/> <seealso cref="Crc"/>
public abstract class Adler<T>
    : NonCryptographicHashAlgorithm
    where T : unmanaged, INumber<T>
{
    /// <summary>The A accumulator, initialized to one and updated with each input byte.</summary>
    protected T partA;

    /// <summary>The B accumulator, which holds the running sum of <see cref="partA" /> across all processed bytes.</summary>
    protected T partB;

    /// <summary>The modulus applied to both accumulators after each reduction step.</summary>
    private readonly T _modulo;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler{T}" /> class with the specified hash length and modulus.
    /// </summary>
    /// <param name="hashLengthInBytes">The digest length, in bytes, produced by the derived algorithm.</param>
    /// <param name="modulo">The modulus applied to the accumulators after each reduction step.</param>
    protected Adler(int hashLengthInBytes, T modulo)
        : base(hashLengthInBytes)
    {
        _modulo = modulo;
        partA = T.One;
        partB = T.Zero;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        const int NMAX = 5552;
        int length = source.Length;
        int index = 0;
        T pA = partA;
        T pB = partB;

        // Canonical zlib-style deferred reduction: NMAX (5552) is the largest run of bytes for which the running
        // B accumulator provably stays within a 32-bit value, so both accumulators are reduced once per NMAX bytes.
        // A previous SIMD path here only walked each widened Vector<T> lane with per-element indexing and generic
        // math — no actual vector arithmetic — which is typically slower than this scalar loop, so it was removed.
        while (index < length)
        {
            pA += T.CreateTruncating(source[index++]);
            pB += pA;

            if ((index % NMAX) == 0)
            {
                pA %= _modulo;
                pB %= _modulo;
            }
        }

        // Reduce on every Append boundary so the stored state is always canonical (Part* < _modulo).
        // The SIMD branch already reduces per chunk; the scalar fallback only reduces at NMAX hits,
        // so without this a sub-NMAX Append (e.g. per-byte) would leave PartA/PartB unreduced and
        // GetCurrentHashCore would emit a non-canonical digest.
        partA = pA % _modulo;
        partB = pB % _modulo;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        partA = T.One;
        partB = T.Zero;
    }
}
