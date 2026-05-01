// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DeferredFinalBlockHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu;
using Bodu.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for hash algorithms that defer compression of the final full block until <see cref="HashAlgorithm.HashFinal" />
/// so that a finalisation flag may be raised on the last compression call (the Blake-family shape). Owns the
/// defer-on-full-block buffering loop and the zero-pad-then-finalise orchestration; the residual buffer, running
/// byte counter, and disposal latch are inherited from <see cref="BufferedBlockHashAlgorithm{T}" />.
/// </summary>
/// <typeparam name="T">The concrete hash algorithm derived from this class. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// Unlike Merkle&#8211;Damg&#229;rd hashes, Blake-family compression functions take a per-block byte counter and an
/// <c>isFinal</c> flag. The final compression call must carry both the actual total byte count of the message and
/// <c>isFinal: true</c>. This base class implements the consequent invariant: a block that fills the residual buffer
/// exactly is <em>not</em> compressed at the point it is filled, because the algorithm cannot yet tell whether the
/// caller will supply more data. Compression of that block is deferred until either (a) the next
/// <see cref="HashAlgorithm.HashCore(ReadOnlySpan{byte})" /> call provides at least one more byte (in which case the
/// pending block is compressed with <c>isFinal: false</c>) or (b) <see cref="HashAlgorithm.HashFinal" /> is called
/// (in which case the pending block is compressed with <c>isFinal: true</c>).
/// </para>
/// <para>
/// The inherited <see cref="BufferedBlockHashAlgorithm{T}._totalBytes" /> field stores the total number of bytes
/// already <em>compressed</em> (i.e. consumed from the residual buffer by previous <see cref="ProcessBlock" />
/// calls). Bytes still held in the residual buffer are <em>not</em> included in this total &#8212; they contribute to
/// the counter only at the moment they are compressed.
/// </para>
/// <para>Derived classes must implement the following:</para>
/// <list type="bullet">
/// <item><description><see cref="BufferedBlockHashAlgorithm{T}.OnInitialize" /> resets the algorithm-specific chaining variables to their initial state.</description></item>
/// <item><description><see cref="ProcessBlock" /> compresses a single block, receiving the per-block counter and the finalisation flag.</description></item>
/// <item><description><see cref="ProcessFinalBlock" /> extracts the digest from the chaining variables after the final compression has run.</description></item>
/// </list>
/// <para>
/// <strong>When to derive from this class.</strong> Pick <see cref="DeferredFinalBlockHashAlgorithm{T}"/> for
/// the BLAKE family and any other algorithm whose compression function takes an explicit
/// "is this the final block?" flag rather than padding the trailing partial block with a length encoding —
/// <see cref="Blake3"/> is the canonical user. For BLAKE2-style hashes that also accept an optional secret
/// key (<see cref="Blake2b"/>, <see cref="Blake2s"/>) derive from
/// <see cref="KeyedDeferredFinalBlockHashAlgorithm{T}"/>, which adds RFC 7693 key-block handling on top of
/// this base. For Merkle–Damgård hashes (SHA-2, Tiger, Whirlpool) use <see cref="BlockHashAlgorithm{T}"/>.
/// </para>
/// </remarks>
/// <seealso cref="BufferedBlockHashAlgorithm{T}"/>
/// <seealso cref="BlockHashAlgorithm{T}"/>
/// <seealso cref="KeyedBlockHashAlgorithm{T}"/>
/// <seealso cref="KeyedDeferredFinalBlockHashAlgorithm{T}"/>
public abstract class DeferredFinalBlockHashAlgorithm<T>
    : BufferedBlockHashAlgorithm<T>
    where T : DeferredFinalBlockHashAlgorithm<T>, new()
{
    /// <summary>
    /// Initialises a new instance of the <see cref="DeferredFinalBlockHashAlgorithm{T}" /> class with the specified
    /// input block size.
    /// </summary>
    /// <param name="blockSize">
    /// The fixed size, in bytes, of each block consumed by the algorithm. Must be greater than zero.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="blockSize" /> is less than or equal to zero.
    /// </exception>
    protected DeferredFinalBlockHashAlgorithm(int blockSize)
        : base(blockSize)
    {
    }

    /// <summary>
    /// Consumes the supplied input span using the deferred-final-block buffering rule. At the start of every iteration
    /// of the inner loop, if the residual buffer is already full and the caller has supplied at least one more byte,
    /// the held block is compressed with <c>isFinal: false</c> and the counter is advanced before the new byte is
    /// copied in. Compression of a block that fills the buffer exactly is otherwise deferred to
    /// <see cref="HashFinal" /> so that <c>isFinal: true</c> may be raised on it.
    /// </summary>
    /// <param name="source">The input bytes to consume. May be empty, partial, exact-block, or multi-block in length.</param>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// On target frameworks prior to .NET 6, the hash algorithm has already been finalised and cannot accept more input.
    /// </exception>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
    if (this._finalized)
        throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

        int pos = 0;
        int remaining = source.Length;
        Span<byte> residualSpan = this._residualBlock.Span;

        while (remaining > 0)
        {
            // The block that fills the residual buffer must not be compressed at fill time, because it may turn out
            // to be the final block and thus require isFinal: true. Compress it only when we know more data follows.
            if (this._residualBytes == this.BlockSizeBytes)
            {
                this.ProcessBlock(residualSpan, this._totalBytes + (ulong)this.BlockSizeBytes, isFinal: false);
                this._totalBytes += (ulong)this.BlockSizeBytes;
                this._residualBytes = 0;
            }

            int canCopy = Math.Min(this.BlockSizeBytes - this._residualBytes, remaining);
            source.Slice(pos, canCopy).CopyTo(residualSpan[this._residualBytes..]);
            this._residualBytes += canCopy;
            pos += canCopy;
            remaining -= canCopy;
        }
    }

    /// <summary>
    /// Finalises the hash computation. Zero-pads the residual buffer (if not already full) up to the block size,
    /// performs the final compression with <c>isFinal: true</c> and a counter equal to the total bytes consumed
    /// (<see cref="BufferedBlockHashAlgorithm{T}._totalBytes" /> plus the residual byte count), and returns the
    /// digest produced by <see cref="ProcessFinalBlock" />.
    /// </summary>
    /// <returns>The computed hash bytes as produced by <see cref="ProcessFinalBlock" />.</returns>
    /// <exception cref="ObjectDisposedException">The algorithm instance has been disposed.</exception>
    /// <exception cref="CryptographicUnexpectedOperationException">
    /// On target frameworks prior to .NET 6, the hash computation has already been finalised.
    /// </exception>
    protected override byte[] HashFinal()
    {
        this.ThrowIfDisposed();

#if !NET6_0_OR_GREATER
    if (this._finalized)
        throw new CryptographicUnexpectedOperationException(ResourceStrings.CryptographicException_AlreadyFinalized);
#endif

        Span<byte> residualSpan = this._residualBlock.Span;

        if (this._residualBytes < this.BlockSizeBytes)
            residualSpan[this._residualBytes..].Clear();

        ulong counter = this._totalBytes + (ulong)this._residualBytes;
        this.ProcessBlock(residualSpan, counter, isFinal: true);

        return this.ProcessFinalBlock();
    }

    /// <summary>
    /// Compresses a single full block of input using the algorithm's internal compression function.
    /// </summary>
    /// <param name="block">
    /// The input block to compress. Always exactly <see cref="BufferedBlockHashAlgorithm{T}.BlockSizeBytes" /> bytes
    /// long, possibly zero-padded by <see cref="HashFinal" /> when <paramref name="isFinal" /> is <see langword="true" />.
    /// </param>
    /// <param name="totalBytesIncludingThisBlock">
    /// The cumulative byte count <em>including</em> the bytes in <paramref name="block" /> being compressed. For
    /// mid-stream blocks this equals the previous total plus
    /// <see cref="BufferedBlockHashAlgorithm{T}.BlockSizeBytes" />; for the final compression it equals the previous
    /// total plus the residual byte count (which may be in the range <c>[0, BlockSizeBytes]</c>).
    /// </param>
    /// <param name="isFinal">
    /// <see langword="true" /> for the last compression call (raised by <see cref="HashFinal" />); otherwise
    /// <see langword="false" />. Algorithms that maintain a finalisation flag should consult this parameter rather
    /// than tracking message length internally.
    /// </param>
    /// <remarks>
    /// Implementations must not retain a reference to the supplied span beyond the call &#8212; the underlying buffer
    /// is the inherited residual buffer and may be overwritten by subsequent input.
    /// </remarks>
    protected abstract void ProcessBlock(ReadOnlySpan<byte> block, ulong totalBytesIncludingThisBlock, bool isFinal);

    /// <summary>
    /// Extracts the digest from the algorithm's chaining variables after <see cref="ProcessBlock" /> has been called
    /// with <c>isFinal: true</c> for the last time.
    /// </summary>
    /// <returns>A byte array containing the final computed hash value.</returns>
    /// <remarks>
    /// This method is invoked once per <see cref="HashAlgorithm.HashFinal" /> call, immediately after the final
    /// compression. It reads from the internal hash state and serialises the result to a byte array in the format
    /// expected by consumers of the algorithm (typically little-endian for Blake-family hashes).
    /// </remarks>
    protected abstract byte[] ProcessFinalBlock();
}
