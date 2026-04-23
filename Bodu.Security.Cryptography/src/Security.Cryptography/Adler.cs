// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a generic base class for Adler-style checksum algorithms parameterised by the accumulator type.
/// </summary>
/// <typeparam name="T">The unsigned numeric type used for internal state accumulation (e.g., <see cref="uint" /> or <see cref="ulong" />).</typeparam>
/// <remarks>
/// <para>
/// Adler checksums maintain two accumulators (A and B) and combine them to form the final checksum. Derived classes supply the modulus
/// appropriate to the desired bit width (e.g., 65521 for Adler-32, or 4294967291 for Adler-64). The core hashing loop provides both a
/// SIMD-accelerated path and a scalar fallback.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
/// digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public abstract class Adler<T>
    : System.Security.Cryptography.HashAlgorithm
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

    private readonly T modulo;
    private bool disposed = false;

    /// <summary>
    /// Initialises a new instance of the <see cref="Adler{T}" /> class with the specified modulus.
    /// </summary>
    /// <param name="modulo">The modulus applied to the accumulators after each reduction step.</param>
    /// <exception cref="NotSupportedException">
    /// <typeparamref name="T" /> is not one of the supported accumulator types (<see cref="uint" /> or <see cref="ulong" />).
    /// </exception>
    protected Adler(T modulo)
    {
        this.modulo = modulo;
        this.HashSizeValue = typeof(T) switch
        {
            var t when t == typeof(uint) => 32,
            var t when t == typeof(ulong) => 64,
            _ => throw new NotSupportedException($"Unsupported hash length for type {typeof(T)}.")
        };
        this.PartA = T.One;
        this.PartB = T.Zero;
    }

    /// <inheritdoc />
    public override bool CanReuseTransform => true;

    /// <inheritdoc />
    public override bool CanTransformMultipleBlocks => true;

#if !NET6_0_OR_GREATER

    // Required for .NET Standard 2.0 or older frameworks
    private bool finalized;
#endif

    /// <inheritdoc />
    public override void Initialize()
    {
#if !NET6_0_OR_GREATER
        State = 0;
        finalized = false;
#endif
        this.ThrowIfDisposed();
        this.PartA = T.One;
        this.PartB = T.Zero;
    }

    /// <summary>
    /// Releases resources used by the algorithm and clears internal accumulators.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (this.disposed) return;

        if (disposing)
        {
            CryptoHelpers.ClearAndNullify(ref this.HashValue);

            this.PartA = this.PartB = T.Zero;
            this.HashSizeValue = 0;
        }

        this.disposed = true;
        base.Dispose(disposing);
    }

    /// <summary>
    /// Feeds <paramref name="cbSize" /> bytes from <paramref name="array" /> (starting at <paramref name="ibStart" />) into the
    /// checksum computation.
    /// </summary>
    /// <param name="array">The input byte array containing the data to hash.</param>
    /// <param name="ibStart">The zero-based index in <paramref name="array" /> at which to begin reading data.</param>
    /// <param name="cbSize">The number of bytes to process from <paramref name="array" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <para><paramref name="ibStart" /> is less than 0.</para>
    /// <para>-or-</para>
    /// <para><paramref name="cbSize" /> is less than 0.</para>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ibStart" /> and <paramref name="cbSize" /> specify a range that exceeds the length of <paramref name="array" />.
    /// </exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
#if !NET6_0_OR_GREATER
        ThrowHelper.ThrowIfLessThan(ibStart, 0);
        ThrowHelper.ThrowIfLessThan(cbSize, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, ibStart, cbSize);
        if (finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

        this.HashCore(array.AsSpan(ibStart, cbSize));
    }

    /// <summary>
    /// Feeds the contents of <paramref name="source" /> into the checksum computation.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();

        const int NMAX = 5552;
        int length = source.Length;
        int index = 0;
        T pA = this.PartA, pB = this.PartB;

        // SIMD path (512+ bytes and hardware available)
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
                        sum += T.CreateTruncating(lo[i]) + T.CreateTruncating(hi[i]); ;

                    pA += sum;
                    pB += pA;

                    index += Vector<byte>.Count;
                }

                // Move index forward to avoid infinite loop, handling remaining non-vectorized bytes later
                index = chunkEnd;

                pA %= this.modulo;
                pB %= this.modulo;
            }
        }

        // Scalar fallback for remainder or short inputs
        while (index < length)
        {
            pA += T.CreateTruncating(source[index++]);
            pB += pA;

            if ((index % NMAX) == 0)
            {
                pA %= this.modulo;
                pB %= this.modulo;
            }
        }

        this.PartA = pA;
        this.PartB = pB;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when any public method or property is accessed after the instance has been disposed.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
#if NET8_0_OR_GREATER
        ObjectDisposedException.ThrowIf(this.disposed, this);
#else
        if (disposed)
            throw new ObjectDisposedException(this.GetType().Name);
#endif
    }

    /// <summary>
    /// Throws if the algorithm has already begun processing data, preventing reconfiguration of parameters after hashing has started.
    /// </summary>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// Thrown when an attempt is made to modify the algorithm after it has entered a non-zero state, which indicates that hashing has
    /// started or been finalized.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfInvalidState()
    {
        if (this.State != 0)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
    }
}
