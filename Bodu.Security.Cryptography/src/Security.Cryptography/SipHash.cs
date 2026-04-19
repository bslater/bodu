// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SipHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using Bodu.Extensions;

    /// <summary>
    /// Base class for the <c>SipHash</c> family of keyed pseudorandom functions, a fast keyed hash designed by Aumasson and Bernstein
    /// for short input messages. See the official <a href="https://131002.net/siphash/">SipHash specification</a> for details.
    /// </summary>
    /// <typeparam name="T">The concrete SipHash variant derived from this class. Must expose a public parameterless constructor.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="SipHash{T}" /> is a keyed hash function that requires a 128-bit (16-byte) secret key. It mixes each input block into
    /// four 64-bit state variables (<c>v0</c> through <c>v3</c>) using Add-Rotate-XOR (ARX) steps, and is designed to resist
    /// hash-flooding attacks against hash tables.
    /// </para>
    /// <para>This base class is extended by:</para>
    /// <list type="bullet">
    /// <item>
    /// <description><see cref="SipHash64" /> produces a 64-bit hash output suitable for compact keyed checksums.</description>
    /// </item>
    /// <item>
    /// <description><see cref="SipHash128" /> produces a 128-bit hash output offering increased collision resistance.</description>
    /// </item>
    /// </list>
    /// <para>
    /// Each 64-bit input block is absorbed during a compression phase consisting of <see cref="CompressionRounds" /> rounds. Once all
    /// input has been processed, <see cref="FinalizationRounds" /> rounds are applied to produce the final digest. The defaults
    /// (<c>c = 2</c>, <c>d = 4</c>) correspond to the standard <c>SipHash-2-4</c> parameterisation.
    /// </para>
    /// </remarks>
    /// <example>
    /// The following example configures a <see cref="SipHash64" /> instance to use the stronger <c>SipHash-4-8</c> parameter set.
    /// <code language="csharp">
    /// using var sipHash = new SipHash64
    /// {
    ///     Key = myKey,
    ///     CompressionRounds = 4,
    ///     FinalizationRounds = 8,
    /// };
    /// byte[] tag = sipHash.ComputeHash(message);
    /// </code>
    /// </example>
    public abstract class SipHash<T>
        : KeyedBlockHashAlgorithm<T>
        where T : SipHash<T>, new()
    {
        /// <summary>
        /// The fixed key size in bytes (128 bits).
        /// </summary>
        public const int KeySize = 16;

        /// <summary>
        /// The minimum number of compression rounds required by SipHash.
        /// </summary>
        public const int MinCompressionRounds = 2;

        /// <summary>
        /// The minimum number of finalization rounds required by SipHash.
        /// </summary>
        public const int MinFinalizationRounds = 4;

        private static readonly int BlockSize = 8;

        private static readonly ulong[] InitialStates = new ulong[]
        {
            0x736f6d6570736575UL,
            0x646f72616e646f6dUL,
            0x6c7967656e657261UL,
            0x7465646279746573UL,
        };

        private static readonly int[] ValidHashSizes = { 64, 128 };
        private int compressionRounds;
        private bool disposed = false;
        private int finalizationRounds;
        private ulong v0, v1, v2, v3;

        /// <summary>
        /// Initializes a new instance of the <see cref="SipHash{T}" /> class with a specified hash size.
        /// </summary>
        /// <param name="hashSize">The desired size of the final hash in bits. Supported values are 64 or 128.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="hashSize" /> is not supported.</exception>
        protected SipHash(int hashSize)
            : base(BlockSize, KeySize)
        {
            if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
                throw new ArgumentOutOfRangeException(nameof(hashSize),
                    string.Format(ResourceStrings.CryptographicException_InvalidHashSize, hashSize, string.Join(", ", ValidHashSizes)));

            this.KeyValue = new byte[KeySize];
            CryptoHelpers.FillWithRandomNonZeroBytes(this.KeyValue);
            this.compressionRounds = MinCompressionRounds;
            this.finalizationRounds = MinFinalizationRounds;
            this.HashSizeValue = hashSize;
            this.OnKeyChanged();
        }

        /// <summary>
        /// Gets the fully qualified algorithm name, including the variant and hash output size.
        /// </summary>
        /// <remarks>
        /// Follows the convention "SipHash-c-d-x", where:
        /// <list type="bullet">
        /// <item>
        /// <description><c>c</c>: compression rounds</description>
        /// </item>
        /// <item>
        /// <description><c>d</c>: finalization rounds</description>
        /// </item>
        /// <item>
        /// <description><c>x</c>: output hash size in bits</description>
        /// </item>
        /// </list>
        /// </remarks>          
        public string AlgorithmName
        {
            get
            {
                this.ThrowIfDisposed();
                return $"SipHash-{this.CompressionRounds}-{this.FinalizationRounds}-{this.HashSizeValue}";
            }
        }

        /// <inheritdoc />
        public override bool CanReuseTransform => true;

        /// <inheritdoc />
        public override bool CanTransformMultipleBlocks => true;

        /// <summary>
        /// Gets or sets the number of compression rounds applied to each input block during the SipHash computation.
        /// </summary>
        /// <value>A positive integer greater than or equal to <see cref="MinCompressionRounds" />. The default is 2.</value>
        /// <remarks>
        /// Compression rounds are performed for every 8-byte message block before finalization. Increasing this value improves diffusion
        /// and resistance to hash-flooding attacks, but also increases computation time.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is less than <see cref="MinCompressionRounds" />.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the algorithm instance has been disposed.</exception>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// Thrown if the hash computation has already begun and the property is modified mid-operation.
        /// </exception>
        public int CompressionRounds
        {
            get
            {
                this.ThrowIfDisposed();
                return this.compressionRounds;
            }

            set
            {
                this.ThrowIfDisposed();
                this.ThrowIfInvalidState();
                ThrowHelper.ThrowIfLessThan(value, MinCompressionRounds);

                this.compressionRounds = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of finalization rounds executed after all message blocks have been absorbed.
        /// </summary>
        /// <value>A positive integer greater than or equal to <see cref="MinFinalizationRounds" />. The default is 4.</value>
        /// <remarks>
        /// Finalization rounds strengthen the avalanche effect after all input is processed. Increasing this value improves security at the
        /// cost of additional computation during final hash derivation.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is less than <see cref="MinFinalizationRounds" />.</exception>
        /// <exception cref="ObjectDisposedException">Thrown if the algorithm instance has been disposed.</exception>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// Thrown if the hash computation has already begun and the property is modified mid-operation.
        /// </exception>
        public int FinalizationRounds
        {
            get
            {
                this.ThrowIfDisposed();
                return this.finalizationRounds;
            }

            set
            {
                this.ThrowIfDisposed();
                this.ThrowIfInvalidState();
                ThrowHelper.ThrowIfLessThan(value, MinFinalizationRounds);

                this.finalizationRounds = value;
            }
        }

        /// <inheritdoc />
        public override int InputBlockSize => BlockSize;

        /// <inheritdoc />
        public override int OutputBlockSize => BlockSize;

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

                this.v0 = this.v1 = this.v2 = this.v3 = 0;
                this.compressionRounds = 0;
                this.finalizationRounds = 0;
                this.HashSizeValue = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        protected override byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength)
        {
            if ((uint)block.Length > 7)
                throw new ArgumentOutOfRangeException(nameof(block), "Residual block must be 0-7 bytes.");

            Span<byte> buffer = stackalloc byte[8];
            block.CopyTo(buffer);
            buffer[7] = (byte)messageLength;

            return buffer.ToArray();
        }

        /// <summary>
        /// Processes a single 64-bit block of data using SipHash compression.
        /// </summary>
        /// <param name="block">The 64-bit block to process.</param>
        /// <remarks>Updates internal state using <see cref="PerformSipRounds" /> and XOR operations.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void ProcessBlock(ReadOnlySpan<byte> block)
        {
            var b = BinaryPrimitives.ReadUInt64LittleEndian(block);
            this.v3 ^= b;
            this.PerformSipRounds(this.compressionRounds);
            this.v0 ^= b;
        }

        //    // Buffer any remaining residual bytes
        //    residualBytes = end - pos;
        //    if (residualBytes > 0)
        //        buffer.AsSpan(pos, residualBytes).CopyTo(residualSpan);
        //}
        /// <summary>
        /// Finalizes the hash computation and produces the output hash value.
        /// </summary>
        /// <returns>A byte array containing the final hash value (8 or 16 bytes).</returns>
        /// <remarks>Combines all partial input and applies the finalization round logic based on the configured output size.</remarks>
        protected override byte[] ProcessFinalBlock()
        {
            this.v2 ^= (this.HashSizeValue == 64) ? 0xffUL : 0xeeUL;
            this.PerformSipRounds(this.finalizationRounds);

            byte[] hash = new byte[this.HashSizeValue / 8];

            // First 64-bit output
            ulong h0 = this.v0 ^ this.v1 ^ this.v2 ^ this.v3;
            MemoryMarshal.Write(hash.AsSpan(0, 8), in h0);

            // Optional second block for SipHash-128
            if (this.HashSizeValue == 128)
            {
                this.v1 ^= 0xdd;
                this.PerformSipRounds(this.finalizationRounds);

                ulong h1 = this.v0 ^ this.v1 ^ this.v2 ^ this.v3;
                MemoryMarshal.Write(hash.AsSpan(8, 8), in h1);
            }

            return hash;
        }

        /// <summary>
        /// Rebuilds the internal SipHash state vectors from the current key whenever the key is assigned or the instance is re-initialised.
        /// </summary>
        /// <remarks>XORs the key halves with the SipHash initial constants, then applies the SipHash-128 finalisation tweak if required.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override void OnKeyChanged()
        {
            // Fix: use little-endian reads to match the SipHash specification and ProcessBlock; prior code used host-endian BitConverter, which would produce incorrect digests on big-endian hosts.
            ulong k0 = BinaryPrimitives.ReadUInt64LittleEndian(this.KeyValue.AsSpan(0));
            ulong k1 = BinaryPrimitives.ReadUInt64LittleEndian(this.KeyValue.AsSpan(8));
            this.v0 = InitialStates[0] ^ k0;
            this.v1 = InitialStates[1] ^ k1;
            this.v2 = InitialStates[2] ^ k0;
            this.v3 = InitialStates[3] ^ k1;

            if (this.HashSizeValue == 128) this.v1 ^= 0xee;
        }

        /// <summary>
        /// Performs a fixed number of SipHash compression or finalization rounds on the internal state.
        /// </summary>
        /// <param name="iterations">The number of rounds to perform.</param>
        /// <remarks>Each round consists of multiple bitwise operations and rotations defined by the SipHash specification.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PerformSipRounds(int iterations)
        {
            ulong r0 = this.v0, r1 = this.v1, r2 = this.v2, r3 = this.v3;

            for (int i = 0; i < iterations; i++)
            {
                r0 += r1;
                r1 = r1.RotateBitsLeftUnchecked(13);
                r1 ^= r0;
                r0 = r0.RotateBitsLeftUnchecked(32);
                r2 += r3;
                r3 = r3.RotateBitsLeftUnchecked(16);
                r3 ^= r2;
                r0 += r3;
                r3 = r3.RotateBitsLeftUnchecked(21);
                r3 ^= r0;
                r2 += r1;
                r1 = r1.RotateBitsLeftUnchecked(17);
                r1 ^= r2;
                r2 = r2.RotateBitsLeftUnchecked(32);
            }

            this.v0 = r0; this.v1 = r1; this.v2 = r2; this.v3 = r3;
        }

        ///// <summary>
        ///// Processes one or more blocks of input data into the SipHash internal state.
        ///// </summary>
        ///// <param name="buffer">The byte array containing the input data.</param>
        ///// <param name="offset">The byte offset in the buffer to start reading from.</param>
        ///// <param name="length">The number of bytes to process.</param>
        ///// <remarks>Handles buffering of partial blocks and invokes <see cref="ProcessBlock" /> for each complete block.</remarks>
        //private void ProcessBlocks(byte[] buffer, int offset, int length)
        //{
        //    int pos = offset;
        //    Span<byte> residualSpan = residualByteBuffer.Span;

        // Handle residual bytes from the previous call if (residualBytes > 0) { int remaining = BlockSize - residualBytes;

        // if (length >= remaining) { // Fill up the buffer and process one full block buffer.AsSpan(pos,
        // remaining).CopyTo(residualSpan[residualBytes..]); ulong block = MemoryMarshal.Read<ulong>(residualSpan); ProcessBlock(block);

        // residualBytes = 0; pos += remaining; } else { // Not enough to complete a block, just append to residuals buffer.AsSpan(pos,
        // length).CopyTo(residualSpan[residualBytes..]); residualBytes += length; return; } }

        // Process full blocks directly from the input int end = offset + length; while (pos + BlockSize <= end) { ulong block =
        // MemoryMarshal.Read<ulong>(buffer.AsSpan(pos, BlockSize)); ProcessBlock(block); pos += BlockSize; }
    }
}
