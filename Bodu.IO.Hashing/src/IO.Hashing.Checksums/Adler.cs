// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Numerics;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides a generic base class for Adler-style checksum algorithms parameterised by the accumulator type.
/// </summary>
/// <typeparam name="T">
/// The unsigned numeric type used for internal state accumulation, either <see cref="uint" /> or
/// <see cref="ulong" />.
/// </typeparam>
/// <remarks>
/// <para>
/// Adler checksums maintain two accumulators (A and B) and combine them to form the final checksum. Derived
/// classes supply the modulus appropriate to the desired bit width (for example 65521 for Adler-32, or
/// 4294967291 for Adler-64). The core hashing loop provides both a SIMD-accelerated path and a scalar
/// fallback; the SIMD path applies the canonical positionally weighted block recurrence so that both paths
/// produce identical digests for any input.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used
/// for password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public abstract class Adler<T>
    : NonCryptographicHashAlgorithm
    where T : unmanaged, INumber<T>
{
    /// <summary>
    /// The A accumulator, initialised to one and updated with each input byte.
    /// </summary>
    protected T PartA;

    /// <summary>
    /// The B accumulator, which holds the running sum of <see cref="PartA" /> across all processed bytes.
    /// </summary>
    protected T PartB;

    private readonly T _modulo;

    /// <summary>
    /// Initializes a new instance of the <see cref="Adler{T}" /> class with the specified hash length and
    /// modulus.
    /// </summary>
    /// <param name="hashLengthInBytes">The digest length, in bytes, produced by the derived algorithm.</param>
    /// <param name="modulo">The modulus applied to the accumulators after each reduction step.</param>
    protected Adler(int hashLengthInBytes, T modulo)
        : base(hashLengthInBytes)
    {
        this._modulo = modulo;
        this.PartA = T.One;
        this.PartB = T.Zero;
    }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        const int NMAX = 5552;
        int length = source.Length;
        int index = 0;
        T pA = this.PartA;
        T pB = this.PartB;

        if (Vector.IsHardwareAccelerated && length >= 512)
        {
            int width = Vector<byte>.Count;
            int half = Vector<ushort>.Count;
            T widthT = T.CreateTruncating((uint)width);

            while (index < length)
            {
                int remaining = Math.Min(length - index, NMAX);
                int chunkEnd = index + remaining;

                // Per width-byte block [b_1 .. b_V] starting from accumulators (A, B), the
                // canonical Adler recurrence yields:
                //     A_new = A + Σ b_i
                //     B_new = B + V · A + Σ (V - i + 1) · b_i
                // The positional weighting and the V·A carry term are essential — omitting
                // either produces a digest that does not match the per-byte definition.
                while (index + width <= chunkEnd)
                {
                    Vector<byte> vec = new Vector<byte>(source.Slice(index, width));
                    Vector.Widen(vec, out Vector<ushort> lo, out Vector<ushort> hi);

                    T sumBytes = T.Zero;
                    T sumWeighted = T.Zero;
                    for (int i = 0; i < half; i++)
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

                pA %= this._modulo;
                pB %= this._modulo;
            }
        }

        while (index < length)
        {
            pA += T.CreateTruncating(source[index++]);
            pB += pA;

            if ((index % NMAX) == 0)
            {
                pA %= this._modulo;
                pB %= this._modulo;
            }
        }

        // Reduce on every Append boundary so the stored state is always canonical (Part* < _modulo).
        // The SIMD branch already reduces per chunk; the scalar fallback only reduces at NMAX hits,
        // so without this a sub-NMAX Append (e.g. per-byte) would leave PartA/PartB unreduced and
        // GetCurrentHashCore would emit a non-canonical digest.
        this.PartA = pA % this._modulo;
        this.PartB = pB % this._modulo;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this.PartA = T.One;
        this.PartB = T.Zero;
    }
}
