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
/// fallback.
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
            while (index < length)
            {
                int remaining = Math.Min(length - index, NMAX);
                int chunkEnd = index + remaining;

                while (index + Vector<byte>.Count <= chunkEnd)
                {
                    var vec = new Vector<byte>(source.Slice(index, Vector<byte>.Count));
                    Vector.Widen(vec, out Vector<ushort> lo, out Vector<ushort> hi);

                    T sum = T.Zero;
                    for (int i = 0; i < Vector<ushort>.Count; i++)
                        sum += T.CreateTruncating(lo[i]) + T.CreateTruncating(hi[i]);

                    pA += sum;
                    pB += pA;

                    index += Vector<byte>.Count;
                }

                index = chunkEnd;

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

        this.PartA = pA;
        this.PartB = pB;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        this.PartA = T.One;
        this.PartB = T.Zero;
    }
}
