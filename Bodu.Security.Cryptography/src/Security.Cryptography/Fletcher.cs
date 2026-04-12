// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Base class for computing hash using the <c>Fletcher</c> hash algorithm family (Fletcher-16, Fletcher-32, Fletcher-64). This class
    /// cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Fletcher algorithm is used to generate non-cryptographic hash values for a given byte sequence. It operates by computing two
    /// components (partA and partB) over the input data and produces a hash based on these.
    /// </para>
    /// <para>
    /// This implementation handles Fletcher hash sizes of 16, 32, and 64 bits. Derived classes (see <see cref="Fletcher16" />,
    /// <see cref="Fletcher32" />, <see cref="Fletcher64" />) can implement specific hash sizes.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for password hashing,
    /// digital signatures, or integrity validation in security-sensitive applications.</note>
    /// </remarks>
    public abstract class Fletcher<TSelf>
        : BlockHashAlgorithm<TSelf>
            where TSelf : Fletcher<TSelf>, new()
    {
        private static readonly int[] ValidHashSizes = { 16, 32, 64 };

        private readonly ulong modulus;
        private bool disposed = false;
        private ulong partA;
        private ulong partB;
#if !NET6_0_OR_GREATER

        // Required for .NET Standard 2.0 or older frameworks
        private bool finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Fletcher{TSelf}" /> class with the specified hash size.
        /// </summary>
        /// <param name="hashSize">The hash size in bits. Valid values are 16, 32, or 64.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="hashSize" /> is not 16, 32, or 64.</exception>
        protected Fletcher(int hashSize)
            : base(ValidHashSizes.Contains(hashSize)
                 ? hashSize / 16
                 : throw new ArgumentException(
                    string.Format(ResourceStrings.CryptographicException_InvalidHashSize, hashSize, string.Join(", ", ValidHashSizes)),
                    nameof(hashSize)))
        {
            this.modulus = (1UL << (hashSize / 2)) - 1;
            HashSizeValue = hashSize;
            this.partA = this.partB = 0;
        }

        /// <summary>
        /// Gets the fully qualified algorithm name, including the variant and hash output size.
        /// </summary>
        /// <value>
        /// A string in the form <c>Fletcher-N</c>, where <c>N</c> is the number of bits in the final hash output (e.g., <c>Fletcher-32</c>
        /// or <c>Fletcher-64</c>).
        /// </value>
        /// <remarks>
        /// The naming convention follows the established Fletcher standard, where the suffix indicates the bit-width of the resulting checksum:
        /// </remarks>
        public string AlgorithmName =>
            $"Fletcher-{HashSizeValue}";

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
        public override int InputBlockSize => BlockSizeBytes;

        /// <inheritdoc />
        public override int OutputBlockSize => BlockSizeBytes;

        /// <inheritdoc />
        public override void Initialize()
        {
            this.ThrowIfDisposed();
            base.Initialize();
#if !NET6_0_OR_GREATER
            State = 0;
            finalized = false;
#endif
            this.partA = this.partB = 0;
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

                this.partA = this.partB = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
        {
            Span<byte> buffer = stackalloc byte[BlockSizeBytes];
            block.CopyTo(buffer);
            return buffer.ToArray();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
            ulong b = 0;
            for (int i = 0; i < block.Length && i < BlockSizeBytes; i++)
            {
                b |= ((ulong)block[i]) << ((BlockSizeBytes - (i + 1)) << 3);
            }

            // Update the internal state (partA and partB)
            this.partA = (this.partA + b) % this.modulus;
            this.partB = (this.partB + this.partA) % this.modulus;
        }

        /// <inheritdoc />
        protected override byte[] ProcessFinalBlock()
        {
            // Combine partA and partB into the final hash value
            ulong finalHash = (this.partA << (HashSizeValue / 2)) | this.partB;

            // Convert to a byte array and take the size
            return finalHash.GetBytes().SliceInternal(0, HashSizeValue / 8);
        }

        /// <inheritdoc />
        protected override bool ShouldPadFinalBlock() => false;
    }
}