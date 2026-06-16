// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.512.Avx512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// AVX-512 vectorised implementation of <see cref="Threefish512Cipher" />. The eight 64-bit state words are split
/// across two <see cref="Vector256{T}" /> registers — <c>lo</c> holds the even-position words <c>(x0, x2, x4, x6)</c>
/// and <c>hi</c> the odd-position words <c>(x1, x3, x5, x7)</c> — and each round applies a vector add, a per-lane
/// variable rotate (<c>VPROLVQ</c>), an XOR, and a pair of lane shuffles that realign the registers for the next
/// round's MIX pairing.
/// </summary>
/// <remarks>
/// <para>
/// The per-round word permutation in Threefish-512 has a 4-round cycle: between rounds, the even-position register
/// rotates its four lanes left by one (<c>VPERMQ</c> with control <c>0x39</c>) and the odd-position register swaps
/// lanes 1 and 3 (<c>VPERMQ</c> with control <c>0x6C</c>). After four such shuffles both registers return to canonical
/// layout, which is exactly the layout the subkey injection expects.
/// </para>
/// <para>
/// Gated on <see cref="Avx512F.VL.IsSupported" /> rather than <see cref="Avx512F.IsSupported" /> because the variable
/// rotate is invoked on <see cref="Vector256{T}" /> rather than <see cref="Vector512{T}" />, which requires the AVX-512
/// Vector Length extensions. AVX-512VL has been bundled with AVX-512F on every mainstream Intel/AMD CPU shipping it
/// (the only AVX-512F-without-VL parts were the discontinued Knights Landing / Knights Mill server processors).
/// </para>
/// </remarks>
public sealed partial class Threefish512Cipher
{
    // Per-round rotation vectors. Each Vector256<ulong> packs four spec-defined rotation amounts (the four
    // MIX rotations for one round) at lane positions matching the hi register's lane layout when the round
    // executes.
    private static readonly Vector256<ulong> s_rotVec0 = Vector256.Create((ulong)R0, R1, R2, R3);
    private static readonly Vector256<ulong> s_rotVec1 = Vector256.Create((ulong)R4, R5, R6, R7);
    private static readonly Vector256<ulong> s_rotVec2 = Vector256.Create((ulong)R8, R9, R10, R11);
    private static readonly Vector256<ulong> s_rotVec3 = Vector256.Create((ulong)R12, R13, R14, R15);
    private static readonly Vector256<ulong> s_rotVec4 = Vector256.Create((ulong)R16, R17, R18, R19);
    private static readonly Vector256<ulong> s_rotVec5 = Vector256.Create((ulong)R20, R21, R22, R23);
    private static readonly Vector256<ulong> s_rotVec6 = Vector256.Create((ulong)R24, R25, R26, R27);
    private static readonly Vector256<ulong> s_rotVec7 = Vector256.Create((ulong)R28, R29, R30, R31);

    // VPERMQ control bytes used by the inter-round shuffle. ShuffleLoForward rotates lo's four lanes left
    // by one position; ShuffleHiSwap13 swaps hi's lanes 1 and 3 (an involution, so the same byte serves
    // both the forward and inverse direction). ShuffleLoInverse rotates lo's lanes right by one for the
    // Decrypt pass.
    private const byte ShuffleLoForward = 0x39;  // lanes (1, 2, 3, 0) — left-rotate by 1
    private const byte ShuffleLoInverse = 0x93;  // lanes (3, 0, 1, 2) — right-rotate by 1
    private const byte ShuffleHiSwap13 = 0x6C;   // lanes (0, 3, 2, 1) — swap 1 and 3 (self-inverse)

    /// <summary>
    /// Encrypts a single 64-byte block using the AVX-512 vectorised Threefish-512 implementation.
    /// </summary>
    /// <param name="input">The 64-byte plaintext block. Caller is responsible for length validation.</param>
    /// <param name="output">The 64-byte buffer that receives the ciphertext block.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void EncryptAvx512(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // Load the plaintext block as eight 64-bit words held across two Vector256 registers, then
        // deinterleave into lo = (x0, x2, x4, x6) and hi = (x1, x3, x5, x7).
        ref ulong wordRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(input));
        var low4 = Vector256.LoadUnsafe(ref wordRef);
        var high4 = Vector256.LoadUnsafe(ref wordRef, 4);
        Vector256<ulong> lo = Avx2.Permute4x64(Avx2.UnpackLow(low4, high4), 0xD8);
        Vector256<ulong> hi = Avx2.Permute4x64(Avx2.UnpackHigh(low4, high4), 0xD8);

        ref ulong keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref ulong tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        // Initial subkey injection. In canonical layout, lo lanes correspond to state positions
        // (0, 2, 4, 6) and hi lanes to (1, 3, 5, 7); the tweak applies at positions 5 and 6.
        lo += Vector256.Create(
            Unsafe.Add(ref keyRef, 0),
            Unsafe.Add(ref keyRef, 2),
            Unsafe.Add(ref keyRef, 4),
            Unsafe.Add(ref keyRef, 6) + Unsafe.Add(ref tweakRef, 1));
        hi += Vector256.Create(
            Unsafe.Add(ref keyRef, 1),
            Unsafe.Add(ref keyRef, 3),
            Unsafe.Add(ref keyRef, 5) + Unsafe.Add(ref tweakRef, 0),
            Unsafe.Add(ref keyRef, 7));

        for (int d = 1; d < 72 / 4; d += 2)
        {
            int dm9 = d % 9, dm3 = d % 3;

            // First 4 rounds (R0..R15). After four forward shuffles both registers cycle back to
            // canonical layout, where the mid subkey injection lands.
            SimdRoundForward(ref lo, ref hi, s_rotVec0);
            SimdRoundForward(ref lo, ref hi, s_rotVec1);
            SimdRoundForward(ref lo, ref hi, s_rotVec2);
            SimdRoundForward(ref lo, ref hi, s_rotVec3);

            // Mid subkey injection at canonical layout.
            lo += Vector256.Create(
                Unsafe.Add(ref keyRef, dm9),
                Unsafe.Add(ref keyRef, dm9 + 2),
                Unsafe.Add(ref keyRef, dm9 + 4),
                Unsafe.Add(ref keyRef, dm9 + 6) + Unsafe.Add(ref tweakRef, dm3 + 1));
            hi += Vector256.Create(
                Unsafe.Add(ref keyRef, dm9 + 1),
                Unsafe.Add(ref keyRef, dm9 + 3),
                Unsafe.Add(ref keyRef, dm9 + 5) + Unsafe.Add(ref tweakRef, dm3),
                Unsafe.Add(ref keyRef, dm9 + 7) + (ulong)d);

            // Second 4 rounds (R16..R31), again returning to canonical layout.
            SimdRoundForward(ref lo, ref hi, s_rotVec4);
            SimdRoundForward(ref lo, ref hi, s_rotVec5);
            SimdRoundForward(ref lo, ref hi, s_rotVec6);
            SimdRoundForward(ref lo, ref hi, s_rotVec7);

            // Post subkey injection at canonical layout.
            lo += Vector256.Create(
                Unsafe.Add(ref keyRef, dm9 + 1),
                Unsafe.Add(ref keyRef, dm9 + 3),
                Unsafe.Add(ref keyRef, dm9 + 5),
                Unsafe.Add(ref keyRef, dm9 + 7) + Unsafe.Add(ref tweakRef, dm3 + 2));
            hi += Vector256.Create(
                Unsafe.Add(ref keyRef, dm9 + 2),
                Unsafe.Add(ref keyRef, dm9 + 4),
                Unsafe.Add(ref keyRef, dm9 + 6) + Unsafe.Add(ref tweakRef, dm3 + 1),
                Unsafe.Add(ref keyRef, dm9 + 8) + (ulong)(d + 1));
        }

        // Reinterleave (lo, hi) back into byte-order halves and commit to the output buffer.
        StoreStateBlock(ref MemoryMarshal.GetReference(output), lo, hi);
    }

    /// <summary>
    /// Decrypts a single 64-byte block using the AVX-512 vectorised Threefish-512 implementation.
    /// </summary>
    /// <param name="input">The 64-byte ciphertext block. Caller is responsible for length validation.</param>
    /// <param name="output">The 64-byte buffer that receives the plaintext block.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void DecryptAvx512(ReadOnlySpan<byte> input, Span<byte> output)
    {
        // Load the ciphertext block in the same canonical layout the Encrypt path produces.
        ref ulong wordRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(input));
        var low4 = Vector256.LoadUnsafe(ref wordRef);
        var high4 = Vector256.LoadUnsafe(ref wordRef, 4);
        Vector256<ulong> lo = Avx2.Permute4x64(Avx2.UnpackLow(low4, high4), 0xD8);
        Vector256<ulong> hi = Avx2.Permute4x64(Avx2.UnpackHigh(low4, high4), 0xD8);

        ref ulong keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref ulong tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        // Walk the subkey-injection / round groups in reverse. Each inverse-round applies the inverse
        // shuffle first to undo the previous forward shuffle, then UNMIX with the rotation that round
        // used originally — so the loop body mirrors EncryptAvx512 in reverse order.
        for (int d = (72 / 4) - 1; d >= 1; d -= 2)
        {
            int dm9 = d % 9, dm3 = d % 3;

            // Reverse the post subkey injection.
            lo -= Vector256.Create(
                Unsafe.Add(ref keyRef, dm9 + 1),
                Unsafe.Add(ref keyRef, dm9 + 3),
                Unsafe.Add(ref keyRef, dm9 + 5),
                Unsafe.Add(ref keyRef, dm9 + 7) + Unsafe.Add(ref tweakRef, dm3 + 2));
            hi -= Vector256.Create(
                Unsafe.Add(ref keyRef, dm9 + 2),
                Unsafe.Add(ref keyRef, dm9 + 4),
                Unsafe.Add(ref keyRef, dm9 + 6) + Unsafe.Add(ref tweakRef, dm3 + 1),
                Unsafe.Add(ref keyRef, dm9 + 8) + (ulong)(d + 1));

            // Reverse the second 4 rounds (rotations R28..R31, R24..R27, R20..R23, R16..R19) — four
            // inverse shuffles cycle the registers back through layouts L3, L2, L1, L0 = canonical.
            SimdRoundInverse(ref lo, ref hi, s_rotVec7);
            SimdRoundInverse(ref lo, ref hi, s_rotVec6);
            SimdRoundInverse(ref lo, ref hi, s_rotVec5);
            SimdRoundInverse(ref lo, ref hi, s_rotVec4);

            // Reverse the mid subkey injection.
            lo -= Vector256.Create(
                Unsafe.Add(ref keyRef, dm9),
                Unsafe.Add(ref keyRef, dm9 + 2),
                Unsafe.Add(ref keyRef, dm9 + 4),
                Unsafe.Add(ref keyRef, dm9 + 6) + Unsafe.Add(ref tweakRef, dm3 + 1));
            hi -= Vector256.Create(
                Unsafe.Add(ref keyRef, dm9 + 1),
                Unsafe.Add(ref keyRef, dm9 + 3),
                Unsafe.Add(ref keyRef, dm9 + 5) + Unsafe.Add(ref tweakRef, dm3),
                Unsafe.Add(ref keyRef, dm9 + 7) + (ulong)d);

            // Reverse the first 4 rounds (rotations R12..R15, R8..R11, R4..R7, R0..R3).
            SimdRoundInverse(ref lo, ref hi, s_rotVec3);
            SimdRoundInverse(ref lo, ref hi, s_rotVec2);
            SimdRoundInverse(ref lo, ref hi, s_rotVec1);
            SimdRoundInverse(ref lo, ref hi, s_rotVec0);
        }

        // Reverse the initial subkey injection.
        lo -= Vector256.Create(
            Unsafe.Add(ref keyRef, 0),
            Unsafe.Add(ref keyRef, 2),
            Unsafe.Add(ref keyRef, 4),
            Unsafe.Add(ref keyRef, 6) + Unsafe.Add(ref tweakRef, 1));
        hi -= Vector256.Create(
            Unsafe.Add(ref keyRef, 1),
            Unsafe.Add(ref keyRef, 3),
            Unsafe.Add(ref keyRef, 5) + Unsafe.Add(ref tweakRef, 0),
            Unsafe.Add(ref keyRef, 7));

        StoreStateBlock(ref MemoryMarshal.GetReference(output), lo, hi);
    }

    /// <summary>
    /// Executes one forward Threefish-512 round on the vectorised state and applies the inter-round shuffle that
    /// realigns the registers for the next round's MIX pairing.
    /// </summary>
    /// <param name="lo">The even-position state register, updated in place.</param>
    /// <param name="hi">The odd-position state register, updated in place.</param>
    /// <param name="rotation">The four per-lane rotation amounts for this round, packed into a Vector256.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SimdRoundForward(ref Vector256<ulong> lo, ref Vector256<ulong> hi, Vector256<ulong> rotation)
    {
        lo += hi;
        hi = Avx512F.VL.RotateLeftVariable(hi, rotation) ^ lo;
        lo = Avx2.Permute4x64(lo, ShuffleLoForward);
        hi = Avx2.Permute4x64(hi, ShuffleHiSwap13);
    }

    /// <summary>
    /// Executes one inverse Threefish-512 round on the vectorised state. The inverse shuffle is applied before the
    /// UNMIX so it undoes the previous forward shuffle, restoring the layout that the original MIX operated on.
    /// </summary>
    /// <param name="lo">The even-position state register, updated in place.</param>
    /// <param name="hi">The odd-position state register, updated in place.</param>
    /// <param name="rotation">The four per-lane rotation amounts originally applied in the forward round.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SimdRoundInverse(ref Vector256<ulong> lo, ref Vector256<ulong> hi, Vector256<ulong> rotation)
    {
        lo = Avx2.Permute4x64(lo, ShuffleLoInverse);
        hi = Avx2.Permute4x64(hi, ShuffleHiSwap13);
        hi ^= lo;
        hi = Avx512F.VL.RotateRightVariable(hi, rotation);
        lo -= hi;
    }

    /// <summary>
    /// Reinterleaves the <c>(lo, hi)</c> vectorised state back into the canonical eight-word byte order and writes it
    /// to the destination buffer as two unaligned 256-bit stores.
    /// </summary>
    /// <param name="destination">A reference to the first byte of the destination buffer.</param>
    /// <param name="lo">The even-position state register holding <c>(x0, x2, x4, x6)</c>.</param>
    /// <param name="hi">The odd-position state register holding <c>(x1, x3, x5, x7)</c>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StoreStateBlock(ref byte destination, Vector256<ulong> lo, Vector256<ulong> hi)
    {
        // UnpackLow/High split the registers into per-128-bit-lane interleavings; Permute2x128 then
        // recombines the lower-half and upper-half pieces into the natural (x0..x3) and (x4..x7) order.
        Vector256<ulong> unLow = Avx2.UnpackLow(lo, hi);       // (x0, x1, x4, x5)
        Vector256<ulong> unHigh = Avx2.UnpackHigh(lo, hi);     // (x2, x3, x6, x7)
        Vector256<ulong> outLow4 = Avx2.Permute2x128(unLow, unHigh, 0x20);   // (x0, x1, x2, x3)
        Vector256<ulong> outHigh4 = Avx2.Permute2x128(unLow, unHigh, 0x31);  // (x4, x5, x6, x7)

        ref ulong outRef = ref Unsafe.As<byte, ulong>(ref destination);
        outLow4.StoreUnsafe(ref outRef);
        outHigh4.StoreUnsafe(ref outRef, 4);
    }
}
