// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Base class for the <c>CityHash</c> family of non-cryptographic hash algorithms developed by Google. See the
    /// <a href="https://github.com/google/cityhash">CityHash reference repository</a> for the specification.
    /// </summary>
    /// <typeparam name="T">
    /// The concrete CityHash variant derived from this class. Must expose a public parameterless constructor.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// CityHash is a one-shot algorithm. To satisfy the incremental input contract of <see cref="HashAlgorithm" />, this base class
    /// accumulates all bytes delivered through <c>HashCore</c> into an internal buffer and invokes the derived variant's
    /// <see cref="ComputeHashCore(ReadOnlySpan{byte})" /> from <see cref="HashFinal" /> once all input is available.
    /// </para>
    /// <para>
    /// Shared mixing primitives (<see cref="Mix(uint)" />, <see cref="Mur(uint, uint)" />, <see cref="Permute3(ref uint, ref uint, ref uint)" />)
    /// and the algorithm constants are defined here and are available to all derived variants. Supported output sizes are 32, 64, and 128 bits.
    /// </para>
    /// <note type="important">
    /// CityHash is <b>not</b> cryptographically secure. It must <b>not</b> be used for password hashing, digital signatures, or any
    /// application that requires collision resistance under adversarial conditions.
    /// </note>
    /// </remarks>
    public abstract class CityHash<T>
        : System.Security.Cryptography.HashAlgorithm
        where T : CityHash<T>, new()
    {
        /// <summary>The first Murmur-style mixing constant used in 32-bit operations.</summary>
        protected const uint C1 = 0xcc9e2d51;

        /// <summary>The second Murmur-style mixing constant used in 32-bit operations.</summary>
        protected const uint C2 = 0x1b873593;

        /// <summary>The 32-bit finalisation magic constant applied during the iterative mixing phase.</summary>
        protected const uint HashMagic = 0xe6546b64;

        /// <summary>The first 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
        protected const ulong K0 = 0xc3a5c85c97cb3127;

        /// <summary>The second 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
        protected const ulong K1 = 0xb492b66fbe98f273;

        /// <summary>The third 64-bit mixing constant, derived from the CityHash reference implementation.</summary>
        protected const ulong K2 = 0x9ae16a3b2f90404f;

        private static readonly int[] ValidHashSizes = { 32, 64, 128 };

        // Accumulates all data delivered through one or more HashCore calls so that HashFinal can
        // invoke ComputeHashCore on the complete input in a single pass, as the algorithm requires.
        private MemoryStream? _inputBuffer = new MemoryStream();

        private bool disposed = false;

#if !NET6_0_OR_GREATER
        // Required for .NET Standard 2.0 or older target frameworks — tracks whether HashFinal
        // has been called since the last Initialize so we can enforce the finalized guard.
        private bool finalized;
#endif

        /// <summary>
        /// Initialises a new instance of the <see cref="CityHash{T}" /> class with the specified hash output size.
        /// </summary>
        /// <param name="hashSize">
        /// The desired hash output size in bits. Must be one of 32, 64, or 128.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="hashSize" /> is not one of the supported values: 32, 64, or 128.
        /// </exception>
        protected CityHash(int hashSize)
        {
            if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
                throw new ArgumentOutOfRangeException(nameof(hashSize),
                    string.Format(ResourceStrings.CryptographicException_InvalidHashSize, hashSize, string.Join(", ", ValidHashSizes)));

            HashSizeValue = hashSize;

            // Call Initialize directly (without the disposed guard) because the instance is not
            // yet fully constructed. The buffer was already allocated above; this call resets it.
            ResetState();
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Public API
        // ---------------------------------------------------------------------------------------------------------------

        /// <inheritdoc />
        /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
        public override void Initialize()
        {
            // The base class calls Initialize() internally (via CaptureHashCodeAndReinitialize) after
            // every ComputeHash or TransformFinalBlock. At that point the instance is not disposed,
            // so ThrowIfDisposed is a no-op. When called explicitly on a disposed instance (as the
            // test Initialize_WhenDisposed_ShouldThrowExactly requires), it correctly throws.
            ThrowIfDisposed();
            ResetState();
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Shared mixing primitives
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Applies a final avalanche mixing step to a 32-bit value, improving bit diffusion.
        /// </summary>
        /// <param name="h">The 32-bit value to mix.</param>
        /// <returns>The mixed 32-bit result.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static uint Mix(uint h) =>
            h ^ (h >> 16);

        /// <summary>
        /// Applies a single Murmur-style multiply-rotate-XOR step, combining two 32-bit values.
        /// </summary>
        /// <param name="a">The input value to multiply and fold.</param>
        /// <param name="h">The accumulator value to combine with the result.</param>
        /// <returns>The result of the Murmur mixing step.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static uint Mur(uint a, uint h) =>
            unchecked((a * C1) ^ h.RotateBitsRight(17));

        /// <summary>
        /// Performs a cyclic three-way permutation of the given values, assigning <c>a ← c</c>, <c>c ← b</c>, <c>b ← a</c>.
        /// </summary>
        /// <param name="a">The first value, receives the original value of <paramref name="c" />.</param>
        /// <param name="b">The second value, receives the original value of <paramref name="a" />.</param>
        /// <param name="c">The third value, receives the original value of <paramref name="b" />.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void Permute3(ref uint a, ref uint b, ref uint c)
        {
            uint t = a;
            a = c;
            c = b;
            b = t;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Abstract algorithm core
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Performs the full hash computation over the complete accumulated input in a single pass.
        /// </summary>
        /// <param name="source">The complete input bytes to hash.</param>
        /// <returns>A byte array containing the final hash output.</returns>
        /// <remarks>
        /// Called exactly once per hash operation from <see cref="HashFinal" />, after all input has been accumulated into the internal
        /// buffer. Derived classes must implement this method to provide the variant-specific algorithm.
        /// </remarks>
        protected abstract byte[] ComputeHashCore(ReadOnlySpan<byte> source);

        // ---------------------------------------------------------------------------------------------------------------
        // HashAlgorithm pipeline
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Accepts a segment of the input byte array and appends it to the internal accumulation buffer.
        /// Forwards to the span-based overload after performing parameter validation.
        /// </summary>
        /// <param name="array">The input byte array. Must not be <see langword="null" />.</param>
        /// <param name="ibStart">The zero-based index at which to begin reading data.</param>
        /// <param name="cbSize">The number of bytes to process from <paramref name="array" />.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <para><paramref name="ibStart" /> is less than 0.</para>
        /// <para>-or-</para>
        /// <para><paramref name="cbSize" /> is less than 0.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="ibStart" /> and <paramref name="cbSize" /> describe a range that exceeds the bounds of <paramref name="array" />.
        /// </exception>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The hash computation has already been finalised and cannot accept further input.
        /// </exception>
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            ThrowHelper.ThrowIfNull(array);

#if !NET6_0_OR_GREATER
            ThrowHelper.ThrowIfLessThan(ibStart, 0);
            ThrowHelper.ThrowIfLessThan(cbSize, 0);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, ibStart, cbSize);

            if (this.finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            HashCore(array.AsSpan(ibStart, cbSize));
        }

        /// <summary>
        /// Appends the contents of <paramref name="source" /> to the internal accumulation buffer. The actual hash computation is deferred
        /// until <see cref="HashFinal" /> is called.
        /// </summary>
        /// <param name="source">The input bytes to accumulate.</param>
        /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            ThrowIfDisposed();

            // Accumulate data; the hash is computed once in HashFinal from the complete buffer.
            if (source.Length > 0)
            {
#if NET6_0_OR_GREATER
                _inputBuffer!.Write(source);
#else
                byte[] temp = source.ToArray();
                _inputBuffer!.Write(temp, 0, temp.Length);
#endif
            }
        }

        /// <summary>
        /// Finalises the hash computation by invoking <see cref="ComputeHashCore(ReadOnlySpan{byte})" /> on the complete accumulated input.
        /// </summary>
        /// <returns>
        /// A byte array containing the hash of all data delivered via preceding <c>HashCore</c> calls. For an empty input the result is
        /// algorithm-defined (for example, the <c>K2</c> constant for CityHash64).
        /// </returns>
        /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
        protected override byte[] HashFinal()
        {
            ThrowIfDisposed();

#if !NET6_0_OR_GREATER
            if (this.finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);

            this.finalized = true;
            State = 2;
#endif

            // Compute the hash of everything accumulated since the last Initialize call.
            byte[] data = _inputBuffer!.ToArray();
            return ComputeHashCore(data);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Disposal
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Releases managed resources, disposes the input accumulation buffer, and clears the hash value.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources;
        /// <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                _inputBuffer?.Dispose();
                _inputBuffer = null;

                CryptoHelpers.ClearAndNullify(ref HashValue);
            }

            disposed = true;
            base.Dispose(disposing);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Private helpers
        // ---------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Resets all mutable algorithm state without performing a disposal check. Used during
        /// construction (before the disposed flag is meaningful) and from <see cref="Initialize" />.
        /// </summary>
        private void ResetState()
        {
            _inputBuffer!.SetLength(0);
            _inputBuffer.Position = 0;

#if !NET6_0_OR_GREATER
            this.finalized = false;
            State = 0;
#endif
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException" /> if this instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(this.disposed, this);
#else
            if (this.disposed)
                throw new ObjectDisposedException(GetType().Name);
#endif
        }

        /// <summary>
        /// Throws a <see cref="CryptographicUnexpectedOperationException" /> if the algorithm is in a
        /// non-zero state, preventing reconfiguration of immutable parameters after hashing has begun.
        /// </summary>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The algorithm has entered a non-zero state indicating that hashing has started or been finalised.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfInvalidState()
        {
            if (State != 0)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
        }
    }
}