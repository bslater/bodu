// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockNonCryptographicHashAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

/// <summary>
/// Base class for non-cryptographic hash algorithms whose internal state advances one fixed-size block at a time —
/// handles residual buffering, block alignment, total-length tracking, snapshot-based <c>GetCurrentHash</c>, and
/// optional final-block padding so that derived implementations only need to express the per-block compression step.
/// </summary>
/// <typeparam name="T">
/// The concrete hash algorithm type derived from this class. Must expose a public parameterless constructor so the base
/// class can satisfy its <c>new()</c> constraint when constructing snapshot clones.
/// </typeparam>
/// <remarks>
/// <para>
/// Many non-cryptographic hashes — Murmur, CityHash, Pearson, the FNV variants — define their compression step over a
/// fixed block size (4, 8, 16, or 32 bytes) and must buffer trailing bytes that do not fill a complete block. Writing
/// that buffering loop correctly is fiddly: handle straddling input, accumulate the running message length, and pad
/// once on finalization. <see cref="BlockNonCryptographicHashAlgorithm{T}" /> centralizes that machinery so derived
/// types only express the algorithm-specific behavior.
/// </para>
/// <para>
/// <strong>Inheritance contract.</strong> Derived classes implement four members and may override two more:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="ProcessBlock(System.ReadOnlySpan{byte})" /> — compress a single complete block.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="PadBlock(System.ReadOnlySpan{byte}, ulong)" /> — pad the final partial block and encode the total message
/// length.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="ProcessFinalBlock" /> — emit the final digest from the accumulator.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Clone" /> — produce a state-equivalent copy used for non-destructive snapshotting.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="ResetState" /> (optional) — restore algorithm-specific accumulators on <see cref="Reset" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="ShouldPadFinalBlock" /> / <see cref="AllowUnalignedFinalBlock" /> (optional) — opt out of padding, or
/// pass the padded result as a single block instead of splitting it block-aligned.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Lifecycle.</strong> Input arrives via the standard
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" /> entry point and is
/// drained block-by-block into the residual buffer.
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> is intentionally non-destructive:
/// the base class clones the live instance via <see cref="Clone" />, runs padding and finalization on the clone, and
/// copies the digest into the destination span. Callers may inspect the running hash as often as they like without
/// disturbing further input. <see cref="Reset" /> clears the residual buffer and total length before invoking
/// <see cref="ResetState" />.
/// </para>
/// <para>
/// <strong>Snapshot helpers for derived <see cref="Clone" /> implementations.</strong> Three protected accessors —
/// <see cref="ResidualByteCount" />, <see cref="ResidualBytes" />, and <see cref="TotalLength" /> — expose the
/// base-class state, and <see cref="CopyResidualStateFrom(BlockNonCryptographicHashAlgorithm{T})" /> performs the
/// corresponding write-side step. Derived <see cref="Clone" /> implementations typically allocate a new instance, copy
/// algorithm-specific accumulators field-by-field, and finish with a call to
/// <see cref="CopyResidualStateFrom(BlockNonCryptographicHashAlgorithm{T})" />.
/// </para>
/// <para>
/// <strong>Suitability.</strong> Like every other type in <c>Bodu.IO.Hashing</c>, derivations of this class produce
/// <em>non-cryptographic</em> digests intended for integrity checks, fingerprinting, hash-table keys, and bloom-filter
/// inputs. They do not provide preimage or collision resistance and must not be used for password hashing, message
/// authentication, or any security-sensitive context — use a member of <c>Bodu.Security.Cryptography</c> or the BCL's
/// <see cref="System.Security.Cryptography.HashAlgorithm" /> hierarchy instead. Instances are not thread-safe; share
/// behind explicit synchronization.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Sketch of a derived block hash. The base class drives buffering and snapshotting; the
/// derived type only expresses how a single block mutates the accumulator.
/// public sealed class MyBlockHash : BlockNonCryptographicHashAlgorithm<MyBlockHash>
/// {
///     private uint _state;
///
///     public MyBlockHash()
///         : base(hashLengthInBytes: 4, blockSize: 16) { }
///
///     protected override void ProcessBlock(ReadOnlySpan<byte> block)
///     {
///         // mix the 16-byte block into _state ...
///     }
///
///     protected override byte[] PadBlock(ReadOnlySpan<byte> trailing, ulong messageLength)
///     {
///         // append a 0x80 byte, zero-pad, then write messageLength little-endian;
///         // return one or more whole blocks.
///         return Array.Empty<byte>();
///     }
///
///     protected override byte[] ProcessFinalBlock() =>
///         BitConverter.GetBytes(_state);
///
///     protected override MyBlockHash Clone()
///     {
///         var copy = new MyBlockHash { _state = _state };
///         copy.CopyResidualStateFrom(this);
///         return copy;
///     }
/// }
///]]>
/// </example>
/// <seealso cref="System.IO.Hashing.NonCryptographicHashAlgorithm"/> <seealso cref="IResumableHashAlgorithm"/>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockNonCryptographicHashAlgorithm{T}" /> class using the specified
    /// output size and block size.
    /// </summary>
    /// <param name="hashLengthInBytes">
    /// The length, in bytes, of the hash produced by this algorithm. Must be greater than zero.
    /// </param>
    /// <param name="blockSize">
    /// The block size, in bytes, that the algorithm uses to process input data. Must be greater than zero.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashLengthInBytes" /> ≤ 0, or <paramref name="blockSize" /> ≤ 0.
    /// </exception>
    protected BlockNonCryptographicHashAlgorithm(int hashLengthInBytes, int blockSize)
        : base(hashLengthInBytes)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(blockSize, 0);
        BlockSizeBytes = blockSize;
        _residualByteBuffer = new byte[blockSize];
    }

    /// <summary>
    /// Gets a value indicating whether the final padded block must be sliced into aligned blocks, or whether the full
    /// padded result may be passed as a single block.
    /// </summary>
    protected virtual bool AllowUnalignedFinalBlock => false;

    /// <summary>
    /// Gets the number of residual bytes currently buffered but not yet processed.
    /// </summary>
    /// <remarks>
    /// Exposed to derived types that snapshot state in <see cref="Clone" />.
    /// </remarks>
    protected int ResidualByteCount => _residualBytes;

    /// <summary>
    /// Gets a read-only view over the residual byte buffer.
    /// </summary>
    /// <remarks>
    /// Exposed to derived types that snapshot state in <see cref="Clone" />.
    /// </remarks>
    protected ReadOnlySpan<byte> ResidualBytes =>
        new(_residualByteBuffer, 0, _residualBytes);

    /// <summary>
    /// Gets the total number of bytes that have been passed to <see cref="Append(ReadOnlySpan{byte})" /> since the last
    /// call to <see cref="Reset" />.
    /// </summary>
    /// <remarks>
    /// Exposed to derived types that snapshot state in <see cref="Clone" />.
    /// </remarks>
    protected ulong TotalLength { get; private set; }

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<byte> source) =>
        ProcessBlocks(source);

    /// <inheritdoc />
    public override void Reset()
    {
        Array.Clear(_residualByteBuffer, 0, _residualByteBuffer.Length);
        _residualBytes = 0;
        TotalLength = 0;
        ResetState();
    }

    /// <summary>
    /// Creates a deep copy of the current algorithm instance, preserving accumulator state. Used by
    /// <see cref="GetCurrentHashCore" /> so that retrieving an intermediate hash does not disturb ongoing computation.
    /// </summary>
    /// <returns>A new instance with the same internal state as the current one.</returns>
    protected abstract T Clone();

    /// <summary>
    /// Copies the caller's residual-buffer state onto this instance. Used by <see cref="Clone" /> implementations in
    /// derived types to duplicate the running input-alignment state.
    /// </summary>
    /// <param name="source">The algorithm instance whose residual state should be copied.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    protected void CopyResidualStateFrom(BlockNonCryptographicHashAlgorithm<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source._residualByteBuffer.AsSpan(0, source._residualBytes).CopyTo(_residualByteBuffer);
        _residualBytes = source._residualBytes;
        TotalLength = source.TotalLength;
    }

    /// <inheritdoc />
    protected override void GetCurrentHashCore(Span<byte> destination)
    {
        // Snapshot current accumulator state so that GetCurrentHash() remains non-destructive:
        // pad/final-block processing is performed on a cloned instance, and its digest is copied to destination.
        T snapshot = Clone();

        if (snapshot.ShouldPadFinalBlock())
        {
            var finalBlock = snapshot.PadBlock(
                new ReadOnlySpan<byte>(snapshot._residualByteBuffer, 0, snapshot._residualBytes),
                snapshot.TotalLength);

            if (snapshot.AllowUnalignedFinalBlock)
            {
                snapshot.ProcessBlock(finalBlock);
            }
            else
            {
                if (finalBlock.Length % snapshot.BlockSizeBytes != 0)
                    throw new InvalidOperationException(
                        $"{nameof(PadBlock)} returned {finalBlock.Length} bytes, which is not a multiple of {nameof(BlockSizeBytes)} ({snapshot.BlockSizeBytes}). Override {nameof(AllowUnalignedFinalBlock)} to permit a non-aligned final block.");

                for (var i = 0; i < finalBlock.Length; i += snapshot.BlockSizeBytes)
                    snapshot.ProcessBlock(finalBlock.AsSpan(i, snapshot.BlockSizeBytes));
            }
        }
        else if (snapshot._residualBytes > 0)
        {
            snapshot.ProcessBlock(new ReadOnlySpan<byte>(snapshot._residualByteBuffer, 0, snapshot._residualBytes));
        }

        var digest = snapshot.ProcessFinalBlock();
        digest.AsSpan(0, HashLengthInBytes).CopyTo(destination);
    }

    /// <summary>
    /// Pads the final partial block of input data and appends the encoded total message length. This ensures that all
    /// input is padded and aligned to the block size required by the algorithm.
    /// </summary>
    /// <param name="block">
    /// The final block of unprocessed input, typically containing 0 to <see cref="BlockSizeBytes" />-1 bytes.
    /// </param>
    /// <param name="messageLength">
    /// The total number of bytes processed by the algorithm before padding, not including this block.
    /// </param>
    /// <returns>
    /// A padded byte array consisting of one or more full blocks that include the input data and message-length
    /// encoding, ready to be passed to <see cref="ProcessBlock(ReadOnlySpan{byte})" />.
    /// </returns>
    protected abstract byte[] PadBlock(ReadOnlySpan<byte> block, ulong messageLength);

    /// <summary>
    /// Transforms a complete block of input data and updates the internal hash state.
    /// </summary>
    /// <param name="block">
    /// The input block to process. Its length must match the algorithm's configured block size.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="block" /> is not the expected size.
    /// </exception>
    protected abstract void ProcessBlock(ReadOnlySpan<byte> block);

    /// <summary>
    /// Finalizes the hash computation and produces the final hash output.
    /// </summary>
    /// <returns>A byte array containing the final computed hash value.</returns>
    protected abstract byte[] ProcessFinalBlock();

    /// <summary>
    /// Resets the derived algorithm's internal accumulators. Called from <see cref="Reset" /> after base-class state is
    /// cleared.
    /// </summary>
    /// <remarks>
    /// Derived types override this to restore any running state they own (for example, partial sums, index counters).
    /// The default implementation does nothing.
    /// </remarks>
    protected virtual void ResetState()
    {
    }

    /// <summary>
    /// Determines whether the final block of input data should be padded before processing.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the final block should be padded; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// By default this method returns <see langword="true" />. Derived classes can override to indicate that trailing
    /// residual bytes should be processed verbatim without explicit padding.
    /// </remarks>
    protected virtual bool ShouldPadFinalBlock() => true;

    /// <summary>
    /// Accumulates <paramref name="buffer" /> into the residual buffer and drains complete blocks to
    /// <c>ProcessFullBlock</c>, preserving any incomplete trailing block for a subsequent call.
    /// </summary>
    /// <param name="buffer">The input bytes to feed into the hash.</param>
    /// <exception cref="InvalidOperationException">
    /// Appending <paramref name="buffer" /> would cause the cumulative <see cref="TotalLength" /> counter to
    /// overflow <see cref="ulong.MaxValue" />.
    /// </exception>
    private void ProcessBlocks(ReadOnlySpan<byte> buffer)
    {
        var pos = 0;
        var bufferLength = (ulong)buffer.Length;
        if (bufferLength > ulong.MaxValue - TotalLength)
            throw new InvalidOperationException(
                $"Cumulative input length would exceed the {ulong.MaxValue}-byte limit tracked by {nameof(TotalLength)}.");

        TotalLength += bufferLength;

        Span<byte> residualSpan = _residualByteBuffer;

        if (_residualBytes > 0)
        {
            var remaining = BlockSizeBytes - _residualBytes;

            if (buffer.Length >= remaining)
            {
                buffer.Slice(0, remaining).CopyTo(residualSpan[_residualBytes..]);
                ProcessBlock(_residualByteBuffer);
                _residualBytes = 0;
                pos += remaining;
            }
            else
            {
                buffer.CopyTo(residualSpan[_residualBytes..]);
                _residualBytes += buffer.Length;
                return;
            }
        }

        while (pos + BlockSizeBytes <= buffer.Length)
        {
            ProcessBlock(buffer.Slice(pos, BlockSizeBytes));
            pos += BlockSizeBytes;
        }

        _residualBytes = buffer.Length - pos;
        if (_residualBytes > 0)
            buffer.Slice(pos, _residualBytes).CopyTo(residualSpan);
    }
}
