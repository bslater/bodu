// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using Bodu.Extensions;

    /// <summary>
    /// Provides a base class for the Fletcher checksum family (Fletcher-16, Fletcher-32, Fletcher-64).
    /// </summary>
    /// <typeparam name="TSelf">The concrete derived type (CRTP) used for block-hash reuse.</typeparam>
    /// <remarks>
    /// <para>
    /// Fletcher is a non-cryptographic position-dependent checksum that maintains two running accumulators (A and B) and combines them into
    /// the final hash. Derived types <see cref="Fletcher16" />, <see cref="Fletcher32" />, and <see cref="Fletcher64" /> select the output width.
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
            this.HashSizeValue = hashSize;
            this.partA = this.partB = 0;
        }

        /// <summary>
        /// Gets the algorithm name in the form <c>Fletcher-N</c>, where <c>N</c> is the output width in bits.
        /// </summary>
        /// <value>A string such as <c>Fletcher-16</c>, <c>Fletcher-32</c>, or <c>Fletcher-64</c>.</value>
        public string AlgorithmName =>
            $"Fletcher-{this.HashSizeValue}";

        /// <inheritdoc />
        public override bool CanReuseTransform => true;

        /// <inheritdoc />
        public override bool CanTransformMultipleBlocks => true;

        /// <inheritdoc />
        public override int InputBlockSize => this.BlockSizeBytes;

        /// <inheritdoc />
        public override int OutputBlockSize => this.BlockSizeBytes;

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
                CryptoHelpers.ClearAndNullify(ref this.HashValue);

                this.partA = this.partB = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
        {
            Span<byte> buffer = stackalloc byte[this.BlockSizeBytes];
            block.CopyTo(buffer);
            return buffer.ToArray();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
            ulong b = 0;
            for (int i = 0; i < block.Length && i < this.BlockSizeBytes; i++)
            {
                b |= ((ulong)block[i]) << ((this.BlockSizeBytes - (i + 1)) << 3);
            }

            // Update the internal state (partA and partB)
            this.partA = (this.partA + b) % this.modulus;
            this.partB = (this.partB + this.partA) % this.modulus;
        }

        /// <inheritdoc />
        protected override byte[] ProcessFinalBlock()
        {
            // Combine partA and partB into the final hash value
            ulong finalHash = (this.partA << (this.HashSizeValue / 2)) | this.partB;

            // Convert to a byte array and take the size
            return finalHash.GetBytes().SliceInternal(0, this.HashSizeValue / 8);
        }

        /// <inheritdoc />
        protected override bool ShouldPadFinalBlock() => false;
    }
}