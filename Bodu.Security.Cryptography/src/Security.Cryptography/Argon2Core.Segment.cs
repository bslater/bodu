// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Segment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>The number of blocks of per-segment scratch: the carried block and its working copy, then the same pair for address generation, then the address block and the address generator's input block.</summary>
    private const int ScratchBlocks = 6;

    /// <summary>
    /// Computes one segment (the intersection of a lane and a slice) of the memory matrix.
    /// </summary>
    /// <typeparam name="TKernel">The compression kernel.</typeparam>
    /// <param name="matrix">The memory matrix.</param>
    /// <param name="geometry">The shape of the matrix.</param>
    /// <param name="pass">The zero-based pass (iteration) index.</param>
    /// <param name="slice">The zero-based slice index within the pass.</param>
    /// <param name="lane">The zero-based lane index being filled.</param>
    /// <remarks>
    /// A segment reads only blocks of its own lane and blocks of other lanes in finished slices, so the segments of one
    /// slice may be computed in any order, or at once.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void FillSegment<TKernel>(Argon2Matrix matrix, in Geometry geometry, int pass, int slice, int lane)
        where TKernel : struct, IArgon2Kernel
    {
        int laneLength = geometry.LaneLength;
        int segmentLength = geometry.SegmentLength;
        bool dataIndependent = geometry.Type == Argon2Type.Argon2i
            || (geometry.Type == Argon2Type.Argon2id && pass == 0 && slice < SyncPoints / 2);

        Span<ulong> scratch = stackalloc ulong[ScratchBlocks * WordsPerBlock];
        Span<ulong> state = scratch[..WordsPerBlock];
        Span<ulong> addressState = scratch.Slice(2 * WordsPerBlock, 2 * WordsPerBlock);
        Span<ulong> addressBlock = scratch.Slice(4 * WordsPerBlock, WordsPerBlock);
        Span<ulong> inputBlock = scratch.Slice(5 * WordsPerBlock, WordsPerBlock);

        ref ulong stateRef = ref MemoryMarshal.GetReference(state);
        ref ulong workRef = ref Unsafe.Add(ref stateRef, WordsPerBlock);

        try
        {
            if (dataIndependent)
            {
                inputBlock.Clear();
                inputBlock[0] = (ulong)pass;
                inputBlock[1] = (ulong)lane;
                inputBlock[2] = (ulong)slice;
                inputBlock[3] = (ulong)geometry.MemoryBlocks;   // m'
                inputBlock[4] = (ulong)geometry.Passes;
                inputBlock[5] = (ulong)(int)geometry.Type;
            }

            int startIndex = 0;
            if (pass == 0 && slice == 0)
            {
                startIndex = 2;   // the first two blocks are already filled
                if (dataIndependent)
                    NextAddresses<TKernel>(addressState, addressBlock, inputBlock);
            }

            int laneStart = lane * laneLength;
            int column = (slice * segmentLength) + startIndex;

            // Carry the previous block in the state instead of reading it back for every block.
            matrix.BlockSpan(laneStart + (column == 0 ? laneLength - 1 : column - 1)).CopyTo(state);

            // Version 0x10 always overwrites; version 0x13 XORs on subsequent passes.
            bool withXor = pass != 0 && geometry.Version != Argon2Parameters.Version10;

            for (int index = startIndex; index < segmentLength; index++, column++)
            {
                ulong pseudoRandom;
                if (dataIndependent)
                {
                    if (index % AddressesPerBlock == 0)
                        NextAddresses<TKernel>(addressState, addressBlock, inputBlock);
                    pseudoRandom = addressBlock[index % AddressesPerBlock];
                }
                else
                {
                    pseudoRandom = stateRef;   // the first word of the previous block
                }

                uint referenceLane = (pass == 0 && slice == 0)
                    ? (uint)lane
                    : (uint)((pseudoRandom >> 32) % (ulong)geometry.Lanes);

                int referenceIndex = ReferenceIndex(
                    pass,
                    slice,
                    index,
                    segmentLength,
                    laneLength,
                    (uint)(pseudoRandom & 0xFFFFFFFF),
                    referenceLane == (uint)lane);

                TKernel.FillBlock(
                    ref stateRef,
                    ref workRef,
                    ref matrix.Block(((int)referenceLane * laneLength) + referenceIndex),
                    ref matrix.Block(laneStart + column),
                    withXor);
            }
        }
        finally
        {
            CryptographyHelper.Clear(scratch);
        }
    }

    /// <summary>
    /// Regenerates the Argon2i address block: <c>address = G(ZERO, G(ZERO, input))</c> (RFC 9106, Section 3.4.1.2).
    /// </summary>
    /// <typeparam name="TKernel">The compression kernel.</typeparam>
    /// <param name="addressState">
    /// The address generator's two blocks of scratch: a carried block, cleared to <c>ZERO</c> before each compression,
    /// followed by its working copy.
    /// </param>
    /// <param name="addressBlock">The block that receives the regenerated pseudo-random addresses.</param>
    /// <param name="inputBlock">The counter input block; its counter word is incremented in place.</param>
    private static void NextAddresses<TKernel>(Span<ulong> addressState, Span<ulong> addressBlock, Span<ulong> inputBlock)
        where TKernel : struct, IArgon2Kernel
    {
        Span<ulong> carried = addressState[..WordsPerBlock];
        ref ulong stateRef = ref MemoryMarshal.GetReference(carried);
        ref ulong workRef = ref addressState[WordsPerBlock];
        ref ulong addressRef = ref MemoryMarshal.GetReference(addressBlock);

        inputBlock[6]++;

        // G(ZERO, Y) is a compression whose previous block is zero, so clearing the carried block is all it takes.
        carried.Clear();
        TKernel.FillBlock(ref stateRef, ref workRef, ref MemoryMarshal.GetReference(inputBlock), ref addressRef, withXor: false);

        carried.Clear();
        TKernel.FillBlock(ref stateRef, ref workRef, ref addressRef, ref addressRef, withXor: false);
    }

    /// <summary>
    /// Maps the pseudo-random value to a reference-block column within the same lane (RFC 9106, Section 3.4.2).
    /// </summary>
    /// <param name="pass">The zero-based pass index.</param>
    /// <param name="slice">The zero-based slice index.</param>
    /// <param name="index">The zero-based position within the current segment.</param>
    /// <param name="segmentLength">The number of blocks in each segment.</param>
    /// <param name="laneLength">The number of blocks in each lane.</param>
    /// <param name="j1">The low 32 bits of the pseudo-random value driving the mapping.</param>
    /// <param name="sameLane"><see langword="true" /> when the reference block is taken from the current lane.</param>
    /// <returns>The column index of the reference block within its lane.</returns>
    private static int ReferenceIndex(
        int pass, int slice, int index, int segmentLength, int laneLength, uint j1, bool sameLane)
    {
        long referenceAreaSize;
        if (pass == 0)
        {
            if (slice == 0)
            {
                referenceAreaSize = index - 1;
            }
            else
            {
                referenceAreaSize = sameLane
                    ? (slice * segmentLength) + index - 1
                    : (slice * segmentLength) + (index == 0 ? -1 : 0);
            }
        }
        else
        {
            referenceAreaSize = sameLane
                ? laneLength - segmentLength + index - 1
                : laneLength - segmentLength + (index == 0 ? -1 : 0);
        }

        // Nonuniform mapping: zz = areaSize - 1 - (areaSize * (j1^2 >> 32) >> 32).
        ulong relative = j1;
        relative = (relative * relative) >> 32;
        relative = (ulong)referenceAreaSize - 1 - (((ulong)referenceAreaSize * relative) >> 32);

        int startPosition = 0;
        if (pass != 0)
            startPosition = slice == SyncPoints - 1 ? 0 : (slice + 1) * segmentLength;

        return (int)(((ulong)startPosition + relative) % (ulong)laneLength);
    }
}
