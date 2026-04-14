// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BKDR.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
namespace Bodu.Security.Cryptography
{
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;

    /// <summary>
    /// Computes a 32-bit non-cryptographic hash using the BKDR polynomial rolling algorithm from Kernighan and Ritchie's "The C Programming
    /// Language". This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each input byte <c>c</c> the hash is updated as <c>hash = (hash * seed) + c</c>. The <see cref="Seed" /> multiplier must be one
    /// of the supported values (31, 131, 1313, 13131, 131313, 1313131, 13131313, 131313131, 1313131313).
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class BKDR
        : System.Security.Cryptography.HashAlgorithm
    {
        /// <summary>
        /// Represents the default seed value used by the <see cref="BKDR" /> hash algorithm.
        /// </summary>
        public const uint DefaultSeed = 131U;

        private static readonly uint[] ValidSeedValues = new[]
        {
            31U, 131U, 1313U, 13131U, 131313U, 1313131U, 13131313U, 131313131U, 1313131313U
        };

        private bool disposed = false;
        private uint seedValue;
        private uint workingHash;
#if !NET6_0_OR_GREATER
        private bool finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="BKDR" /> class.
        /// </summary>
        public BKDR()
        {
            this.seedValue = DefaultSeed;
            this.HashSizeValue = 32;
            this.Initialize();
        }

        /// <inheritdoc />
        public override bool CanReuseTransform => true;

        /// <inheritdoc />
        public override bool CanTransformMultipleBlocks => true;

        /// <summary>
        /// Gets or sets the seed value used in the BKDR hash algorithm.
        /// </summary>
        /// <value>The seed value. Must be one of the supported seed constants.</value>
        /// <exception cref="ObjectDisposedException">Thrown when accessing after disposal.</exception>
        /// <exception cref="CryptographicUnexpectedOperationException">Thrown when modified after hashing has started.</exception>
        /// <exception cref="ArgumentException">Thrown when the seed value is not supported.</exception>
        public uint Seed
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

                if (Array.IndexOf(ValidSeedValues, value) == -1)
                    throw new ArgumentException(
                        string.Format(ResourceStrings.CryptographicException_InvalidPropertyValue, nameof(this.Seed)), nameof(value));

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
        /// <remarks>Ensures all internal secrets are overwritten with zeros before releasing resources.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) return;
            if (disposing)
            {
                CryptoHelpers.ClearAndNullify(ref this.HashValue);

                this.workingHash = this.seedValue = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the <see cref="BKDR" /> hashing algorithm. This method updates the
        /// internal state by processing <paramref name="cbSize" /> bytes starting at the specified <paramref name="ibStart" /> offset.
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
        /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="BKDR" /> hashing algorithm. This
        /// method updates the internal hash state accordingly by consuming the entire input span.
        /// </summary>
        /// <param name="source">The input byte span containing the data to hash.</param>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The hash algorithm has already been finalized and cannot accept more input data.
        /// </exception>
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif
            uint v = this.workingHash;
            foreach (var b in source)
            {
                v = (v * this.seedValue) + b;
            }

            this.workingHash = v;
        }

        /// <summary>
        /// Finalises the BKDR hash computation and returns the 32-bit result as a 4-byte big-endian array.
        /// </summary>
        /// <returns>A 4-byte array containing the hash value in <b>big-endian</b> byte order.</returns>
        protected override byte[] HashFinal()
        {
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
            finalized = true;
            State = 2;
#endif
            Span<byte> span = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(span, this.workingHash);
            return span.ToArray();
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
                throw new ObjectDisposedException(nameof(BKDR));
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
            if (this.State != 0)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
        }
    }
}