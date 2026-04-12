using Bodu;
using Bodu.Security.Cryptography;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes the hash for the input data using a block-oriented hash algorithm. This implementation processes input in fixed-size chunks
    /// and handles residual buffering, alignment, and final block padding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="BlockHashAlgorithm{T}" /> is designed for hash algorithms that consume input data in uniformly sized blocks. It
    /// automatically handles residual data between calls, ensuring only full blocks are passed to the underlying algorithm for processing.
    /// </para>
    /// <para>
    /// Input data is accumulated into an internal buffer until a complete block of <see cref="BlockSizeBytes" /> is available. Once filled,
    /// the buffer is passed to the abstract <see cref="ProcessBlock" /> method for algorithm-specific transformation. Any remaining bytes
    /// are preserved across calls and finalized using <see cref="PadBlock" /> during <see cref="HashAlgorithm.HashFinal" />.
    /// </para>
    /// <para>Implementing classes must override the following abstract methods:</para>
    /// <list type="bullet">
    /// <item><see cref="ProcessBlock" /> – Processes a single complete block of input data.</item>
    /// <item><see cref="PadBlock" /> – Pads the final input segment and encodes total message length.</item>
    /// <item><see cref="ProcessFinalBlock" /> – Finalizes the hash computation and returns the final hash value.</item>
    /// </list>
    /// <para>
    /// This class provides compatibility with both span-based and byte-array-based input pipelines, allowing integration with streaming
    /// data, cryptographic transforms, and legacy interfaces.
    /// </para>
    /// </remarks>
    public abstract class BlockHashAlgorithm<T>
        : HashAlgorithm
        where T : BlockHashAlgorithm<T>, new()
    {
        /// <summary>
        /// The fixed size, in bytes, of each block processed by the algorithm.
        /// </summary>
        protected readonly int BlockSizeBytes; // Size of a single block (in bytes) to be processed per hash step

        private readonly Memory<byte> residualByteBuffer; // Temporary buffer to hold incomplete blocks between HashCore calls
        private bool disposed;
        private int residualBytes;                        // Number of bytes currently in the residual buffer
        private ulong totalLength;                        // Total length of data processed, used for padding

        // Tracks whether Dispose() has been called

#if !NET6_0_OR_GREATER

    /// <summary>
    /// Indicates whether the hash computation has been finalized. Used in .NET Standard environments.
    /// </summary>
    protected bool this.finalized;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockHashAlgorithm{T}" /> class using the specified input block size.
        /// </summary>
        /// <param name="blockSize">
        /// The block size, in bytes, that the algorithm uses to process input data. This value determines how data is buffered and
        /// segmented during hashing operations.
        /// </param>
        /// <remarks>
        /// <para>
        /// The specified <paramref name="blockSize" /> defines the size of each complete block passed to the <see cref="ProcessBlock" />
        /// method during hashing. Any input data not aligned to this size is temporarily stored in a residual buffer until enough bytes are
        /// accumulated for a full block.
        /// </para>
        /// <para>
        /// This constructor allocates the internal buffer used to accumulate and align partial input segments across multiple calls to
        /// <see cref="HashAlgorithm.TransformBlock" /> and <see cref="HashAlgorithm.TransformFinalBlock" />.
        /// </para>
        /// <para>
        /// The specified block size must match the expectations of the underlying algorithm implementation. For example, a SHA-like
        /// construction may expect 64 or 128 bytes per block.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="blockSize" /> is less than or equal to zero.</exception>
        protected BlockHashAlgorithm(int blockSize)
        {
            ThrowHelper.ThrowIfLessThanOrEqual(blockSize, 0);
            BlockSizeBytes = blockSize;
            this.residualByteBuffer = new byte[BlockSizeBytes];
        }

        /// <summary>
        /// Gets a value indicating whether the final padded block must be sliced into aligned blocks, or whether the full padded result may
        /// be passed as a single block (e.g., Poly1305-style).
        /// </summary>
        protected virtual bool AllowUnalignedFinalBlock => false;

        /// <inheritdoc />
        public override void Initialize()
        {
            // Clear internal buffers and reset state
            this.residualByteBuffer.Span.Clear();
            this.residualBytes = 0;
            this.totalLength = 0;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) return;

            if (disposing)
            {
                CryptoHelpers.Clear(this.residualByteBuffer);
                this.residualBytes = 0;
                this.totalLength = 0;
            }

            this.disposed = true;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Processes a segment of the input byte array and feeds it into the hash computation pipeline. This method updates the internal
        /// state of the algorithm by processing <paramref name="cbSize" /> bytes starting at the specified <paramref name="ibStart" /> offset.
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
        /// <remarks>
        /// <para>
        /// This method is part of the core hashing process and is automatically invoked by methods such as
        /// <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[], int)" /> and <see cref="HashAlgorithm.ComputeHash(byte[])" />.
        /// It handles processing of raw byte array input and ensures the hash algorithm receives data in properly sized blocks.
        /// </para>
        /// <para>
        /// This method internally buffers incomplete blocks between calls to ensure proper alignment. Full blocks are immediately
        /// processed; any remaining bytes are stored until more data arrives or finalization occurs.
        /// </para>
        /// </remarks>
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            ThrowHelper.ThrowIfNull(array);
            this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
        ThrowHelper.ThrowIfLessThan(ibStart, 0);
        ThrowHelper.ThrowIfLessThan(cbSize, 0);
        ThrowHelper.ThrowIfArrayLengthIsInsufficient(array, ibStart, cbSize);
        if (this.finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            ProcessBlocks(array.AsSpan(ibStart, cbSize));
        }

        /// <summary>
        /// Processes the entirety of the input <paramref name="source" /> and feeds it into the computation pipeline. This method updates
        /// the internal hash state accordingly by consuming the entire input span.
        /// </summary>
        /// <param name="source">The input byte span containing the data to hash.</param>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// The hash algorithm has already been finalized and cannot accept more input data.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method is part of the core hashing process and is automatically invoked by methods such as
        /// <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[], int)" /> and <see cref="HashAlgorithm.ComputeHash(byte[])" />.
        /// It handles processing of raw byte array input and ensures the hash algorithm receives data in properly sized blocks.
        /// </para>
        /// <para>
        /// This method internally buffers incomplete blocks between calls to ensure proper alignment. Full blocks are immediately
        /// processed; any remaining bytes are stored until more data arrives or finalization occurs.
        /// </para>
        /// </remarks>
        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
        if (this.finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            ProcessBlocks(source);
        }

        /// <summary>
        /// Finalizes the hash computation and returns the resulting hash value. This method reflects all input previously processed via
        /// <see cref="HashAlgorithm.HashCore(byte[], int, int)" /> or <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})" /> and
        /// produces a final, stable hash output.
        /// </summary>
        /// <returns>
        /// A byte array representing the final computed hash value. The result is encoded in the platform’s native byte order unless
        /// explicitly converted to big-endian or little-endian as required by the algorithm.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method completes the internal state of the hashing algorithm and serializes the final hash value into a
        /// platform-independent format. It is invoked automatically by <see cref="HashAlgorithm.ComputeHash(byte[])" /> and related methods
        /// once all data has been processed.
        /// </para>
        /// <para>
        /// After this method returns, the internal state is considered finalized, and the computed hash value is stable and ready for use.
        /// </para>
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
        if (this.finalized)
            throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

            if (ShouldPadFinalBlock())
            {
                var finalBlock = PadBlock(this.residualByteBuffer.Span.Slice(0, this.residualBytes), this.totalLength);

                if (AllowUnalignedFinalBlock)
                {
                    ProcessBlock(finalBlock);
                }
                else
                {
                    for (int i = 0; i < finalBlock.Length; i += BlockSizeBytes)
                        ProcessBlock(finalBlock.AsSpan(i, BlockSizeBytes));
                }
            }
            else if (this.residualBytes > 0)
            {
                ProcessBlock(this.residualByteBuffer.Span.Slice(0, this.residualBytes));
            }

            return ProcessFinalBlock();
        }

        /// <summary>
        /// Pads the final partial block of input data and appends the encoded total message length.This ensures that all input is padded
        /// and aligned to the block size required by the algorithm, often with trailing zeroes and encoded length information.
        /// </summary>
        /// <param name="block">The final block of unprocessed input, typically containing 0 to BlockSize–1 bytes.</param>
        /// <param name="messageLength">The total number of bytes processed by the algorithm before padding, not including this block.</param>
        /// <returns>
        /// A padded byte array consisting of one or more full blocks that include the input data and message length encoding, ready to be
        /// passed to <see cref="ProcessBlock(ReadOnlySpan{byte})" />.
        /// </returns>
        /// <remarks>
        /// The returned array must be aligned to the algorithm’s block size. Padding schemes often include a leading '1' bit, followed by
        /// zero bytes, and end with a length field (e.g., as in Merkle–Damgård construction).
        /// </remarks>
        protected abstract byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength);

        /// <summary>
        /// Transforms a complete block of input data and updates the internal hash state.
        /// </summary>
        /// <param name="block">The input block to process. Its length must match the algorithm's configured block size.</param>
        /// <remarks>
        /// This method performs the core transformation logic of the hash algorithm. It is called repeatedly with aligned input blocks and
        /// is not responsible for padding or finalization steps.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the <paramref name="block" /> is not the expected size.</exception>
        protected abstract void ProcessBlock(ReadOnlySpan<byte> block);

        /// <summary>
        /// Finalizes the hash computation and produces the final hash output.
        /// </summary>
        /// <returns>A byte array containing the final computed hash value.</returns>
        /// <remarks>
        /// This method is invoked after all input has been processed and padded. It reads from the internal hash state and serializes the
        /// result to a byte array in the format expected by consumers of the algorithm (e.g., big-endian or little-endian).
        /// </remarks>
        protected abstract byte[] ProcessFinalBlock();

        /// <summary>
        /// Determines whether the final block of input data should be padded before processing.
        /// </summary>
        /// <returns><see langword="true" /> if the final block should be padded; otherwise, <see langword="false" />.</returns>
        /// <remarks>
        /// This method is used to decide whether padding is required for the final block of input data. Derived classes can override this
        /// method to implement their own logic for padding behavior. By default, this method returns <see langword="true" />, indicating
        /// that padding is required.
        /// </remarks>
        protected virtual bool ShouldPadFinalBlock() => true;

        /// <summary>
        /// Throws an exception if the instance has been disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has already been disposed.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfDisposed()
        {
#if NET8_0_OR_GREATER
            ObjectDisposedException.ThrowIf(this.disposed, this);
#else
        if (disposed)
            throw new ObjectDisposedException(nameof(T));
#endif
        }

        /// <summary>
        /// Throws if the algorithm has begun processing and can no longer be reconfigured.
        /// </summary>
        /// <exception cref="CryptographicUnexpectedOperationException">
        /// Thrown if an attempt is made to change configuration after the algorithm has started.
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ThrowIfInvalidState()
        {
            if (State != 0)
                throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_ReconfigurationNotAllowed);
        }

        /// <summary>
        /// Processes a span of input bytes in fixed-size blocks, handling any residual bytes from previous invocations.
        /// </summary>
        /// <param name="buffer">The input span to process. May include incomplete blocks.</param>
        /// <remarks>
        /// This method handles leftover bytes from previous calls and ensures full blocks are processed immediately. Any incomplete tail
        /// data is buffered until more input is received.
        /// </remarks>
        private void ProcessBlocks(ReadOnlySpan<byte> buffer)
        {
            int pos = 0;
            this.totalLength += (ulong)buffer.Length;

            Span<byte> residualSpan = this.residualByteBuffer.Span;

            // Attempt to fill a partial residual block if it exists
            if (this.residualBytes > 0)
            {
                int remaining = BlockSizeBytes - this.residualBytes;

                if (buffer.Length >= remaining)
                {
                    // Complete residual block and process it
                    buffer.Slice(pos, remaining).CopyTo(residualSpan[this.residualBytes..]);
                    ProcessBlock(this.residualByteBuffer.Span);
                    this.residualBytes = 0;
                    pos += remaining;
                }
                else
                {
                    // Not enough to complete a block, buffer it for later
                    buffer.CopyTo(residualSpan[this.residualBytes..]);
                    this.residualBytes += buffer.Length;
                    return;
                }
            }

            // Process complete blocks from input span
            while (pos + BlockSizeBytes <= buffer.Length)
            {
                ProcessBlock(buffer.Slice(pos, BlockSizeBytes));
                pos += BlockSizeBytes;
            }

            // Buffer any trailing bytes that form an incomplete block
            this.residualBytes = buffer.Length - pos;
            if (this.residualBytes > 0)
                buffer.Slice(pos, this.residualBytes).CopyTo(residualSpan);
        }
    }
}