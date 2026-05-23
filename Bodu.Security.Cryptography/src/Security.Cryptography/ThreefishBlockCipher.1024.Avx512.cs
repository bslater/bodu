// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.1024.Avx512.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// AVX-512 vectorised implementation of <see cref="Threefish1024Cipher" />. The sixteen 64-bit state words
/// are split across two <see cref="Vector512{T}" /> registers — <c>loVec</c> holds the eight even-position
/// words <c>(x0, x2, x4, x6, x8, x10, x12, x14)</c> and <c>hiVec</c> the eight odd-position words
/// <c>(x1, x3, x5, x7, x9, x11, x13, x15)</c>. Each round performs a vector add, a per-lane variable rotate
/// (<c>VPROLVQ</c>), an XOR, and a pair of single-source <c>VPERMQ</c> shuffles that realign the registers
/// for the next round's MIX pairing.
/// </summary>
/// <remarks>
/// <para>
/// The per-round word permutation has a uniform 4-round cycle: the same two lane-shuffle vectors realign
/// both registers for every inter-round transition within a group, and after four shuffles both registers
/// return to canonical layout — exactly where the subkey injection lands.
/// </para>
/// <para>
/// Gated on <see cref="Avx512F.IsSupported" /> directly because the implementation operates on
/// <see cref="Vector512{T}" /> rather than <see cref="Vector256{T}" />, so the Vector Length extensions are
/// not required.
/// </para>
/// </remarks>
public sealed partial class Threefish1024Cipher
{
    // Per-round rotation vectors. Each Vector512<ulong> packs eight spec-defined rotation amounts (the
    // eight MIX rotations for one round) at lane positions matching the hi register's lane layout when
    // the round executes.
    private static readonly Vector512<ulong> s_rotVec0 = Vector512.Create((ulong)R0, R1, R2, R3, R4, R5, R6, R7);
    private static readonly Vector512<ulong> s_rotVec1 = Vector512.Create((ulong)R8, R9, R10, R11, R12, R13, R14, R15);
    private static readonly Vector512<ulong> s_rotVec2 = Vector512.Create((ulong)R16, R17, R18, R19, R20, R21, R22, R23);
    private static readonly Vector512<ulong> s_rotVec3 = Vector512.Create((ulong)R24, R25, R26, R27, R28, R29, R30, R31);
    private static readonly Vector512<ulong> s_rotVec4 = Vector512.Create((ulong)R32, R33, R34, R35, R36, R37, R38, R39);
    private static readonly Vector512<ulong> s_rotVec5 = Vector512.Create((ulong)R40, R41, R42, R43, R44, R45, R46, R47);
    private static readonly Vector512<ulong> s_rotVec6 = Vector512.Create((ulong)R48, R49, R50, R51, R52, R53, R54, R55);
    private static readonly Vector512<ulong> s_rotVec7 = Vector512.Create((ulong)R56, R57, R58, R59, R60, R61, R62, R63);

    // Inter-round lane-shuffle vectors. Forward shuffle takes canonical lane order to the round-1 layout
    // (and applied four times in a row cycles back to canonical); inverse shuffle takes canonical back to
    // the round-3 post-MIX layout that Decrypt's first UNMIX operates on.
    private static readonly Vector512<ulong> s_loShuffleForward = Vector512.Create(0UL, 1, 3, 2, 5, 6, 7, 4);
    private static readonly Vector512<ulong> s_hiShuffleForward = Vector512.Create(4UL, 6, 5, 7, 3, 1, 2, 0);
    private static readonly Vector512<ulong> s_loShuffleInverse = Vector512.Create(0UL, 1, 3, 2, 7, 4, 5, 6);
    private static readonly Vector512<ulong> s_hiShuffleInverse = Vector512.Create(7UL, 5, 6, 4, 0, 2, 1, 3);

    // Deinterleave indices for the initial block load: separate the canonical-byte-order block into the
    // even-position loVec and odd-position hiVec via a single VPERMI2Q each (indices 0-7 select from the
    // first source vector, 8-15 from the second).
    private static readonly Vector512<ulong> s_evenWordIndices = Vector512.Create(0UL, 2, 4, 6, 8, 10, 12, 14);
    private static readonly Vector512<ulong> s_oddWordIndices = Vector512.Create(1UL, 3, 5, 7, 9, 11, 13, 15);

    // Reinterleave indices for the final block store: combine UnpackLow / UnpackHigh of (loVec, hiVec)
    // into the canonical-byte-order halves of the output block.
    private static readonly Vector512<ulong> s_outFirstHalfIndices = Vector512.Create(0UL, 1, 8, 9, 2, 3, 10, 11);
    private static readonly Vector512<ulong> s_outSecondHalfIndices = Vector512.Create(4UL, 5, 12, 13, 6, 7, 14, 15);

    /// <summary>
    /// Encrypts a single 128-byte block using the AVX-512 vectorised Threefish-1024 implementation.
    /// </summary>
    /// <param name="input">The 128-byte plaintext block. Caller is responsible for length validation.</param>
    /// <param name="output">The 128-byte buffer that receives the ciphertext block.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void EncryptAvx512(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // Load both halves of the plaintext block and deinterleave into the (lo, hi) lane layout.
        ref ulong wordRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(input));
        Vector512<ulong> vec0 = Vector512.LoadUnsafe(ref wordRef);
        Vector512<ulong> vec1 = Vector512.LoadUnsafe(ref wordRef, 8);
        Vector512<ulong> loVec = Avx512F.PermuteVar8x64x2(vec0, s_evenWordIndices, vec1);
        Vector512<ulong> hiVec = Avx512F.PermuteVar8x64x2(vec0, s_oddWordIndices, vec1);

        ref ulong keyRef = ref MemoryMarshal.GetArrayDataReference(this._keySchedule);
        ref ulong tweakRef = ref MemoryMarshal.GetArrayDataReference(this._tweakSchedule);

        // Initial subkey injection. Tweak applies at positions 13 (hiVec lane 6) and 14 (loVec lane 7).
        loVec += Vector512.Create(
            Unsafe.Add(ref keyRef, 0),
            Unsafe.Add(ref keyRef, 2),
            Unsafe.Add(ref keyRef, 4),
            Unsafe.Add(ref keyRef, 6),
            Unsafe.Add(ref keyRef, 8),
            Unsafe.Add(ref keyRef, 10),
            Unsafe.Add(ref keyRef, 12),
            Unsafe.Add(ref keyRef, 14) + Unsafe.Add(ref tweakRef, 1));
        hiVec += Vector512.Create(
            Unsafe.Add(ref keyRef, 1),
            Unsafe.Add(ref keyRef, 3),
            Unsafe.Add(ref keyRef, 5),
            Unsafe.Add(ref keyRef, 7),
            Unsafe.Add(ref keyRef, 9),
            Unsafe.Add(ref keyRef, 11),
            Unsafe.Add(ref keyRef, 13) + Unsafe.Add(ref tweakRef, 0),
            Unsafe.Add(ref keyRef, 15));

        for (var d = 1; d < 80 / 4; d += 2)
        {
            int dm17 = d % 17, dm3 = d % 3;

            // First 4 rounds (R0..R31). Four forward shuffles cycle the registers back to canonical layout.
            SimdRoundForward(ref loVec, ref hiVec, s_rotVec0);
            SimdRoundForward(ref loVec, ref hiVec, s_rotVec1);
            SimdRoundForward(ref loVec, ref hiVec, s_rotVec2);
            SimdRoundForward(ref loVec, ref hiVec, s_rotVec3);

            // Mid subkey injection at canonical layout.
            loVec += Vector512.Create(
                Unsafe.Add(ref keyRef, dm17),
                Unsafe.Add(ref keyRef, dm17 + 2),
                Unsafe.Add(ref keyRef, dm17 + 4),
                Unsafe.Add(ref keyRef, dm17 + 6),
                Unsafe.Add(ref keyRef, dm17 + 8),
                Unsafe.Add(ref keyRef, dm17 + 10),
                Unsafe.Add(ref keyRef, dm17 + 12),
                Unsafe.Add(ref keyRef, dm17 + 14) + Unsafe.Add(ref tweakRef, dm3 + 1));
            hiVec += Vector512.Create(
                Unsafe.Add(ref keyRef, dm17 + 1),
                Unsafe.Add(ref keyRef, dm17 + 3),
                Unsafe.Add(ref keyRef, dm17 + 5),
                Unsafe.Add(ref keyRef, dm17 + 7),
                Unsafe.Add(ref keyRef, dm17 + 9),
                Unsafe.Add(ref keyRef, dm17 + 11),
                Unsafe.Add(ref keyRef, dm17 + 13) + Unsafe.Add(ref tweakRef, dm3),
                Unsafe.Add(ref keyRef, dm17 + 15) + (ulong)d);

            // Second 4 rounds (R32..R63), again returning to canonical layout.
            SimdRoundForward(ref loVec, ref hiVec, s_rotVec4);
            SimdRoundForward(ref loVec, ref hiVec, s_rotVec5);
            SimdRoundForward(ref loVec, ref hiVec, s_rotVec6);
            SimdRoundForward(ref loVec, ref hiVec, s_rotVec7);

            // Post subkey injection at canonical layout.
            loVec += Vector512.Create(
                Unsafe.Add(ref keyRef, dm17 + 1),
                Unsafe.Add(ref keyRef, dm17 + 3),
                Unsafe.Add(ref keyRef, dm17 + 5),
                Unsafe.Add(ref keyRef, dm17 + 7),
                Unsafe.Add(ref keyRef, dm17 + 9),
                Unsafe.Add(ref keyRef, dm17 + 11),
                Unsafe.Add(ref keyRef, dm17 + 13),
                Unsafe.Add(ref keyRef, dm17 + 15) + Unsafe.Add(ref tweakRef, dm3 + 2));
            hiVec += Vector512.Create(
                Unsafe.Add(ref keyRef, dm17 + 2),
                Unsafe.Add(ref keyRef, dm17 + 4),
                Unsafe.Add(ref keyRef, dm17 + 6),
                Unsafe.Add(ref keyRef, dm17 + 8),
                Unsafe.Add(ref keyRef, dm17 + 10),
                Unsafe.Add(ref keyRef, dm17 + 12),
                Unsafe.Add(ref keyRef, dm17 + 14) + Unsafe.Add(ref tweakRef, dm3 + 1),
                Unsafe.Add(ref keyRef, dm17 + 16) + (ulong)(d + 1));
        }

        // Reinterleave (loVec, hiVec) back into canonical byte order and commit to the output buffer.
        StoreStateBlock(ref MemoryMarshal.GetReference(output), loVec, hiVec);
    }

    /// <summary>
    /// Decrypts a single 128-byte block using the AVX-512 vectorised Threefish-1024 implementation.
    /// </summary>
    /// <param name="input">The 128-byte ciphertext block. Caller is responsible for length validation.</param>
    /// <param name="output">The 128-byte buffer that receives the plaintext block.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void DecryptAvx512(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ref ulong wordRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(input));
        Vector512<ulong> vec0 = Vector512.LoadUnsafe(ref wordRef);
        Vector512<ulong> vec1 = Vector512.LoadUnsafe(ref wordRef, 8);
        Vector512<ulong> loVec = Avx512F.PermuteVar8x64x2(vec0, s_evenWordIndices, vec1);
        Vector512<ulong> hiVec = Avx512F.PermuteVar8x64x2(vec0, s_oddWordIndices, vec1);

        ref ulong keyRef = ref MemoryMarshal.GetArrayDataReference(this._keySchedule);
        ref ulong tweakRef = ref MemoryMarshal.GetArrayDataReference(this._tweakSchedule);

        for (var d = (80 / 4) - 1; d >= 1; d -= 2)
        {
            int dm17 = d % 17, dm3 = d % 3;

            // Reverse the post subkey injection.
            loVec -= Vector512.Create(
                Unsafe.Add(ref keyRef, dm17 + 1),
                Unsafe.Add(ref keyRef, dm17 + 3),
                Unsafe.Add(ref keyRef, dm17 + 5),
                Unsafe.Add(ref keyRef, dm17 + 7),
                Unsafe.Add(ref keyRef, dm17 + 9),
                Unsafe.Add(ref keyRef, dm17 + 11),
                Unsafe.Add(ref keyRef, dm17 + 13),
                Unsafe.Add(ref keyRef, dm17 + 15) + Unsafe.Add(ref tweakRef, dm3 + 2));
            hiVec -= Vector512.Create(
                Unsafe.Add(ref keyRef, dm17 + 2),
                Unsafe.Add(ref keyRef, dm17 + 4),
                Unsafe.Add(ref keyRef, dm17 + 6),
                Unsafe.Add(ref keyRef, dm17 + 8),
                Unsafe.Add(ref keyRef, dm17 + 10),
                Unsafe.Add(ref keyRef, dm17 + 12),
                Unsafe.Add(ref keyRef, dm17 + 14) + Unsafe.Add(ref tweakRef, dm3 + 1),
                Unsafe.Add(ref keyRef, dm17 + 16) + (ulong)(d + 1));

            // Reverse the second 4 rounds (rotations R56..R63, R48..R55, R40..R47, R32..R39).
            SimdRoundInverse(ref loVec, ref hiVec, s_rotVec7);
            SimdRoundInverse(ref loVec, ref hiVec, s_rotVec6);
            SimdRoundInverse(ref loVec, ref hiVec, s_rotVec5);
            SimdRoundInverse(ref loVec, ref hiVec, s_rotVec4);

            // Reverse the mid subkey injection.
            loVec -= Vector512.Create(
                Unsafe.Add(ref keyRef, dm17),
                Unsafe.Add(ref keyRef, dm17 + 2),
                Unsafe.Add(ref keyRef, dm17 + 4),
                Unsafe.Add(ref keyRef, dm17 + 6),
                Unsafe.Add(ref keyRef, dm17 + 8),
                Unsafe.Add(ref keyRef, dm17 + 10),
                Unsafe.Add(ref keyRef, dm17 + 12),
                Unsafe.Add(ref keyRef, dm17 + 14) + Unsafe.Add(ref tweakRef, dm3 + 1));
            hiVec -= Vector512.Create(
                Unsafe.Add(ref keyRef, dm17 + 1),
                Unsafe.Add(ref keyRef, dm17 + 3),
                Unsafe.Add(ref keyRef, dm17 + 5),
                Unsafe.Add(ref keyRef, dm17 + 7),
                Unsafe.Add(ref keyRef, dm17 + 9),
                Unsafe.Add(ref keyRef, dm17 + 11),
                Unsafe.Add(ref keyRef, dm17 + 13) + Unsafe.Add(ref tweakRef, dm3),
                Unsafe.Add(ref keyRef, dm17 + 15) + (ulong)d);

            // Reverse the first 4 rounds (rotations R24..R31, R16..R23, R8..R15, R0..R7).
            SimdRoundInverse(ref loVec, ref hiVec, s_rotVec3);
            SimdRoundInverse(ref loVec, ref hiVec, s_rotVec2);
            SimdRoundInverse(ref loVec, ref hiVec, s_rotVec1);
            SimdRoundInverse(ref loVec, ref hiVec, s_rotVec0);
        }

        // Reverse the initial subkey injection.
        loVec -= Vector512.Create(
            Unsafe.Add(ref keyRef, 0),
            Unsafe.Add(ref keyRef, 2),
            Unsafe.Add(ref keyRef, 4),
            Unsafe.Add(ref keyRef, 6),
            Unsafe.Add(ref keyRef, 8),
            Unsafe.Add(ref keyRef, 10),
            Unsafe.Add(ref keyRef, 12),
            Unsafe.Add(ref keyRef, 14) + Unsafe.Add(ref tweakRef, 1));
        hiVec -= Vector512.Create(
            Unsafe.Add(ref keyRef, 1),
            Unsafe.Add(ref keyRef, 3),
            Unsafe.Add(ref keyRef, 5),
            Unsafe.Add(ref keyRef, 7),
            Unsafe.Add(ref keyRef, 9),
            Unsafe.Add(ref keyRef, 11),
            Unsafe.Add(ref keyRef, 13) + Unsafe.Add(ref tweakRef, 0),
            Unsafe.Add(ref keyRef, 15));

        StoreStateBlock(ref MemoryMarshal.GetReference(output), loVec, hiVec);
    }

    /// <summary>
    /// Executes one forward Threefish-1024 round on the vectorised state and applies the inter-round
    /// shuffle that realigns the registers for the next round's MIX pairing.
    /// </summary>
    /// <param name="loVec">The even-position state register, updated in place.</param>
    /// <param name="hiVec">The odd-position state register, updated in place.</param>
    /// <param name="rotation">The eight per-lane rotation amounts for this round.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SimdRoundForward(ref Vector512<ulong> loVec, ref Vector512<ulong> hiVec, Vector512<ulong> rotation)
    {
        loVec += hiVec;
        hiVec = Avx512F.RotateLeftVariable(hiVec, rotation) ^ loVec;
        loVec = Avx512F.PermuteVar8x64(loVec, s_loShuffleForward);
        hiVec = Avx512F.PermuteVar8x64(hiVec, s_hiShuffleForward);
    }

    /// <summary>
    /// Executes one inverse Threefish-1024 round on the vectorised state. The inverse shuffle runs before
    /// the UNMIX so it undoes the previous forward shuffle, restoring the layout the original MIX operated
    /// on.
    /// </summary>
    /// <param name="loVec">The even-position state register, updated in place.</param>
    /// <param name="hiVec">The odd-position state register, updated in place.</param>
    /// <param name="rotation">The eight per-lane rotation amounts originally applied in the forward round.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SimdRoundInverse(ref Vector512<ulong> loVec, ref Vector512<ulong> hiVec, Vector512<ulong> rotation)
    {
        loVec = Avx512F.PermuteVar8x64(loVec, s_loShuffleInverse);
        hiVec = Avx512F.PermuteVar8x64(hiVec, s_hiShuffleInverse);
        hiVec ^= loVec;
        hiVec = Avx512F.RotateRightVariable(hiVec, rotation);
        loVec -= hiVec;
    }

    /// <summary>
    /// Reinterleaves the <c>(loVec, hiVec)</c> vectorised state back into the canonical sixteen-word byte
    /// order and writes it to the destination buffer as two unaligned 512-bit stores.
    /// </summary>
    /// <param name="destination">A reference to the first byte of the destination buffer.</param>
    /// <param name="loVec">The even-position state register.</param>
    /// <param name="hiVec">The odd-position state register.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StoreStateBlock(ref byte destination, Vector512<ulong> loVec, Vector512<ulong> hiVec)
    {
        // UnpackLow / UnpackHigh on Vector512 interleave within each 128-bit lane, producing:
        //   unLow  = (x0, x1, x4, x5, x8, x9, x12, x13)
        //   unHigh = (x2, x3, x6, x7, x10, x11, x14, x15)
        // PermuteVar8x64x2 then combines them into the natural (x0..x7) and (x8..x15) halves.
        Vector512<ulong> unLow = Avx512F.UnpackLow(loVec, hiVec);
        Vector512<ulong> unHigh = Avx512F.UnpackHigh(loVec, hiVec);
        Vector512<ulong> outFirst = Avx512F.PermuteVar8x64x2(unLow, s_outFirstHalfIndices, unHigh);
        Vector512<ulong> outSecond = Avx512F.PermuteVar8x64x2(unLow, s_outSecondHalfIndices, unHigh);

        ref ulong outRef = ref Unsafe.As<byte, ulong>(ref destination);
        outFirst.StoreUnsafe(ref outRef);
        outSecond.StoreUnsafe(ref outRef, 8);
    }
}
