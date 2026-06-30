// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein{T}.Ubi.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

public abstract partial class Skein<T>
{
    /// <summary>
    /// Returns the cached initial chaining value derived from the configuration block (and the optional KEY UBI phase
    /// when a key is set). Exposed for Skein 1.3 Appendix B verification — the published IVs for the canonical unkeyed
    /// hash configurations must equal the words returned here when the algorithm is constructed with no key and the
    /// matching output size.
    /// </summary>
    /// <returns>
    /// A defensive copy of the chaining-value words that <see cref="Initialize" /> would seed the state with.
    /// </returns>
    /// <remarks>
    /// The first call computes and caches the IV; subsequent calls return clones of the cached value. Mutating the
    /// returned array does not affect the algorithm's internal state.
    /// </remarks>
    internal ulong[] GetInitialChainingValueWords()
    {
        ThrowIfDisposed();
        EnsureChainingValueInitialized();
        return (ulong[])_initialChainingValue.Clone();
    }

    /// <summary>
    /// Applies a single Skein <c>UBI</c> (Unique Block Iteration) compression step that folds <paramref name="block" />
    /// into the chaining state using the supplied tweak fields.
    /// </summary>
    /// <param name="block">
    /// The block to absorb. Must be exactly <see cref="BufferedBlockHashAlgorithm{T}.BlockSize" /> bytes long.
    /// </param>
    /// <param name="type">The UBI block type, which occupies bits 120..125 of the tweak.</param>
    /// <param name="first">Whether this is the first UBI call in the current stage. Sets bit 126 of the tweak.</param>
    /// <param name="final">Whether this is the final UBI call in the current stage. Sets bit 127 of the tweak.</param>
    /// <param name="position">
    /// The cumulative number of real (unpadded) bytes processed by this stage through and including the current block.
    /// Stored in the lower 64 bits of the tweak — the upper 32 bits of the specification's 96-bit position field are
    /// always zero for the sequential hashing profile implemented here.
    /// </param>
    /// <remarks>
    /// Performs <c>G' = E_G(block) ⊕ block</c>, the Matyas-Meyer-Oseas mode that underlies Skein: the current chaining
    /// value serves as the Threefish key, the packed tweak carries the domain-separating metadata, and the block is
    /// both the plaintext and the second operand of the feed-forward XOR.
    /// </remarks>
    private void Ubi(ReadOnlySpan<byte> block, SkeinTweakType type, bool first, bool final, ulong position)
    {
        Span<byte> tweakBytes = stackalloc byte[TweakSize / 8];
        PackTweak(tweakBytes, position, type, first, final);

        Span<byte> stateBytes = MemoryMarshal.AsBytes(_state.AsSpan());
        _cipher.Rekey(stateBytes, tweakBytes);
        _cipher.Encrypt(block, _ubiCipherOutput);

        // Matyas-Meyer-Oseas feed-forward: new chaining = E(block) ⊕ block.
        Span<ulong> stateWords = _state;
        ReadOnlySpan<ulong> cipherWords = MemoryMarshal.Cast<byte, ulong>(_ubiCipherOutput);
        ReadOnlySpan<ulong> blockWords = MemoryMarshal.Cast<byte, ulong>(block);

        for (int i = 0; i < stateWords.Length; i++)
            stateWords[i] = cipherWords[i] ^ blockWords[i];
    }

    /// <summary>
    /// Packs the 128-bit Skein tweak into 16 bytes ready for <see cref="ThreefishBlockCipher.Rekey" />.
    /// </summary>
    /// <param name="destination">A 16-byte buffer that will receive the packed tweak.</param>
    /// <param name="position">The position field (cumulative stage byte count through the current block).</param>
    /// <param name="type">The UBI block type.</param>
    /// <param name="first">Whether the <c>First</c> flag is set.</param>
    /// <param name="final">Whether the <c>Final</c> flag is set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PackTweak(Span<byte> destination, ulong position, SkeinTweakType type, bool first, bool final)
    {
        // T0 carries the low 64 bits of the position; the upper 32 bits of the 96-bit position field are always zero
        // for sequential hashing, so the derived T1 simply encodes the block type and the first/final flags.
        ulong t1 = ((ulong)(byte)type << 56)
            | (first ? 1UL << 62 : 0UL)
            | (final ? 1UL << 63 : 0UL);

        BinaryPrimitives.WriteUInt64LittleEndian(destination, position);
        BinaryPrimitives.WriteUInt64LittleEndian(destination[sizeof(ulong)..], t1);
    }

    /// <summary>
    /// Builds the 32-byte configuration block defined by Skein 1.3 §3.5.1 and zero-pads it out to the current state
    /// size.
    /// </summary>
    /// <param name="destination">
    /// A buffer of <see cref="BufferedBlockHashAlgorithm{T}.BlockSize" /> bytes that receives the packed configuration
    /// block.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BuildConfigurationBlock(Span<byte> destination)
    {
        destination.Clear();
        BinaryPrimitives.WriteUInt32LittleEndian(destination, SchemaIdentifier);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[4..], SchemaVersion);

        // Bytes 6..7 are reserved and remain zero. Bytes 8..15 encode the output size in bits as a little-endian u64.
        BinaryPrimitives.WriteUInt64LittleEndian(destination[8..], (ulong)HashSizeValue);

        // Tree-related fields at offsets 16..18 remain zero for sequential hashing.
    }

    /// <summary>
    /// Derives and caches the initial chaining value for the current configuration by running the <c>KEY</c> UBI phase
    /// (when a key is present) followed by the <c>CFG</c> UBI phase over the Skein configuration block.
    /// </summary>
    private void EnsureChainingValueInitialized()
    {
        CryptographyHelper.Clear(_state);

        if (KeyValue is { Length: > 0 })
        {
            AbsorbKeyPhase(KeyValue);
        }

        Span<byte> configurationBlock = stackalloc byte[BlockSize / 8];
        BuildConfigurationBlock(configurationBlock);

        // The configuration UBI is a single 32-byte payload, zero-padded up to the state size. Per the Skein
        // specification the tweak position is fixed at 32, matching the logical length of the configuration string
        // regardless of the state block size it is padded to.
        Ubi(configurationBlock, SkeinTweakType.Cfg, first: true, final: true, position: 32UL);

        Array.Copy(_state, _initialChainingValue, _state.Length);
        _isChainingValueCached = true;
    }

    /// <summary>
    /// Runs the Skein-MAC <c>KEY</c> UBI phase, absorbing <paramref name="key" /> in state-sized blocks with zero
    /// padding on the final partial block when required.
    /// </summary>
    /// <param name="key">The key material to absorb.</param>
    private void AbsorbKeyPhase(ReadOnlySpan<byte> key)
    {
        int blockBytes = BlockSize / 8;
        Span<byte> block = stackalloc byte[blockBytes];
        ulong absorbed = 0UL;
        bool first = true;
        int remaining = key.Length;
        int offset = 0;

        while (remaining > blockBytes)
        {
            key.Slice(offset, blockBytes).CopyTo(block);
            absorbed += (ulong)blockBytes;
            Ubi(block, SkeinTweakType.Key, first, final: false, position: absorbed);

            first = false;
            offset += blockBytes;
            remaining -= blockBytes;
        }

        // Final key block: remaining is in [0..blockSize]; an empty key is not allowed to reach this method.
        int finalLength = remaining;
        block.Clear();
        key.Slice(offset, finalLength).CopyTo(block);
        absorbed += (ulong)finalLength;
        Ubi(block, SkeinTweakType.Key, first, final: true, position: absorbed);
    }

    /// <summary>
    /// Absorbs message bytes into the chaining state using a one-block lag so that the last UBI call in the message
    /// phase can be correctly flagged as <c>Final</c> at <see cref="HashFinal" /> time.
    /// </summary>
    /// <param name="source">The next chunk of input data.</param>
    private void AbsorbMessage(ReadOnlySpan<byte> source)
    {
        int blockSize = BlockSize / 8;

        // Merge the tail of the previous call with the head of this one until we hold a full block of pending data.
        if (_pendingBytes > 0 && _pendingBytes + source.Length > blockSize)
        {
            int toCopy = blockSize - _pendingBytes;
            source[..toCopy].CopyTo(_pendingBlock.AsSpan(_pendingBytes));
            _pendingBytes = blockSize;
            source = source[toCopy..];

            // Only flush the pending block once we know additional bytes exist to distinguish it from the Final block.
            _messageBytesProcessed += (ulong)blockSize;
            Ubi(
                _pendingBlock,
                SkeinTweakType.Msg,
                first: !_hasProcessedAnyMessageBlock,
                final: false,
                position: _messageBytesProcessed);
            _hasProcessedAnyMessageBlock = true;
            _pendingBytes = 0;
        }

        // Pending is empty here unless source exactly fits alongside the existing pending tail — handled above.
        // Stream full blocks, always keeping at least one block of lookahead deferred in the pending buffer.
        while (source.Length > blockSize)
        {
            _messageBytesProcessed += (ulong)blockSize;
            Ubi(
                source[..blockSize],
                SkeinTweakType.Msg,
                first: !_hasProcessedAnyMessageBlock,
                final: false,
                position: _messageBytesProcessed);
            _hasProcessedAnyMessageBlock = true;
            source = source[blockSize..];
        }

        // Buffer the trailing bytes (which may form a full block or a partial one); they are flushed by HashFinal.
        if (source.Length > 0)
        {
            source.CopyTo(_pendingBlock.AsSpan(_pendingBytes));
            _pendingBytes += source.Length;
        }
    }

    /// <summary>
    /// Runs the Skein output UBI chain against the finalized chaining value, emitting consecutive state-sized output
    /// fragments until <paramref name="destination" /> is filled.
    /// </summary>
    /// <param name="destination">
    /// The digest buffer, of length <see cref="System.Security.Cryptography.HashAlgorithm.HashSize" /> / 8.
    /// </param>
    /// <remarks>
    /// <para>
    /// Each output call saves the final chaining value and re-runs UBI over a zero-filled block whose first eight bytes
    /// carry the output counter (0, 1, 2, …). The resulting chaining value contributes one state-sized fragment of the
    /// digest; the chaining value is then restored before the next iteration so that each output counter is applied to
    /// the same final state.
    /// </para>
    /// </remarks>
    private void GenerateOutput(Span<byte> destination)
    {
        int blockSize = BlockSize / 8;
        int stateWords = _state.Length;

        Span<ulong> savedChainingValue = stackalloc ulong[stateWords];
        _state.AsSpan().CopyTo(savedChainingValue);

        Span<byte> outputBlock = stackalloc byte[blockSize];
        int written = 0;
        ulong counter = 0UL;

        while (written < destination.Length)
        {
            outputBlock.Clear();
            BinaryPrimitives.WriteUInt64LittleEndian(outputBlock, counter);

            // Restore the saved chaining value so every output counter is applied to the same finalized state.
            savedChainingValue.CopyTo(_state);

            Ubi(outputBlock, SkeinTweakType.Out, first: true, final: true, position: sizeof(ulong));

            int remaining = destination.Length - written;
            int toCopy = remaining < blockSize ? remaining : blockSize;
            MemoryMarshal.AsBytes(_state.AsSpan())[..toCopy].CopyTo(destination[written..]);
            written += toCopy;
            counter++;
        }

        // Leave the chaining state in its finalized position so that a caller inspecting it cannot read stale output.
        savedChainingValue.CopyTo(_state);
    }
}
