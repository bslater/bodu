// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FNV.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System;
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;

    /// <summary>
    /// Base class for computing hashes using the <c>FNV</c> (Fowler-Noll-Vo) hash algorithm family (FNV-1, FNV-1a). This class cannot be
    /// inherited directly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Fowler�Noll�Vo (FNV) family of hash functions is designed to efficiently compute non-cryptographic hash values over input data
    /// using simple bitwise and multiplicative operations. The algorithm maintains a running hash value initialized with a predefined
    /// <c>offset basis</c>, and processes each byte of input by combining multiplication with a large <c>FNV prime</c> and XOR operations.
    /// </para>
    /// <para>
    /// This implementation serves as the foundation for specific hash widths and variants of the FNV hash family. Variants differ by output
    /// size, with 32-bit and 64-bit being common, and by the order of operations used to process each byte:
    /// <list type="bullet">
    /// <item>
    /// <term>FNV-1</term>
    /// <description>Performs multiplication followed by XOR</description>
    /// </item>
    /// <item>
    /// <term>FNV-1a</term>
    /// <description>Performs XOR followed by multiplication</description>
    /// </item>
    /// </list>
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>

    public abstract class Fnv
        : System.Security.Cryptography.HashAlgorithm
    {
        private static readonly int[] ValidHashSizes = { 16, 32, 64 };

        private readonly ulong offsetBasis;
        private readonly ulong prime;
        private readonly bool useFnv1a;
        private bool disposed = false;
        private ulong workingHash;
#if !NET6_0_OR_GREATER

        // Required for .NET Standard 2.0 or older frameworks
        private bool finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Fnv" /> class using the specified configuration parameters.
        /// </summary>
        /// <param name="hashSize">The size, in bits, of the resulting hash value. Only supported values are <c>32</c> and <c>64</c>.</param>
        /// <param name="prime">
        /// The FNV prime multiplier used during the hash computation. This value should correspond to the standard prime for the given hash
        /// size (e.g., <c>0x01000193</c> for 32-bit, <c>0x00000100000001B3</c> for 64-bit).
        /// </param>
        /// <param name="offsetBasis">
        /// The initial offset basis used to seed the hash value. This value should match the standard offset basis for the chosen FNV
        /// variant and hash size (e.g., <c>0x811C9DC5</c> for FNV-1a 32-bit).
        /// </param>
        /// <param name="useFnv1a">
        /// <c>true</c> to use the FNV-1a variant (XOR followed by multiplication); <c>false</c> to use the FNV-1 variant (multiplication
        /// followed by XOR).
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="hashSize" /> is not one of the supported values defined in <c>ValidHashSizes</c>.
        /// </exception>
        /// <remarks>
        /// This constructor is intended to be used by derived classes that implement specific FNV hash variants, such as
        /// <see cref="Fnv1a32" /> or <see cref="Fnv1a64" />. It validates the provided hash size against the supported options and
        /// initializes the internal hashing state accordingly.
        /// </remarks>
        protected Fnv(int hashSize, ulong prime, ulong offsetBasis, bool useFnv1a = false)
        {
            if (!ValidHashSizes.Contains(hashSize))
                throw new ArgumentException(
                    string.Format(ResourceStrings.CryptographicException_InvalidHashSize, hashSize, string.Join(", ", ValidHashSizes)),
                    nameof(hashSize));

            this.HashSizeValue = hashSize;
            this.prime = prime;
            this.offsetBasis = this.workingHash = offsetBasis;
            this.useFnv1a = useFnv1a;
        }

        /// <summary>
        /// Gets the fully qualified algorithm name, including the variant and hash output size.
        /// </summary>
        /// <value>
        /// A string representation in the format <c>FNV-1-32</c> or <c>FNV-1a-64</c>, where:
        /// <list type="bullet">
        /// <item>
        /// <description><c>1</c> or <c>1a</c> indicates the FNV variant used (FNV-1 or FNV-1a).</description>
        /// </item>
        /// <item>
        /// <description>The trailing number indicates the hash output size in bits (e.g., 32, 64).</description>
        /// </item>
        /// </list>
        /// </value>
        public string AlgorithmName
            => $"FNV-{(this.useFnv1a ? "1a" : "1")}-{this.HashSizeValue}";

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
            this.ThrowIfDisposed();
#if !NET6_0_OR_GREATER
            State = 0;
            finalized = false;
#endif
            this.workingHash = this.offsetBasis;
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

                this.workingHash = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the <see cref="Fnv" /> hashing algorithm. This method updates the
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
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, offset, cbSize);
            if (finalized)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            this.HashCore(array.AsSpan(ibStart, cbSize));
        }

        /// <summary>
        /// Processes the entirety of the input <paramref name="source" /> and feeds it into the <see cref="Fnv" /> hashing algorithm. This
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
            if (this.useFnv1a)
                this.HashCoreFNV1a(source);
            else
                this.HashCoreFNV1(source);
        }

        /// <summary>
        /// Finalizes the hash computation and returns the resulting 64-bit <see cref="Fnv" /> hash in big-endian format. This method
        /// reflects all input previously processed via <see cref="HashAlgorithm.HashCore(byte[], int, int)" /> or
        /// <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})" /> and produces a final, stable hash output.
        /// </summary>
        /// <returns>
        /// A 8-byte array representing the computed <c>Bernstein</c> hash value. The result is encoded in <b>big-endian</b> byte order.
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

            Span<byte> buffer = stackalloc byte[8];
            BinaryPrimitives.WriteUInt64BigEndian(buffer, this.workingHash);

            return this.HashSizeValue switch
            {
                32 => [buffer[4], buffer[5], buffer[6], buffer[7]],
                64 => buffer.ToArray(),
                _ => throw new NotSupportedException($"Unsupported hash this.size: {this.HashSizeValue} bits.")
            };
        }

        /// <summary>
        /// Applies the original <c>FNV-1</c> hash transformation to the specified input data by performing multiplication followed by a
        /// bitwise XOR using the configured FNV prime. This method updates the internal hash state for each byte of the input span.
        /// </summary>
        /// <param name="source">The input span containing the data to hash.</param>
        /// <remarks>
        /// This method implements the FNV-1 variant, which multiplies the current hash by the FNV prime before XORing each input byte. It
        /// is intended for internal use and invoked only when <c>useFnv1a</c> is <c>false</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashCoreFNV1(ReadOnlySpan<byte> source)
        {
            var hash = this.workingHash;
            for (int i = 0; i < source.Length; i++)
            {
                hash *= this.prime;
                hash ^= source[i];
            }

            this.workingHash = hash;
        }

        /// <summary>
        /// Applies the <c>FNV-1a</c> hash transformation to the specified input data by performing a bitwise XOR followed by multiplication
        /// using the configured FNV prime. This method updates the internal hash state for each byte of the input span.
        /// </summary>
        /// <param name="source">The input span containing the data to hash.</param>
        /// <remarks>
        /// This method implements the FNV-1a variant, which improves diffusion by XORing each byte before multiplying with the FNV prime.
        /// It is intended for internal use and invoked only when <c>useFnv1a</c> is <c>true</c>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashCoreFNV1a(ReadOnlySpan<byte> source)
        {
            var hash = this.workingHash;
            for (int i = 0; i < source.Length; i++)
            {
                hash ^= source[i];
                hash *= this.prime;
            }

            this.workingHash = hash;
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
                throw new ObjectDisposedException(nameof(Fnv));
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