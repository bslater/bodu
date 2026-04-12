// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
using Bodu.Extensions;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Bodu.Security.Cryptography
{
    public abstract class CityHashBase<T>
        : System.Security.Cryptography.HashAlgorithm
        where T : CityHashBase<T>, new()
    {
        protected const UInt32 C1 = 0xcc9e2d51;
        protected const UInt32 C2 = 0x1b873593;
        protected const uint HashMagic = 0xe6546b64;
        protected const UInt64 K0 = 0xc3a5c85c97cb3127;
        protected const UInt64 K1 = 0xb492b66fbe98f273;
        protected const UInt64 K2 = 0x9ae16a3b2f90404f;
        private static readonly int[] ValidHashSizes = { 32, 64, 128 };

        private byte[]? computedHash;
        private bool disposed = false;
#if !NET6_0_OR_GREATER

        // Required for .NET Standard 2.0 or older frameworks
        private bool finalized;
#endif

        protected CityHashBase(int hashSize)
        {
            if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
                throw new ArgumentOutOfRangeException(nameof(hashSize),
                    string.Format(ResourceStrings.CryptographicException_InvalidHashSize, hashSize, string.Join(", ", ValidHashSizes)));

            HashSizeValue = hashSize;
            Initialize();
        }

        /// <inheritdoc />
        public override void Initialize()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static uint Mix(uint h) =>
                    h ^ (h >> 16);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static uint Mur(uint a, uint h) =>
                    unchecked((a * C1) ^ h.RotateBitsRight(17));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Permute3(ref uint a, ref uint b, ref uint c)
        {
            uint t = a;
            a = c;
            c = b;
            b = t;
        }

        /// <summary>
        /// Performs the full hashing process on a complete input span. Used when ComputeHash is called in one shot (non-streaming).
        /// </summary>
        protected abstract byte[] ComputeHashCore(ReadOnlySpan<byte> source);

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                CryptoHelpers.ClearAndNullify(ref HashValue);
            }

            disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Performs the final hash computation after all blocks have been processed. Only used if streaming (TransformBlock) is supported.
        /// </summary>
        protected virtual byte[] FinalizeHashCore() => Array.Empty<byte>();

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the <see cref="CityHashBase{T}" /> hashing algorithm. This method
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
        /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="CityHashBase{T}" /> hashing
        /// algorithm. This method updates the internal hash state accordingly by consuming the entire input span.
        /// </summary>
        /// <param name="source">The input byte span containing the data to hash.</param>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The hash algorithm has already been finalized and cannot accept more input data.
        /// </exception>
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            this.ThrowIfDisposed();

            this.computedHash = ComputeHashCore(source);
        }

        /// <inheritdoc />
        protected override byte[] HashFinal()
        {
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);

            finalized = true;
            State = 2;
#endif

            return new byte[0];
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException" /> if the algorithm instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when any public method or property is accessed after the instance has been disposed.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(disposed, this);
#else
            if (disposed)
                throw new ObjectDisposedException(nameof(T));
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
        private void ThrowIfInvalidState()
        {
            if (State != 0)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
        }
    }
}