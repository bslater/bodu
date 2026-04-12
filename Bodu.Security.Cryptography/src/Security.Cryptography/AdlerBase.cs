// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides a generic base class for <c>Adler</c> style checksum algorithms.
    /// </summary>
    /// <typeparam name="T">The unsigned numeric type used for internal state accumulation (e.g., <see cref="uint" />, <see cref="ulong" />).</typeparam>
    /// <remarks>
    /// <para>
    /// Adler checksum algorithms maintain two internal accumulators (A and B) and combine them to form a final checksum value. This generic
    /// base class implements the core checksum logic in a type-safe and reusable form, enabling 32-bit and 64-bit variants.
    /// </para>
    /// <para>Implementations must supply a modulus appropriate to the desired bit width (e.g., 65521 for Adler-32, or 4294967291 for Adler-64).</para>
    /// <para>
    /// This class is designed for reuse, supports incremental block processing, and provides SIMD-accelerated and scalar fallback paths.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public abstract class AdlerBase<T>
        : System.Security.Cryptography.HashAlgorithm
        where T : unmanaged, INumber<T>
    {
        /// <summary>
        /// The first part of the Adler checksum accumulator, typically initialized to 1.
        /// </summary>
        protected T PartA;

        /// <summary>
        /// The second part of the Adler checksum accumulator, which accumulates the cumulative sum of <see cref="PartA" />.
        /// </summary>
        protected T PartB;

        private readonly T modulo;
        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdlerBase{T}" /> class.
        /// </summary>
        protected AdlerBase(T modulo)
        {
            this.modulo = modulo;
            HashSizeValue = typeof(T) switch
            {
                var t when t == typeof(uint) => 32,
                var t when t == typeof(ulong) => 64,
                _ => throw new NotSupportedException($"Unsupported hash length for type {typeof(T)}.")
            };
            PartA = T.One;
            PartB = T.Zero;
        }

        /// <summary>
        /// Gets a value indicating whether this transform instance can be reused after a hash operation is completed.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the transform supports multiple hash computations via <see cref="HashAlgorithm.Initialize" />;
        /// otherwise, <see langword="false" />.
        /// </value>
        /// <remarks>
        /// Reusable transforms allow the internal state to be reset for subsequent operations using the same instance. One-shot algorithms
        /// that clear sensitive key material after finalization typically return <see langword="false" />.
        /// </remarks>
        public override bool CanReuseTransform => true;

        /// <summary>
        /// Gets a value indicating whether this transform supports processing multiple blocks of data in a single operation.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if multiple input blocks can be transformed in sequence without intermediate finalization; otherwise, <see langword="false" />.
        /// </value>
        /// <remarks>
        /// Most hash algorithms and block ciphers support multi-block transformations for streaming input. If <see langword="false" />, the
        /// transform must be invoked one block at a time.
        /// </remarks>
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
            PartA = T.One;
            PartB = T.Zero;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
        /// </param>
        /// <remarks>Ensures all internal secrets are overwritten with zeros before releasing resources.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) return;

            if (disposing)
            {
                CryptoHelpers.ClearAndNullify(ref HashValue);

                PartA = PartB = T.Zero;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the <see cref="AdlerBase{T}" /> hashing algorithm. This method
        /// updates the internal state by processing <paramref name="cbSize" /> bytes starting at the specified <paramref name="ibStart" /> offset.
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
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, offset, cbSize);
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            HashCore(array.AsSpan(ibStart, cbSize));
        }

        /// <summary>
        /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="AdlerBase{T}" /> hashing
        /// algorithm. This method updates the internal hash state accordingly by consuming the entire input span.
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
            T pA = PartA, pB = PartB;

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

            PartA = pA;
            PartB = pB;
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
                throw new ObjectDisposedException(nameof(Adler));
#endif
        }

        /// <summary>
        /// Throws a <see cref="CryptographicUnexpectedOperationException" /> if the hash algorithm has already started processing data,
        /// indicating that the instance is in a finalized or non-configurable state.
        /// </summary>
        /// <remarks>
        /// This method is used to prevent reconfiguration of algorithm parameters such as the key, number of rounds, or other settings once
        /// hashing has begun. It ensures settings are immutable after initialization.
        /// </remarks>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// Thrown when an attempt is made to modify the algorithm after it has entered a non-zero state, which indicates that hashing has
        /// started or been finalized.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfInvalidState()
        {
            if (State != 0)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
        }
    }
}