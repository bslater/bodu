// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Bernstein.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;

    /// <summary>
    /// Computes a 32-bit non-cryptographic hash using Daniel J. Bernstein's djb2 algorithm, optionally using the XOR-modified variant. This
    /// class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default algorithm computes <c>hash = (hash * 33) + c</c> for each input byte <c>c</c>. Setting
    /// <see cref="UseModifiedAlgorithm" /> to <see langword="true" /> selects the XOR-modified form, <c>hash = (hash * 33) ^ c</c>, which
    /// may give better distribution in some hash-table workloads.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class Bernstein :
         System.Security.Cryptography.HashAlgorithm
    {
        /// <summary>
        /// The default initial value used to seed the hash algorithm. This is constant.
        /// </summary>
        public const uint DefaultInitialValue = 5381U;

        private bool disposed = false;


        private uint initialValue;
        private bool useModified;
        private uint workingHash;
#if !NET6_0_OR_GREATER
        // Required for .NET Standard 2.0 or older frameworks
        private bool finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Bernstein" /> class with default parameters.
        /// </summary>
        public Bernstein()
        {
            this.HashSizeValue = 32;
            this.initialValue = this.workingHash = DefaultInitialValue;
            this.useModified = false;
        }

        /// <inheritdoc />
        public override bool CanReuseTransform => true;

        /// <inheritdoc />
        public override bool CanTransformMultipleBlocks => true;

        /// <summary>
        /// Gets or sets the initial seed value used to start the hash computation.
        /// </summary>
        /// <value>The initial hash code value. Defaults to <see cref="DefaultInitialValue" />.</value>
        /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
        /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
        public uint InitialValue
        {
            get
            {
                this.ThrowIfDisposed();

                return this.initialValue;
            }

            set
            {
                this.ThrowIfDisposed();
                this.ThrowIfInvalidState();

                this.initialValue = value;
                this.Initialize();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use the XOR-modified variant of the <see cref="Bernstein" /> hash algorithm.
        /// </summary>
        /// <value>
        /// <see langword="true" /> to use the modified algorithm ( <c>hash = (hash * 33) ^ c</c>); <see langword="false" /> to use the
        /// original djb2 form ( <c>hash = (hash * 33) + c</c>). The default is <see langword="false" />.
        /// </value>
        /// <exception cref="ObjectDisposedException">Instance has been disposed and its members are accessed.</exception>
        /// <exception cref="CryptographicUnexpectedOperationException">The hash computation has already started.</exception>
        public bool UseModifiedAlgorithm
        {
            get
            {
                this.ThrowIfDisposed();

                return this.useModified;
            }

            set
            {
                this.ThrowIfDisposed();
                this.ThrowIfInvalidState();

                this.useModified = value;
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
            this.workingHash = this.initialValue;
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

                this.initialValue = this.workingHash = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the <see cref="Bernstein" /> hashing algorithm. This method
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
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            ThrowHelper.ThrowIfLessThan(ibStart, 0);
            ThrowHelper.ThrowIfLessThan(cbSize, 0);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, ibStart, cbSize);
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            if (this.useModified)
                this.HashModified(array.AsSpan(ibStart, cbSize));
            else
                this.HashOriginal(array.AsSpan(ibStart, cbSize));
        }

        /// <summary>
        /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="Bernstein" /> hashing algorithm.
        /// This method updates the internal hash state accordingly by consuming the entire input span.
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

            if (this.useModified)
                this.HashModified(source);
            else
                this.HashOriginal(source);
        }

        /// <summary>
        /// Finalises the Bernstein hash computation and returns the result as a 4-byte big-endian array.
        /// </summary>
        /// <returns>A 4-byte array containing the 32-bit hash value in <b>big-endian</b> byte order.</returns>
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
            BinaryPrimitives.WriteUInt32BigEndian(span, this.workingHash); // Explicit big-endian output
            return span.ToArray();
        }

        /// <summary>
        /// Updates the internal hash state using the modified Bernstein hash algorithm (hash = hash * 33 ^ b).
        /// </summary>
        /// <param name="data">
        /// The input data to hash. Each byte is processed sequentially and combined into the internal hash state using XOR.
        /// </param>
        /// <remarks>
        /// This variation of the Bernstein hash function replaces the addition step with a bitwise XOR operation for alternative mixing behavior.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashModified(ReadOnlySpan<byte> data)
        {
            uint v = this.workingHash;
            foreach (byte b in data)
            {
                v = ((v << 5) + v) ^ b;
            }

            this.workingHash = v;
        }

        /// <summary>
        /// Updates the internal hash state using the original Bernstein hash algorithm (hash = hash * 33 + b).
        /// </summary>
        /// <param name="data">
        /// The input data to hash. Each byte is processed sequentially and combined into the internal hash state using addition.
        /// </param>
        /// <remarks>This implementation corresponds to the original "djb2" hash function commonly used in hash table implementations.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashOriginal(ReadOnlySpan<byte> data)
        {
            uint v = this.workingHash;
            foreach (var b in data)
            {
                v = ((v << 5) + v) + b;
            }

            this.workingHash = v;
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
                throw new ObjectDisposedException(nameof(Bernstein));
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