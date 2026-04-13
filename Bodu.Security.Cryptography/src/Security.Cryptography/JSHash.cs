namespace Bodu.Security.Cryptography
{
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

    /// <summary>
    /// Computes the hash for the input data using the <c>JSHash</c> hash algorithm. This variant applies a non-cryptographic bitwise mixing
    /// function developed by Justin Sobel to produce a 32-bit hash value. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// JSHash is a compact and efficient bitwise hash function, originally developed by Justin Sobel. It is suitable for hash
    /// permutationTable indexing and other non-cryptographic use cases where speed is preferred over security.
    /// </para>
    /// <para>The core hash function operates using the expression: <c><![CDATA[hash ^= (hash << 5) + (hash >> 2) + byte]]></c>.</para>
    /// ///
    /// <para>
    /// The computed hash is returned as a 4-byte array in the <b>system's native endianness</b>. On little-endian platforms (such as most
    /// modern desktops), the least significant byte appears first. Consumers requiring a specific byte order (e.g., big-endian for network
    /// protocols) should normalize the output using <see cref="System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(uint)" /> as needed.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public sealed class JSHash
        : System.Security.Cryptography.HashAlgorithm
    {
        private const uint DefaultValue = 0x4E67C6A7;

        private bool disposed = false;
        private uint workingHash;
#if !NET6_0_OR_GREATER

        // Required for .NET Standard 2.0 or older frameworks
        private bool finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="JSHash" /> class.
        /// </summary>
        /// <remarks>This constructor initializes the hash algorithm to a default 32-bit output with a seed value of <c>0x4E67C6A7</c>.</remarks>
        public JSHash()
        {
            this.HashSizeValue = 32;
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

        /// <inheritdoc />
        public override void Initialize()
        {
#if !NET6_0_OR_GREATER
            State = 0;
            finalized = false;
#endif
            this.ThrowIfDisposed();
            this.workingHash = DefaultValue;
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
            if (this.disposed)
                return;

            if (disposing)
            {
                CryptoHelpers.ClearAndNullify(ref this.HashValue);

                this.workingHash = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the <see cref="JSHash" /> hashing algorithm. This method updates
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
        /// The hash algorithm has already been finalized and cannot accept more input data.
        /// </exception>
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            ThrowHelper.ThrowIfNull(array);
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            ThrowHelper.ThrowIfLessThan(ibStart, 0);
            ThrowHelper.ThrowIfLessThan(cbSize, 0);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, offset, cbSize);
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            this.HashCore(array.AsSpan(ibStart, cbSize));
        }

        /// <summary>
        /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="JSHash" /> hashing algorithm.
        /// This method updates the internal hash state accordingly by consuming the entire input span.
        /// </summary>
        /// <param name="source">The input byte span containing the data to hash.</param>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The hash algorithm has already been finalized and cannot accept more input data.
        /// </exception>
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            this.ThrowIfDisposed();

            var v = this.workingHash;
            foreach (byte b in source)
            {
                v ^= (v << 5) + (v >> 2) + b;
            }

            this.workingHash = v;
        }

        /// <summary>
        /// Finalizes the hash computation and returns the resulting 32-bit <see cref="JSHash" /> hash. This method reflects all input
        /// previously processed via <see cref="HashAlgorithm.HashCore(byte[], int, int)" /> or
        /// <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})" /> and produces a final, stable hash output.
        /// </summary>
        /// <returns>
        /// A 4-byte array representing the computed <c>JSHash</c> hash value. The result is encoded in the platform’s native byte order.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method completes the internal state of the hashing algorithm and serializes the final hash value into a
        /// platform-independent format. It is invoked automatically by <see cref="HashAlgorithm.ComputeHash(byte[])" /> and related methods
        /// once all data has been processed.
        /// </para>
        /// <para>After this method returns, the internal state is considered finalized and the computed hash is stable.</para>
        /// <para>
        /// In .NET 6.0 and later, the algorithm is automatically reset by invoking <see cref="HashAlgorithm.Initialize" />, allowing the
        /// instance to be reused immediately.
        /// </para>
        /// <para>
        /// In earlier versions of .NET, the internal state is marked as finalized, and any subsequent calls to
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
            Span<byte> span = stackalloc byte[4];
            MemoryMarshal.Write(span, in this.workingHash);
            return span.ToArray();
        }

        /// <summary>
        /// Throws an <see cref="ObjectDisposedException" /> if the instance has already been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the algorithm has been disposed and further access is attempted.</exception>
        private void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(this.disposed, this);
#else
            if (disposed)
                throw new ObjectDisposedException(nameof(JSHash));
#endif
        }
    }
}