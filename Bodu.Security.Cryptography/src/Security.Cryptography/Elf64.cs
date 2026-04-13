// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using Bodu.Extensions;

    /// <summary>
    /// Computes the hash for the input data using the <c>ELF-64</c> (Executable and Linkable Format) hash algorithm. This variant applies a
    /// non-cryptographic bitwise transformation commonly used in Unix object file processing and hash table indexing. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ELF hashing is a simple, non-cryptographic routine originally used in the UNIX System V ELF object file format. It processes each
    /// byte of input by shifting and mixing bits to produce a pseudo-random but repeatable hash output.
    /// </para>
    /// <para>
    /// This implementation uses a 64-bit internal state and is intended for fast hashing of byte sequences such as identifiers or text
    /// keys. It is <b>not suitable</b> for cryptographic purposes.
    /// </para>
    /// <para>
    /// An optional <see cref="Seed" /> value may be specified to alter the initial state. The seed cannot be changed once hashing begins.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class Elf64
        : System.Security.Cryptography.HashAlgorithm
    {
        private const ulong HighBitsMask = 0xF000000000000000UL;
        private const int HighBitsShift = 56;

        private bool disposed = false;
        private ulong seedValue;
        private ulong workingHash;
#if !NET6_0_OR_GREATER

        // Required for .NET Standard 2.0 or older frameworks
        private bool finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Elf64" /> class.
        /// </summary>
        public Elf64()
        {
            this.HashSizeValue = 64;
            this.Initialize();
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

        /// <summary>
        /// Gets or sets the seed used to initialise the internal hash state.
        /// </summary>
        /// <value>The seed value applied before hashing begins.</value>
        /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
        /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
        /// <remarks>
        /// Changing the seed influences the initial hash state and therefore the resulting hash output. Common seed values such as 31, 131,
        /// or 1313 are often used to reduce clustering or bias.
        /// </remarks>
        public ulong Seed
        {
            get
            {
                this.ThrowIfDisposed();

                return this.seedValue;
            }

            set
            {
                this.ThrowIfDisposed();
                this.ThrowIfInvalidState();

                this.seedValue = value;
                this.Initialize();
            }
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            State = 0;
            finalized = false;
#endif
            this.workingHash = this.seedValue;
        }

        /// <summary>
        /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
        /// </param>
        /// <remarks>Ensures all internal state is overwritten with zeros before releasing resources.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) return;

            if (disposing)
            {
                CryptoHelpers.ClearAndNullify(ref this.HashValue);

                this.seedValue = this.workingHash = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the <see cref="Elf64" /> hashing algorithm. This method updates
        /// the internal state by processing <paramref name="cbSize" /> bytes starting at the specified <paramref name="ibStart" /> offset.
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
        /// The hash algorithm has already been finalised and cannot accept more input data.
        /// </exception>
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            ThrowHelper.ThrowIfNull(array);
            this.ThrowIfDisposed();
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
        /// Processes the input <paramref name="source" /> and feeds it into the <see cref="Elf64" /> hashing algorithm, updating the
        /// internal hash state accordingly.
        /// </summary>
        /// <param name="source">The input byte span containing the data to hash.</param>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The hash algorithm has already been finalised and cannot accept more input data.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif
            ulong v = this.workingHash;

            foreach (byte b in source)
            {
                v = (v << 4) + b;

                ulong high = v & HighBitsMask;
                v ^= high >> HighBitsShift;
                v &= ~high;
            }

            this.workingHash = v;
        }

        /// <summary>
        /// Finalises the hash computation and returns the resulting 64-bit <see cref="Elf64" /> hash in big-endian format. This method
        /// reflects all input previously processed via <see cref="HashAlgorithm.HashCore(byte[], int, int)" /> or
        /// <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})" /> and produces a final, stable hash output.
        /// </summary>
        /// <returns>
        /// An 8-byte array representing the computed <c>Elf64</c> hash value. The result is encoded in <b>big-endian</b> byte order.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method completes the internal state of the hashing algorithm and serialises the final hash value into a
        /// platform-independent format. It is invoked automatically by <see cref="HashAlgorithm.ComputeHash(byte[])" /> and related methods
        /// once all data has been processed.
        /// </para>
        /// <para>After this method returns, the internal state is considered finalised and the computed hash is stable.</para>
        /// <para>
        /// In .NET 6.0 and later, the algorithm is automatically reset by invoking <see cref="HashAlgorithm.Initialize" />, allowing the
        /// instance to be reused immediately.
        /// </para>
        /// <para>
        /// In earlier versions of .NET, the internal state is marked as finalised, and any subsequent calls to
        /// <see cref="HashAlgorithm.HashCore(byte[], int, int)" />, <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})" />, or
        /// <see cref="HashAlgorithm.HashFinal" /> will throw a <see cref="CryptographicUnexpectedOperationException" />. To compute another
        /// hash, you must explicitly call <see cref="HashAlgorithm.Initialize" /> to reset the algorithm.
        /// </para>
        /// <para>
        /// Implementations should ensure all residual or pending data is processed and integrated into the final hash value before returning.
        /// </para>
        /// </remarks>
        protected override byte[] HashFinal()
        {
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);

            finalized = true;
            State = 2;
#endif

            return this.workingHash.GetBytes(asBigEndian: true);
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
            ObjectDisposedException.ThrowIf(this.disposed, this);
#else
            if (disposed)
                throw new ObjectDisposedException(nameof(Elf64));
#endif
        }

        /// <summary>
        /// Throws a <see cref="CryptographicUnexpectedOperationException" /> if the hash algorithm has already started processing data,
        /// indicating that the instance is in a finalised or non-configurable state.
        /// </summary>
        /// <remarks>
        /// This method is used to prevent reconfiguration of algorithm parameters such as the key, number of rounds, or other settings once
        /// hashing has begun. It ensures settings are immutable after initialisation.
        /// </remarks>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// Thrown when an attempt is made to modify the algorithm after it has entered a non-zero state, which indicates that hashing has
        /// started or been finalised.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfInvalidState()
        {
            if (this.State != 0)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
        }
    }
}