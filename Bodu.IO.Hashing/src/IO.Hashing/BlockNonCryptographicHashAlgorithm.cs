// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System;
using System.IO.Hashing;
using Bodu;

/// <summary>
/// Base class for non-cryptographic hash algorithms that consume input in fixed-size blocks. Handles residual buffering,
/// block alignment, total-length tracking, and optional final-block padding on behalf of derived implementations.
/// </summary>
/// <typeparam name="T">The concrete hash algorithm derived from this class. Must expose a public parameterless constructor.</typeparam>
/// <remarks>
/// <para>
/// Input data is accumulated into an internal buffer until a complete block of <see cref="BlockSizeBytes" /> is available,
/// at which point it is passed to <see cref="ProcessBlock" />. Any residual bytes left over when
/// <see cref="GetCurrentHashCore" /> is invoked are padded via <see cref="PadBlock" /> before a final call to
/// <see cref="ProcessFinalBlock" /> produces the digest.
/// </para>
/// <para>Derived classes must implement the following:</para>
/// <list type="bullet">
/// <item><description><see cref="ProcessBlock" /> processes a single complete block of input data.</description></item>
/// <item><description><see cref="PadBlock" /> pads the final input segment and encodes the total message length.</description></item>
/// <item><description><see cref="ProcessFinalBlock" /> finalises the hash computation and returns the resulting digest.</description></item>
/// </list>
/// <para>
/// Unlike cryptographic hashes derived from <see cref="System.Security.Cryptography.HashAlgorithm" />, instances of
/// <see cref="BlockNonCryptographicHashAlgorithm{T}" /> do not participate in <see cref="System.Security.Cryptography.ICryptoTransform" />
/// pipelines. They are intended for integrity checks, fingerprinting, and hash-table workloads — not for security-sensitive contexts.
/// </para>
/// </remarks>
public abstract class BlockNonCryptographicHashAlgorithm<T>
    : NonCryptographicHashAlgorithm
    where T : BlockNonCryptographicHashAlgorithm<T>, new()
{
    /// <summary>
    /// The fixed size, in bytes, of each block processed by the algorithm.
    /// </summary>
    protected readonly int BlockSizeBytes;

    private readonly byte[] _residualByteBuffer;
    private int _residualBytes;
    private ulong _totalLength;

    /// <summary>
    /// Initialises a new instance of the <see cref="BlockNonCryptographicHashAlgorithm{T}" /> class using the specified
    /// output size and block size.
    /// </summary>
    /// <param name="hashLengthInBytes">The length, in bytes, of the hash produced by this algorithm. Must be greater than zero.</param>
    /// <param name="blockSize">The block size, in bytes, that the algorithm uses to process input data. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashLengthInBytes" /> ≤ 0, or <paramref name="blockSize" /> ≤ 0.
    /// </exception>
    protected BlockNonCryptographicHashAlgorithm(int hashLengthInBytes, int blockSize)
        : base(hashLengthInBytes)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(blockSize, 0);
        this.BlockSizeBytes = blockSize;
        this._residualByteBuffer = new byte[blockSize];
    }

    /// <summary>
    /// Gets a value indicating whether the final padded block must be sliced into aligned blocks, or whether the full
    /// padded result may be passed as a single block.
    /// </summary>
    protected virtual bool AllowUnalignedFinalBlock => false;

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source)
    {
        this.ProcessBlocks(source);
    }

    /// <inheritdoc />
    public override void Reset()
    {
        Array.Clear(this._residualByteBuffer, 0, this._residualByteBuffer.Length);
        this._residualBytes = 0;
        this._totalLength = 0;
        this.ResetState();
    }

    /// <summary>
    /// Resets the derived algorithm's internal accumulators. Called from <see cref="Reset" /> after base-class state is cleared.
    /// </summary>
    /// <remarks>
    /// Derived types override this to restore any running state they own (for example, partial sums, index counters).
    /// The default implementation does nothing.
    /// </remarks>
    protected virtual void ResetState()
    {
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        // Snapshot current accumulator state so that GetCurrentHash() remains non-destructive:
        // pad/final-block processing is performed on a cloned instance, and its digest is copied to destination.
        T snapshot = this.Clone();

        if (snapshot.ShouldPadFinalBlock())
        {
            byte[] finalBlock = snapshot.PadBlock(
                new ReadOnlySpan<byte>(snapshot._residualByteBuffer, 0, snapshot._residualBytes),
                snapshot._totalLength);

            if (snapshot.AllowUnalignedFinalBlock)
            {
                snapshot.ProcessBlock(finalBlock);
            }
            else
            {
                for (int i = 0; i < finalBlock.Length; i += snapshot.BlockSizeBytes)
                    snapshot.ProcessBlock(finalBlock.AsSpan(i, snapshot.BlockSizeBytes));
            }
        }
        else if (snapshot._residualBytes > 0)
        {
            snapshot.ProcessBlock(new ReadOnlySpan<byte>(snapshot._residualByteBuffer, 0, snapshot._residualBytes));
        }

        byte[] digest = snapshot.ProcessFinalBlock();
        digest.AsSpan(0, this.HashLengthInBytes).CopyTo(destination);
    }

    /// <summary>
    /// Creates a deep copy of the current algorithm instance, preserving accumulator state. Used by
    /// <see cref="GetCurrentHashCore" /> so that retrieving an intermediate hash does not disturb ongoing computation.
    /// </summary>
    /// <returns>A new instance with the same internal state as the current one.</returns>
    protected abstract T Clone();

    /// <summary>
    /// Pads the final partial block of input data and appends the encoded total message length. This ensures that all input
    /// is padded and aligned to the block size required by the algorithm.
    /// </summary>
    /// <param name="block">The final block of unprocessed input, typically containing 0 to <see cref="BlockSizeBytes" />-1 bytes.</param>
    /// <param name="messageLength">The total number of bytes processed by the algorithm before padding, not including this block.</param>
    /// <returns>
    /// A padded byte array consisting of one or more full blocks that include the input data and message-length encoding,
    /// ready to be passed to <see cref="ProcessBlock(ReadOnlySpan{byte})" />.
    /// </returns>
    protected abstract byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength);

    /// <summary>
    /// Transforms a complete block of input data and updates the internal hash state.
    /// </summary>
    /// <param name="block">The input block to process. Its length must match the algorithm's configured block size.</param>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="block" /> is not the expected size.</exception>
    protected abstract void ProcessBlock(ReadOnlySpan<byte> block);

    /// <summary>
    /// Finalises the hash computation and produces the final hash output.
    /// </summary>
    /// <returns>A byte array containing the final computed hash value.</returns>
    protected abstract byte[] ProcessFinalBlock();

    /// <summary>
    /// Determines whether the final block of input data should be padded before processing.
    /// </summary>
    /// <returns><see langword="true" /> if the final block should be padded; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// By default this method returns <see langword="true" />. Derived classes can override to indicate that trailing
    /// residual bytes should be processed verbatim without explicit padding.
    /// </remarks>
    protected virtual bool ShouldPadFinalBlock() => true;

    /// <summary>
    /// Gets the number of residual bytes currently buffered but not yet processed.
    /// </summary>
    /// <remarks>Exposed to derived types that snapshot state in <see cref="Clone" />.</remarks>
    protected int ResidualByteCount => this._residualBytes;

    /// <summary>
    /// Gets a read-only view over the residual byte buffer.
    /// </summary>
    /// <remarks>Exposed to derived types that snapshot state in <see cref="Clone" />.</remarks>
    protected ReadOnlySpan<byte> ResidualBytes =>
        new(this._residualByteBuffer, 0, this._residualBytes);

    /// <summary>
    /// Gets the total number of bytes that have been passed to <see cref="Append(ReadOnlySpan{byte})" /> since the
    /// last call to <see cref="Reset" />.
    /// </summary>
    /// <remarks>Exposed to derived types that snapshot state in <see cref="Clone" />.</remarks>
    protected ulong TotalLength => this._totalLength;

    /// <summary>
    /// Copies the caller's residual-buffer state onto this instance. Used by <see cref="Clone" /> implementations in
    /// derived types to duplicate the running input-alignment state.
    /// </summary>
    /// <param name="source">The algorithm instance whose residual state should be copied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    protected void CopyResidualStateFrom(BlockNonCryptographicHashAlgorithm<T> source)
    {
        ThrowHelper.ThrowIfNull(source);
        source._residualByteBuffer.AsSpan(0, source._residualBytes).CopyTo(this._residualByteBuffer);
        this._residualBytes = source._residualBytes;
        this._totalLength = source._totalLength;
    }

    /// <summary>
    /// Accumulates <paramref name="buffer" /> into the residual buffer and drains complete blocks
    /// to <c>ProcessFullBlock</c>, preserving any incomplete trailing block for a subsequent call.
    /// </summary>
    /// <param name="buffer">The input bytes to feed into the hash.</param>
    private void ProcessBlocks(ReadOnlySpan<byte> buffer)
    {
        int pos = 0;
        this._totalLength += (ulong)buffer.Length;

        Span<byte> residualSpan = this._residualByteBuffer;

        if (this._residualBytes > 0)
        {
            int remaining = this.BlockSizeBytes - this._residualBytes;

            if (buffer.Length >= remaining)
            {
                buffer.Slice(0, remaining).CopyTo(residualSpan[this._residualBytes..]);
                this.ProcessBlock(this._residualByteBuffer);
                this._residualBytes = 0;
                pos += remaining;
            }
            else
            {
                buffer.CopyTo(residualSpan[this._residualBytes..]);
                this._residualBytes += buffer.Length;
                return;
            }
        }

        while (pos + this.BlockSizeBytes <= buffer.Length)
        {
            this.ProcessBlock(buffer.Slice(pos, this.BlockSizeBytes));
            pos += this.BlockSizeBytes;
        }

        this._residualBytes = buffer.Length - pos;
        if (this._residualBytes > 0)
            buffer.Slice(pos, this._residualBytes).CopyTo(residualSpan);
    }
}
