// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Computes a 256-bit cryptographic hash using the <c>BLAKE3</c> algorithm designed by Jack
/// O'Connor, Jean-Philippe Aumasson, Samuel Neves, and Zooko Wilcox-O'Hearn. This class cannot
/// be inherited.
/// </summary>
/// <remarks>
/// <para>
/// BLAKE3 is a cryptographic hash function that combines the speed of non-cryptographic hashes
/// with strong security guarantees. It is based on a binary tree structure where each leaf
/// (chunk) processes up to 1024 bytes of input and each internal (parent) node combines two
/// child chaining values. All compression is performed by a single ARX-based function derived
/// from the BLAKE2 and ChaCha families.
/// </para>
/// <para>
/// Input is divided into 1024-byte chunks, each compressed block-by-block into an 8-word
/// (256-bit) chaining value. When more than one chunk exists the chaining values are folded
/// pairwise into parent nodes until a single root chaining value remains. The root compression
/// call is distinguished by the <c>ROOT</c> domain-separation flag, which enables XOF-style
/// output extraction; this implementation fixes the output length at 256 bits.
/// </para>
/// <para>
/// This implementation supports the standard, unkeyed hash mode only. Keyed-hash and
/// key-derivation modes are not exposed.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var blake3 = new Blake3();
/// byte[] digest = blake3.ComputeHash(message);
/// </code>
/// </example>
public sealed class Blake3
    : HashAlgorithm
{
    // ---- domain-separation flags (§2.5 of the BLAKE3 specification) ----

    /// <summary>The flag applied to the first compression block of every chunk.</summary>
    private const uint FlagChunkStart = 1u;

    /// <summary>The flag applied to the last compression block of every chunk.</summary>
    private const uint FlagChunkEnd = 2u;

    /// <summary>The flag applied to every parent (non-leaf) node compression call.</summary>
    private const uint FlagParent = 4u;

    /// <summary>The flag applied to the root compression call to enable output extraction.</summary>
    private const uint FlagRoot = 8u;

    // ---- structural constants ----

    /// <summary>Size, in bytes, of a single compression input block.</summary>
    private const int BlockSize = 64;

    /// <summary>Size, in bytes, of a single input chunk (leaf of the hash tree).</summary>
    private const int ChunkSize = 1024;

    /// <summary>Output length in bytes.</summary>
    private const int OutLen = 32;

    // ---- initialisation vector (first eight words of the SHA-256 IV) ----

    /// <summary>
    /// The BLAKE3 initialisation vector, taken from the fractional parts of the square roots of
    /// the first eight prime numbers, identical to the SHA-256 IV.
    /// </summary>
    private static readonly uint[] s_iv =
    {
        0x6A09E667u, 0xBB67AE85u, 0x3C6EF372u, 0xA54FF53Au,
        0x510E527Fu, 0x9B05688Cu, 0x1F83D9ABu, 0x5BE0CD19u,
    };

    /// <summary>
    /// The per-round message word permutation table (§2.4 of the BLAKE3 specification).
    /// Each row gives the 16 message-word indices consumed by a single round's G calls.
    /// </summary>
    private static readonly byte[,] s_msgSchedule =
    {
        {  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 },
        {  2,  6,  3, 10,  7,  0,  4, 13,  1, 11, 12,  5,  9, 14, 15,  8 },
        {  3,  4, 10, 12, 13,  2,  7, 14,  6,  5,  9,  0, 11, 15,  8,  1 },
        { 10,  7, 12,  9, 14,  3, 13, 15,  4,  0, 11,  2,  5,  8,  1,  6 },
        { 12, 13,  9, 11, 15, 10, 14,  8,  7,  2,  5,  3,  0,  1,  6,  4 },
        {  9, 14, 11,  5,  8, 12, 15,  1, 13,  3,  0, 10,  2,  6,  4,  7 },
        { 11, 15,  5,  0,  1,  9,  8,  6, 14, 10,  2, 12,  3,  4,  7, 13 },
    };

    // ---- streaming state ----

    /// <summary>
    /// Accumulation buffer for the current in-progress chunk.
    /// </summary>
    /// <remarks>
    /// The buffer always holds at least one byte of buffered input before <see cref="HashFinal" />
    /// is called, so that the final chunk — whether partial or exactly 1024 bytes — is never
    /// eagerly compressed during <see cref="HashCore(ReadOnlySpan{byte})" />. This invariant
    /// ensures the <see cref="FlagRoot" /> domain-separation flag can be applied correctly.
    /// </remarks>
    private readonly byte[] _chunkBuffer = new byte[ChunkSize];

    /// <summary>Chaining-value stack used to build parent nodes as chunks complete.</summary>
    private readonly List<uint[]> _cvStack = new();

    /// <summary>Number of bytes currently held in <see cref="_chunkBuffer" />.</summary>
    private int _chunkBuffered;

    /// <summary>Zero-based index of the chunk currently being accumulated.</summary>
    private ulong _chunkCounter;

    /// <summary>
    /// <see langword="true" /> after <see cref="Dispose(bool)" /> has been called; prevents double-disposal.
    /// </summary>
    private bool _disposed;

    // ---- construction ----

    /// <summary>
    /// Initialises a new instance of the <see cref="Blake3" /> class, configured to produce a
    /// 256-bit digest.
    /// </summary>
    public Blake3()
    {
        HashSizeValue = 256;
    }

    // ---- HashAlgorithm overrides ----

    /// <summary>
    /// Gets a value indicating whether this transform instance can be reused after a hash
    /// operation is completed.
    /// </summary>
    /// <returns>
    /// <see langword="true" />; <see cref="Blake3" /> resets its state automatically and may be
    /// reused across multiple <c>ComputeHash</c> calls.
    /// </returns>
    public override bool CanReuseTransform => true;

    /// <summary>
    /// Gets a value indicating whether multiple blocks may be transformed in a single
    /// <see cref="HashAlgorithm.TransformBlock" /> call.
    /// </summary>
    /// <returns>
    /// <see langword="true" />; the implementation accumulates arbitrary-length input internally.
    /// </returns>
    public override bool CanTransformMultipleBlocks => true;

    /// <summary>
    /// Gets the preferred input block size, in bytes, used when feeding data through
    /// <see cref="System.Security.Cryptography.CryptoStream" />.
    /// </summary>
    /// <returns>
    /// <see cref="ChunkSize" /> (1024 bytes) — one full BLAKE3 leaf chunk.
    /// </returns>
    public override int InputBlockSize => BlockSize;

    /// <summary>
    /// Gets the output block size, in bytes.
    /// </summary>
    /// <returns>
    /// <see cref="OutLen" /> (32 bytes), the fixed 256-bit digest length produced by this
    /// implementation.
    /// </returns>
    public override int OutputBlockSize => OutLen;

    /// <summary>
    /// Resets the hash algorithm to its initial state so that a new hash computation can begin.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public override void Initialize()
    {
        ThrowIfDisposed();

        _chunkBuffered = 0;
        _chunkCounter = 0;
        _cvStack.Clear();
    }

    // ---- HashAlgorithm core ----

    /// <summary>
    /// Routes incoming data through the BLAKE3 streaming accumulator. Full intermediate chunks
    /// are compressed immediately; the final chunk (whether exactly 1024 bytes or partial) is
    /// always deferred to <see cref="HashFinal" /> so that the <c>ROOT</c> flag can be applied
    /// correctly.
    /// </summary>
    /// <param name="array">The input byte array containing the data to hash. Must not be <see langword="null" />.</param>
    /// <param name="ibStart">The zero-based offset in <paramref name="array" /> at which to begin reading.</param>
    /// <param name="cbSize">The number of bytes to process.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="ibStart" /> or <paramref name="cbSize" /> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ibStart" /> and <paramref name="cbSize" /> together exceed the length of
    /// <paramref name="array" />.
    /// </exception>
    protected override void HashCore(byte[] array, int ibStart, int cbSize)
    {
        ThrowHelper.ThrowIfNull(array);
        ThrowIfDisposed();

        HashCore(array.AsSpan(ibStart, cbSize));
    }

    /// <summary>
    /// Routes incoming data through the BLAKE3 streaming accumulator. Full intermediate chunks
    /// are compressed immediately; the final chunk is always deferred to
    /// <see cref="HashFinal" />.
    /// </summary>
    /// <param name="source">The input byte span containing the data to hash.</param>
    protected override void HashCore(ReadOnlySpan<byte> source)
    {
        ThrowIfDisposed();

        while (source.Length > 0)
        {
            int spaceInChunk = ChunkSize - _chunkBuffered;

            // A chunk is only compressed here when the buffer is full AND there is still more
            // input after it.  This guarantees the chunk buffer always holds ≥ 1 byte on entry
            // to HashFinal, so the last chunk can be correctly tagged with CHUNK_END | ROOT.
            if (_chunkBuffered == ChunkSize && source.Length > 0)
            {
                uint[] cv = ChunkChainingValue(_chunkBuffer.AsSpan(0, ChunkSize), _chunkCounter, isRoot: false);
                PushChunkCv(cv, _chunkCounter);

                _chunkCounter++;
                _chunkBuffered = 0;
                spaceInChunk = ChunkSize;
            }

            // Copy as many bytes as fit into the current chunk buffer.
            int toCopy = source.Length < spaceInChunk ? source.Length : spaceInChunk;
            source.Slice(0, toCopy).CopyTo(_chunkBuffer.AsSpan(_chunkBuffered));
            _chunkBuffered += toCopy;
            source = source.Slice(toCopy);
        }
    }

    /// <summary>
    /// Finalises the BLAKE3 hash computation and returns the 256-bit digest.
    /// </summary>
    /// <returns>
    /// A 32-byte array containing the BLAKE3 digest of all data supplied through
    /// <see cref="HashCore(byte[], int, int)" />.
    /// </returns>
    protected override byte[] HashFinal()
    {
        ThrowIfDisposed();

        uint[] rootCv;

        if (_chunkCounter == 0)
        {
            // The entire input fits in a single chunk — compress it directly as the root.
            rootCv = ChunkChainingValue(_chunkBuffer.AsSpan(0, _chunkBuffered), _chunkCounter, isRoot: true);
        }
        else
        {
            // Compress the last (possibly partial) chunk, then fold the CV stack to the root.
            uint[] lastCv = ChunkChainingValue(_chunkBuffer.AsSpan(0, _chunkBuffered), _chunkCounter, isRoot: false);
            PushChunkCv(lastCv, _chunkCounter);

            rootCv = MergeStack();
        }

        // Serialise the root chaining value (8 × uint32 LE) into the output digest.
        byte[] digest = new byte[OutLen];

        for (int i = 0; i < 8; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(digest.AsSpan(i * 4), rootCv[i]);

        return digest;
    }

    /// <summary>
    /// Releases managed and unmanaged resources held by this instance, zeroing sensitive state.
    /// </summary>
    /// <param name="disposing">
    /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" />
    /// to release only unmanaged resources.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            Array.Clear(_chunkBuffer, 0, _chunkBuffer.Length);
            _cvStack.Clear();
            _chunkBuffered = 0;
            _chunkCounter = 0;
            HashSizeValue = 0;
            CryptoHelpers.ClearAndNullify(ref HashValue);
        }

        _disposed = true;
        base.Dispose(disposing);
    }

    // ---- core compression ----

    /// <summary>
    /// Performs the BLAKE3 compression function, transforming a chaining value and a 16-word
    /// message block into a 16-word output state.
    /// </summary>
    /// <param name="cv">
    /// The 8-word input chaining value. Must have a length of at least 8.
    /// </param>
    /// <param name="blockWords">
    /// The 16-word message block in little-endian order. Must have a length of at least 16.
    /// </param>
    /// <param name="counter">
    /// The 64-bit chunk counter encoding the position of this chunk in the input stream.
    /// </param>
    /// <param name="blockLen">
    /// The number of input bytes represented by <paramref name="blockWords" /> (0–64).
    /// </param>
    /// <param name="flags">
    /// The bitwise OR of any applicable domain-separation flags (<see cref="FlagChunkStart" />,
    /// <see cref="FlagChunkEnd" />, <see cref="FlagParent" />, <see cref="FlagRoot" />).
    /// </param>
    /// <returns>
    /// A 16-element array containing the post-compression state, whose first eight words form
    /// the updated chaining value.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint[] Compress(uint[] cv, uint[] blockWords, ulong counter, uint blockLen, uint flags)
    {
        uint[] state = new uint[16];

        // Upper half: chaining value.
        state[0] = cv[0];
        state[1] = cv[1];
        state[2] = cv[2];
        state[3] = cv[3];
        state[4] = cv[4];
        state[5] = cv[5];
        state[6] = cv[6];
        state[7] = cv[7];

        // Lower half: IV words, counter split into two 32-bit halves, block length, flags.
        state[8] = s_iv[0];
        state[9] = s_iv[1];
        state[10] = s_iv[2];
        state[11] = s_iv[3];
        state[12] = (uint)counter;
        state[13] = (uint)(counter >> 32);
        state[14] = blockLen;
        state[15] = flags;

        // Seven rounds of column then diagonal mixing, each using a permuted view of the message block.
        for (int round = 0; round < 7; round++)
        {
            // Column step.
            G(state, 0, 4,  8, 12, blockWords[s_msgSchedule[round,  0]], blockWords[s_msgSchedule[round,  1]]);
            G(state, 1, 5,  9, 13, blockWords[s_msgSchedule[round,  2]], blockWords[s_msgSchedule[round,  3]]);
            G(state, 2, 6, 10, 14, blockWords[s_msgSchedule[round,  4]], blockWords[s_msgSchedule[round,  5]]);
            G(state, 3, 7, 11, 15, blockWords[s_msgSchedule[round,  6]], blockWords[s_msgSchedule[round,  7]]);

            // Diagonal step.
            G(state, 0, 5, 10, 15, blockWords[s_msgSchedule[round,  8]], blockWords[s_msgSchedule[round,  9]]);
            G(state, 1, 6, 11, 12, blockWords[s_msgSchedule[round, 10]], blockWords[s_msgSchedule[round, 11]]);
            G(state, 2, 7,  8, 13, blockWords[s_msgSchedule[round, 12]], blockWords[s_msgSchedule[round, 13]]);
            G(state, 3, 4,  9, 14, blockWords[s_msgSchedule[round, 14]], blockWords[s_msgSchedule[round, 15]]);
        }

        // Finalise: XOR the two halves of the state.
        for (int i = 0; i < 8; i++)
            state[i] ^= state[i + 8];

        // Mix the original chaining value into the high half.
        for (int i = 8; i < 16; i++)
            state[i] ^= cv[i - 8];

        return state;
    }

    /// <summary>
    /// Applies the BLAKE3 quarter-round mixing function <c>G</c> to four positions in the
    /// working state, using two message words <paramref name="mx" /> and <paramref name="my" />.
    /// </summary>
    /// <param name="state">The 16-word working state array, modified in place.</param>
    /// <param name="a">Index of the first state word.</param>
    /// <param name="b">Index of the second state word.</param>
    /// <param name="c">Index of the third state word.</param>
    /// <param name="d">Index of the fourth state word.</param>
    /// <param name="mx">First message word for this mixing step.</param>
    /// <param name="my">Second message word for this mixing step.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void G(uint[] state, int a, int b, int c, int d, uint mx, uint my)
    {
        state[a] = state[a] + state[b] + mx;
        state[d] = RotateRight(state[d] ^ state[a], 16);
        state[c] = state[c] + state[d];
        state[b] = RotateRight(state[b] ^ state[c], 12);
        state[a] = state[a] + state[b] + my;
        state[d] = RotateRight(state[d] ^ state[a], 8);
        state[c] = state[c] + state[d];
        state[b] = RotateRight(state[b] ^ state[c], 7);
    }

    /// <summary>
    /// Rotates <paramref name="value" /> right by <paramref name="bits" /> positions.
    /// </summary>
    /// <param name="value">The value to rotate.</param>
    /// <param name="bits">The number of bit positions to rotate right.</param>
    /// <returns>The rotated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint RotateRight(uint value, int bits) =>
        (value >> bits) | (value << (32 - bits));

    // ---- chunk and parent processing ----

    /// <summary>
    /// Computes the chaining value for a single input chunk by processing its bytes as up to
    /// 16 sequential 64-byte blocks through the BLAKE3 compression function.
    /// </summary>
    /// <param name="chunk">
    /// The raw chunk bytes to process (0 to 1024 bytes). Blocks shorter than 64 bytes are
    /// zero-padded internally before compression. An empty span is permitted and produces a
    /// single compression call with <c>block_len = 0</c>, as required for the empty-input case.
    /// </param>
    /// <param name="chunkCounter">
    /// The zero-based index of this chunk within the full input stream.
    /// </param>
    /// <param name="isRoot">
    /// <see langword="true" /> if this chunk represents the sole or final root of the hash
    /// tree, causing the <see cref="FlagRoot" /> domain flag to be applied to the last block.
    /// </param>
    /// <returns>
    /// An 8-element array containing the 256-bit chaining value produced by this chunk.
    /// </returns>
    private static uint[] ChunkChainingValue(ReadOnlySpan<byte> chunk, ulong chunkCounter, bool isRoot)
    {
        // Start each chunk with the IV as the initial chaining value.
        uint[] cv = new uint[8];
        s_iv.AsSpan().CopyTo(cv);

        uint blockFlags = FlagChunkStart;
        int processed = 0;

        // Process the chunk in 64-byte blocks; the final block may be shorter.
        // The do-while ensures at least one compression call even for an empty chunk.
        do
        {
            int remaining = chunk.Length - processed;
            int blockLen = remaining >= BlockSize ? BlockSize : remaining;
            bool isLastBlock = (processed + blockLen) >= chunk.Length;

            if (isLastBlock)
            {
                blockFlags |= FlagChunkEnd;

                if (isRoot)
                    blockFlags |= FlagRoot;
            }

            // Read the block as 16 little-endian uint32 words, zero-padding if short.
            uint[] blockWords = ReadBlockWords(chunk.Slice(processed, blockLen));

            uint[] outState = Compress(cv, blockWords, chunkCounter, (uint)blockLen, blockFlags);

            // The new chaining value is the first 8 words of the compression output.
            cv[0] = outState[0];
            cv[1] = outState[1];
            cv[2] = outState[2];
            cv[3] = outState[3];
            cv[4] = outState[4];
            cv[5] = outState[5];
            cv[6] = outState[6];
            cv[7] = outState[7];

            processed += blockLen;

            // Only the first block of each chunk carries CHUNK_START.
            blockFlags = 0u;
        }
        while (processed < chunk.Length);

        return cv;
    }

    /// <summary>
    /// Reads up to 64 bytes from <paramref name="block" /> into a 16-element little-endian
    /// uint32 word array, zero-padding any bytes beyond the actual block length.
    /// </summary>
    /// <param name="block">The raw block bytes to interpret (0–64 bytes).</param>
    /// <returns>A 16-element array of little-endian uint32 words representing the block.</returns>
    private static uint[] ReadBlockWords(ReadOnlySpan<byte> block)
    {
        // Stack-allocate a 64-byte buffer, explicitly clear it, and copy the block into it.
        // Short BLAKE3 blocks are zero-padded before the 16 little-endian words are read.
        Span<byte> padded = stackalloc byte[BlockSize];
        padded.Clear();
        block.CopyTo(padded);

        uint[] words = new uint[16];

        for (int i = 0; i < 16; i++)
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(padded.Slice(i * 4));

        return words;
    }

    /// <summary>
    /// Computes a parent node chaining value by compressing the concatenation of a left and a
    /// right child chaining value.
    /// </summary>
    /// <param name="leftCv">The 8-word chaining value of the left child.</param>
    /// <param name="rightCv">The 8-word chaining value of the right child.</param>
    /// <param name="isRoot">
    /// <see langword="true" /> if this parent node is the root of the hash tree, causing
    /// <see cref="FlagRoot" /> to be applied.
    /// </param>
    /// <returns>
    /// An 8-element array containing the parent node's 256-bit chaining value.
    /// </returns>
    private static uint[] ParentCv(uint[] leftCv, uint[] rightCv, bool isRoot)
    {
        // The block words for a parent node are the concatenation of the two child CVs.
        uint[] blockWords = new uint[16];
        leftCv.AsSpan(0, 8).CopyTo(blockWords.AsSpan(0));
        rightCv.AsSpan(0, 8).CopyTo(blockWords.AsSpan(8));

        uint flags = FlagParent;

        if (isRoot)
            flags |= FlagRoot;

        // Parent nodes always use the IV as their chaining value input, counter = 0.
        uint[] outState = Compress(s_iv, blockWords, 0UL, BlockSize, flags);

        return new uint[] { outState[0], outState[1], outState[2], outState[3], outState[4], outState[5], outState[6], outState[7] };
    }

    // ---- tree-merging stack helpers ----

    /// <summary>
    /// Pushes a completed chunk chaining value onto <see cref="_cvStack" />, merging adjacent
    /// pairs into parent nodes whenever the binary tree structure requires it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// BLAKE3 uses a Merkle tree with a power-of-two fan-out. The merge condition is: after
    /// completing chunk at zero-based index <paramref name="chunkIdx" />, merge the top-of-stack
    /// entry with the incoming CV while the number of trailing zeros in
    /// <c>(<paramref name="chunkIdx" /> + 1)</c> is greater than or equal to the current stack
    /// depth. This ensures that only perfectly balanced subtrees of equal height are merged,
    /// maintaining the left-to-right ordering of leaves.
    /// </para>
    /// </remarks>
    /// <param name="cv">The 8-word chaining value produced by the completed chunk.</param>
    /// <param name="chunkIdx">The zero-based chunk index of the completed chunk.</param>
    private void PushChunkCv(uint[] cv, ulong chunkIdx)
    {
        // Merge while the top-of-stack subtree and the incoming CV form a balanced pair.
        while (_cvStack.Count > 0 && IsSubtreeComplete(chunkIdx, _cvStack.Count))
        {
            uint[] left = _cvStack[_cvStack.Count - 1];
            _cvStack.RemoveAt(_cvStack.Count - 1);
            cv = ParentCv(left, cv, isRoot: false);
        }

        _cvStack.Add(cv);
    }

    /// <summary>
    /// Determines whether the subtrees currently on the stack and the newly completed chunk
    /// should be merged, based on the binary tree completion invariant.
    /// </summary>
    /// <param name="chunkIdx">The zero-based index of the most recently completed chunk.</param>
    /// <param name="stackDepth">The current depth of <see cref="_cvStack" />.</param>
    /// <returns>
    /// <see langword="true" /> if the top entry on the stack represents a subtree of exactly
    /// the same height as the subtree rooted at the incoming CV, and therefore the two should
    /// be merged into a parent node.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSubtreeComplete(ulong chunkIdx, int stackDepth)
    {
        // The number of trailing zeros in (chunkIdx + 1) reflects how many full subtree levels
        // are complete at this point.  Merge whenever that count meets or exceeds the stack depth.
        ulong completed = chunkIdx + 1;
        int trailingZeros = System.Numerics.BitOperations.TrailingZeroCount(completed);
        return trailingZeros >= stackDepth;
    }

    /// <summary>
    /// Merges all chaining values remaining on <see cref="_cvStack" /> into a single root
    /// chaining value by folding pairs from right to left.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After the last chunk CV has been pushed via <see cref="PushChunkCv" />, the stack may
    /// hold up to ⌊log₂ N⌋ + 1 entries for an N-chunk input. The entries are ordered with the
    /// largest (leftmost) subtree at index 0 and the smallest (most-recently-completed) subtree
    /// at the top (highest index).
    /// </para>
    /// <para>
    /// Merging proceeds right-to-left: the two top-most entries are combined into a parent node,
    /// the result is re-pushed, and the loop continues until a single root CV remains. The final
    /// merge receives the <see cref="FlagRoot" /> flag.
    /// </para>
    /// </remarks>
    /// <returns>
    /// An 8-element array containing the 256-bit root chaining value of the entire hash tree.
    /// </returns>
    private uint[] MergeStack()
    {
        // Collapse right-to-left: the top two entries are always the next pair to merge,
        // because the stack invariant guarantees they represent equal-sized subtrees.
        while (_cvStack.Count > 1)
        {
            int lastIdx = _cvStack.Count - 1;
            bool isRootMerge = _cvStack.Count == 2;

            uint[] right = _cvStack[lastIdx];
            uint[] left = _cvStack[lastIdx - 1];

            _cvStack.RemoveAt(lastIdx);
            _cvStack.RemoveAt(lastIdx - 1);

            _cvStack.Add(ParentCv(left, right, isRoot: isRootMerge));
        }

        return _cvStack[0];
    }

    // ---- guard helpers ----

    /// <summary>
    /// Throws <see cref="ObjectDisposedException" /> if this instance has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);
}
