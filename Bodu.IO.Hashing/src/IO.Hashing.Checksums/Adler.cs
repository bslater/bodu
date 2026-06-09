// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.cs" company="Bodu Pty. Ltd.">
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
    /// <summary>
    /// The A accumulator, initialized to one and updated with each input byte.
    /// </summary>
    protected T PartA;

    /// <summary>
    /// The B accumulator, which holds the running sum of <see cref="PartA" /> across all processed bytes.
    /// </summary>
    protected T PartB;

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
        PartA = T.One;
        PartB = T.Zero;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        const int NMAX = 5552;
        var length = source.Length;
        var index = 0;
        T pA = PartA;
        T pB = PartB;

        if (Vector.IsHardwareAccelerated && length >= 512)
        {
            var width = Vector<byte>.Count;
            var half = Vector<ushort>.Count;
            T widthT = T.CreateTruncating((uint)width);

            while (index < length)
            {
                var remaining = Math.Min(length - index, NMAX);
                var chunkEnd = index + remaining;

                // Per width-byte block [b_1 .. b_V] starting from accumulators (A, B), the
                // canonical Adler recurrence yields:
                //     A_new = A + Σ b_i
                //     B_new = B + V · A + Σ (V - i + 1) · b_i
                // The positional weighting and the V·A carry term are essential — omitting
                // either produces a digest that does not match the per-byte definition.
                while (index + width <= chunkEnd)
                {
                    var vec = new Vector<byte>(source.Slice(index, width));
                    Vector.Widen(vec, out Vector<ushort> lo, out Vector<ushort> hi);

                    T sumBytes = T.Zero;
                    T sumWeighted = T.Zero;
                    for (var i = 0; i < half; i++)
                    {
                        T loByte = T.CreateTruncating(lo[i]);
                        T hiByte = T.CreateTruncating(hi[i]);
                        sumBytes += loByte + hiByte;
                        sumWeighted += (T.CreateTruncating((uint)(width - i)) * loByte)
                                     + (T.CreateTruncating((uint)(half - i)) * hiByte);
                    }

                    pB += (pA * widthT) + sumWeighted;
                    pA += sumBytes;

                    index += width;
                }

                while (index < chunkEnd)
                {
                    pA += T.CreateTruncating(source[index++]);
                    pB += pA;
                }

                pA %= _modulo;
                pB %= _modulo;
            }
        }

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
        PartA = pA % _modulo;
        PartB = pB % _modulo;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        PartA = T.One;
        PartB = T.Zero;
    }
}
