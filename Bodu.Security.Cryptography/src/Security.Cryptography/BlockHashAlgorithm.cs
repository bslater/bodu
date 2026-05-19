// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for hash algorithms that consume input in fixed-size blocks and pad the final partial block before
/// processing it (the Merkle&#8211;Damg&#229;rd shape). Handles block alignment and final-block padding orchestration
/// on behalf of derived implementations; the residual buffer, running byte total, and disposal latch are inherited from
/// <see cref="BufferedBlockHashAlgorithm{T}" />.
/// </summary>
/// <typeparam name="T">
/// The concrete hash algorithm derived from this class. Must expose a public parameterless constructor.
/// </typeparam>
/// <remarks>
/// <para>
/// Input data is accumulated into the inherited residual buffer until a complete block of
/// <see cref="BufferedBlockHashAlgorithm{T}.BlockSize" /> is available, at which point it is passed to
/// <see cref="ProcessBlock" />. Any residual bytes left over at <see cref="HashAlgorithm.HashFinal" /> are padded via
/// <see cref="PadBlock" /> before a final call to <see cref="ProcessFinalBlock" /> produces the digest.
/// </para>
/// <para>
/// Derived classes must implement the following:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="ProcessBlock" /> processes a single complete block of input data.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="PadBlock" /> pads the final input segment and encodes the total message length.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="ProcessFinalBlock" /> finalizes the hash computation and returns the resulting digest.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>When to derive from this class.</strong> Pick <see cref="BlockHashAlgorithm{T}" /> for any classic
/// Merkle–Damgård cryptographic hash — the family includes the SHA-2 hashes, Tiger, Whirlpool, Snefru, and similar
/// designs that finalize by appending a length-encoding pad to the last partial block. For the BLAKE-family pattern
/// (final-block flag, no length-encoding pad) derive from <see cref="DeferredFinalBlockHashAlgorithm{T}" /> instead.
/// For a keyed Merkle–Damgård hash (Poly1305, SipHash) derive from <see cref="KeyedBlockHashAlgorithm{T}" />, which
/// adds key handling on top of this base. For non-cryptographic block hashes (Fletcher, CRC) the parallel
/// <c>BlockNonCryptographicHashAlgorithm&lt;T&gt;</c> base in <c>Bodu.IO.Hashing</c> is the right pick — it integrates
/// with <c>NonCryptographicHashAlgorithm</c> rather than <see cref="HashAlgorithm" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Consume through a concrete derivative — the base class drives buffering and finalization.
/// using HashAlgorithm hash = new Tiger();      // 512-bit block, 192-bit digest
/// byte[] digest = hash.ComputeHash("hello"u8.ToArray());
///
/// // Or feed input incrementally via the standard streaming surface.
/// using HashAlgorithm streaming = new Tiger();
/// streaming.TransformBlock(buffer1, 0, buffer1.Length, null, 0);
/// streaming.TransformBlock(buffer2, 0, buffer2.Length, null, 0);
/// streaming.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
/// byte[] result = streaming.Hash!;
///]]>
/// </example>
/// <seealso cref="BufferedBlockHashAlgorithm{T}"/> <seealso cref="DeferredFinalBlockHashAlgorithm{T}"/>
/// <seealso cref="KeyedBlockHashAlgorithm{T}"/> <seealso cref="KeyedDeferredFinalBlockHashAlgorithm{T}"/>
public abstract class BlockHashAlgorithm<T>
    : BufferedBlockHashAlgorithm<T>
    where T : BlockHashAlgorithm<T>, new()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlockHashAlgorithm{T}" /> class using the specified input block
    /// size.
    /// </summary>
    /// <param name="blockSize">
    /// The block size, in bits, that the algorithm uses to process input data. Must be a positive multiple of 8. This
    /// value determines how data is buffered and segmented during hashing operations; the equivalent byte length is
    /// available via the inherited <see cref="BufferedBlockHashAlgorithm{T}.BlockSize" /> field.
    /// </param>
    /// <remarks>
    /// <para>
    /// The specified <paramref name="blockSize" /> defines the size of each complete block passed to the
    /// <see cref="ProcessBlock" /> method during hashing. Any input data not aligned to this size is temporarily stored
    /// in a residual buffer until enough bytes are accumulated for a full block.
    /// </para>
    /// <para>
    /// This constructor delegates to <see cref="BufferedBlockHashAlgorithm{T}" />, which allocates the residual buffer
    /// used to accumulate and align partial input segments across multiple calls to
    /// <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[], int)" /> and
    /// <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" />.
    /// </para>
    /// <para>
    /// The specified block size must match the expectations of the underlying algorithm implementation. For example, a
    /// SHA-like construction may expect 64 or 128 bytes per block.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    protected BlockHashAlgorithm(int blockSize)
        : base(blockSize)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the final padded block must be sliced into aligned blocks, or whether the full
    /// padded result may be passed as a single block (e.g., Poly1305-style).
    /// </summary>
    protected virtual bool AllowUnalignedFinalBlock => false;

    /// <summary>
    /// Processes the entirety of the input <paramref name="source" /> and feeds it into the computation pipeline. This
    /// method updates the internal hash state accordingly by consuming the entire input span.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// The hash algorithm has already been finalized and cannot accept more input data.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is part of the core hashing process and is automatically invoked by methods such as
    /// <see cref="HashAlgorithm.TransformBlock(byte[], int, int, byte[], int)" /> and
    /// <see cref="HashAlgorithm.ComputeHash(byte[])" />. It handles processing of raw byte array input and ensures the
    /// hash algorithm receives data in properly sized blocks.
    /// </para>
    /// <para>
    /// This method internally buffers incomplete blocks between calls to ensure proper alignment. Full blocks are
    /// immediately processed; any remaining bytes are stored until more data arrives or finalization occurs.
    /// </para>
    /// </remarks>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
    if (this._finalized)
        throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.Crypt_Invalid_AlreadyFinalized);
#endif

        this.ProcessBlocks(source);
    }

    /// <summary>
    /// Finalizes the hash computation by padding and processing any residual data, and returns the resulting digest.
    /// </summary>
    /// <returns>A byte array containing the final computed hash value.</returns>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// On target frameworks prior to .NET 6, the hash computation has already been finalized.
    /// </exception>
    protected override byte[] HashFinal()
    {
        this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
    if (this._finalized)
        throw new CryptographicUnexpectedOperationException(CryptoResourceStrings.Crypt_Invalid_AlreadyFinalized);
#endif

        if (this.ShouldPadFinalBlock())
        {
            var finalBlock = this.PadBlock(this._residualBlock.Span[..this._residualBytes], this._totalBytes);

            if (this.AllowUnalignedFinalBlock)
            {
                this.ProcessBlock(finalBlock);
            }
            else
            {
                var blockBytes = this.BlockSize / 8;
                for (var i = 0; i < finalBlock.Length; i += blockBytes)
                    this.ProcessBlock(finalBlock.AsSpan(i, blockBytes));
            }
        }
        else if (this._residualBytes > 0)
        {
            this.ProcessBlock(this._residualBlock.Span[..this._residualBytes]);
        }

        return this.ProcessFinalBlock();
    }

    /// <summary>
    /// Pads the final partial block of input data and appends the encoded total message length.This ensures that all
    /// input is padded and aligned to the block size required by the algorithm, often with trailing zeroes and encoded
    /// length information.
    /// </summary>
    /// <param name="block">The final block of unprocessed input, typically containing 0 to BlockSize–1 bytes.</param>
    /// <param name="messageLength">
    /// The total number of bytes processed by the algorithm before padding, not including this block.
    /// </param>
    /// <returns>
    /// A padded byte array consisting of one or more full blocks that include the input data and message length
    /// encoding, ready to be passed to <see cref="ProcessBlock(ReadOnlySpan{byte})" />.
    /// </returns>
    /// <remarks>
    /// The returned array must be aligned to the algorithm’s block size. Padding schemes often include a leading '1'
    /// bit, followed by zero bytes, and end with a length field (e.g., as in Merkle–Damgård construction).
    /// </remarks>
    protected abstract byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength);

    /// <summary>
    /// Transforms a complete block of input data and updates the internal hash state.
    /// </summary>
    /// <param name="block">
    /// The input block to process. Its length must match the algorithm's configured block size.
    /// </param>
    /// <remarks>
    /// This method performs the core transformation logic of the hash algorithm. It is called repeatedly with aligned
    /// input blocks and is not responsible for padding or finalization steps.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="block" /> is not the expected size.
    /// </exception>
    protected abstract void ProcessBlock(ReadOnlySpan<byte> block);

    /// <summary>
    /// Finalizes the hash computation and produces the final hash output.
    /// </summary>
    /// <returns>A byte array containing the final computed hash value.</returns>
    /// <remarks>
    /// This method is invoked after all input has been processed and padded. It reads from the internal hash state and
    /// serializes the result to a byte array in the format expected by consumers of the algorithm (e.g., big-endian or
    /// little-endian).
    /// </remarks>
    protected abstract byte[] ProcessFinalBlock();

    /// <summary>
    /// Determines whether the final block of input data should be padded before processing.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the final block should be padded; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This method is used to decide whether padding is required for the final block of input data. Derived classes can
    /// override this method to implement their own logic for padding behavior. By default, this method returns
    /// <see langword="true" />, indicating that padding is required.
    /// </remarks>
    protected virtual bool ShouldPadFinalBlock() => true;

    /// <summary>
    /// Processes a span of input bytes in fixed-size blocks, handling any residual bytes from previous invocations.
    /// </summary>
    /// <param name="buffer">The input span to process. May include incomplete blocks.</param>
    /// <remarks>
    /// This method handles leftover bytes from previous calls and ensures full blocks are processed immediately. Any
    /// incomplete tail data is buffered until more input is received.
    /// </remarks>
    private void ProcessBlocks(ReadOnlySpan<byte> buffer)
    {
        var pos = 0;
        var blockBytes = this.BlockSize / 8;
        this._totalBytes += (ulong)buffer.Length;

        Span<byte> residualSpan = this._residualBlock.Span;

        // Attempt to fill a partial residual block if it exists
        if (this._residualBytes > 0)
        {
            var remaining = blockBytes - this._residualBytes;

            if (buffer.Length >= remaining)
            {
                // Complete residual block and process it
                buffer.Slice(pos, remaining).CopyTo(residualSpan[this._residualBytes..]);
                this.ProcessBlock(this._residualBlock.Span);
                this._residualBytes = 0;
                pos += remaining;
            }
            else
            {
                // Not enough to complete a block, buffer it for later
                buffer.CopyTo(residualSpan[this._residualBytes..]);
                this._residualBytes += buffer.Length;
                return;
            }
        }

        // Process complete blocks from input span
        while (pos + blockBytes <= buffer.Length)
        {
            this.ProcessBlock(buffer.Slice(pos, blockBytes));
            pos += blockBytes;
        }

        // Buffer any trailing bytes that form an incomplete block
        this._residualBytes = buffer.Length - pos;
        if (this._residualBytes > 0)
            buffer.Slice(pos, this._residualBytes).CopyTo(residualSpan);
    }
}
