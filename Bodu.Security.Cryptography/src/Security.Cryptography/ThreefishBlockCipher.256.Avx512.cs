// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishBlockCipher.256.Avx512.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// AVX-512 vectorised implementation of <see cref="Threefish256Cipher" />. The four 64-bit state words are split across
/// two <see cref="Vector128{T}" /> registers — <c>lo</c> holds the even-position words <c>(b0, b2)</c> and <c>hi</c>
/// the odd-position words <c>(b1, b3)</c>. Each round performs a vector add, a per-lane variable rotate (<c>VPROLVQ</c>
/// ), an XOR, and a single 64-bit lane swap on <c>hi</c> that realigns it for the next round's MIX pairing.
/// </summary>
/// <remarks>
/// <para>
/// Threefish-256 alternates between two MIX pair patterns within a 4-round group: <c>(0,1)(2,3)</c> at rounds 0 and 2,
/// and <c>(0,3)(2,1)</c> at rounds 1 and 3. With the state split into even/odd halves, the alternation reduces to a
/// single swap of <c>hi</c>'s two lanes — applied four times in a row, the swaps cycle back to canonical layout, which
/// is exactly where the subkey injection lands.
/// </para>
/// <para>
/// Gated on <see cref="Avx512F.VL.IsSupported" /> because the per-lane variable rotate runs on
/// <see cref="Vector128{T}" />. SIMD gain on Threefish-256 is the smallest of the three variants — the 128-bit working
/// width matches scalar register count and the per-instruction overhead is high relative to the work — but the
/// implementation keeps the family pattern consistent and provides a measured reduction in scalar instruction count per
/// round.
/// </para>
/// </remarks>
public sealed partial class Threefish256Cipher
{
    // Per-round rotation vectors. Each Vector128<ulong> packs the two MIX rotations for one round at
    // lane positions matching the hi register's lane layout when the round executes.
    private static readonly Vector128<ulong> s_rotVec0 = Vector128.Create((ulong)R0, R1);
    private static readonly Vector128<ulong> s_rotVec1 = Vector128.Create((ulong)R2, R3);
    private static readonly Vector128<ulong> s_rotVec2 = Vector128.Create((ulong)R4, R5);
    private static readonly Vector128<ulong> s_rotVec3 = Vector128.Create((ulong)R6, R7);
    private static readonly Vector128<ulong> s_rotVec4 = Vector128.Create((ulong)R8, R9);
    private static readonly Vector128<ulong> s_rotVec5 = Vector128.Create((ulong)R10, R11);
    private static readonly Vector128<ulong> s_rotVec6 = Vector128.Create((ulong)R12, R13);
    private static readonly Vector128<ulong> s_rotVec7 = Vector128.Create((ulong)R14, R15);

    // Lane-swap index vector: swaps the two 64-bit lanes of a Vector128<ulong>. The swap is its own
    // inverse, so the same vector serves both the forward (Encrypt) and inverse (Decrypt) directions.
    private static readonly Vector128<ulong> s_hiSwapIndices = Vector128.Create(1UL, 0);

    /// <summary>
    /// Encrypts a single 32-byte block using the AVX-512 vectorised Threefish-256 implementation.
    /// </summary>
    /// <param name="input">The 32-byte plaintext block. Caller is responsible for length validation.</param>
    /// <param name="output">The 32-byte buffer that receives the ciphertext block.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void EncryptAvx512(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ref var wordRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(input));
        var b0 = Unsafe.Add(ref wordRef, 0);
        var b1 = Unsafe.Add(ref wordRef, 1);
        var b2 = Unsafe.Add(ref wordRef, 2);
        var b3 = Unsafe.Add(ref wordRef, 3);
        var lo = Vector128.Create(b0, b2);
        var hi = Vector128.Create(b1, b3);

        ref var keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref var tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        // Initial subkey injection. Tweak applies at positions 1 (hi lane 0) and 2 (lo lane 1).
        lo += Vector128.Create(
            Unsafe.Add(ref keyRef, 0),
            Unsafe.Add(ref keyRef, 2) + Unsafe.Add(ref tweakRef, 1));
        hi += Vector128.Create(
            Unsafe.Add(ref keyRef, 1) + Unsafe.Add(ref tweakRef, 0),
            Unsafe.Add(ref keyRef, 3));

        for (var d = 1; d < 72 / 4; d += 2)
        {
            var dm5 = d % 5;
            var dm3 = d % 3;

            // First 4 rounds (R0..R7). Four hi-swaps cycle the layout back to canonical.
            SimdRoundForward(ref lo, ref hi, s_rotVec0);
            SimdRoundForward(ref lo, ref hi, s_rotVec1);
            SimdRoundForward(ref lo, ref hi, s_rotVec2);
            SimdRoundForward(ref lo, ref hi, s_rotVec3);

            // Mid subkey injection at canonical layout.
            lo += Vector128.Create(
                Unsafe.Add(ref keyRef, dm5),
                Unsafe.Add(ref keyRef, dm5 + 2) + Unsafe.Add(ref tweakRef, dm3 + 1));
            hi += Vector128.Create(
                Unsafe.Add(ref keyRef, dm5 + 1) + Unsafe.Add(ref tweakRef, dm3),
                Unsafe.Add(ref keyRef, dm5 + 3) + (ulong)d);

            // Second 4 rounds (R8..R15), again returning to canonical layout.
            SimdRoundForward(ref lo, ref hi, s_rotVec4);
            SimdRoundForward(ref lo, ref hi, s_rotVec5);
            SimdRoundForward(ref lo, ref hi, s_rotVec6);
            SimdRoundForward(ref lo, ref hi, s_rotVec7);

            // Post subkey injection at canonical layout.
            lo += Vector128.Create(
                Unsafe.Add(ref keyRef, dm5 + 1),
                Unsafe.Add(ref keyRef, dm5 + 3) + Unsafe.Add(ref tweakRef, dm3 + 2));
            hi += Vector128.Create(
                Unsafe.Add(ref keyRef, dm5 + 2) + Unsafe.Add(ref tweakRef, dm3 + 1),
                Unsafe.Add(ref keyRef, dm5 + 4) + (ulong)d + 1);
        }

        ref var outWordRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(output));
        Unsafe.Add(ref outWordRef, 0) = lo.GetElement(0);
        Unsafe.Add(ref outWordRef, 1) = hi.GetElement(0);
        Unsafe.Add(ref outWordRef, 2) = lo.GetElement(1);
        Unsafe.Add(ref outWordRef, 3) = hi.GetElement(1);
    }

    /// <summary>
    /// Decrypts a single 32-byte block using the AVX-512 vectorised Threefish-256 implementation.
    /// </summary>
    /// <param name="input">The 32-byte ciphertext block. Caller is responsible for length validation.</param>
    /// <param name="output">The 32-byte buffer that receives the plaintext block.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private void DecryptAvx512(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ref var wordRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(input));
        var b0 = Unsafe.Add(ref wordRef, 0);
        var b1 = Unsafe.Add(ref wordRef, 1);
        var b2 = Unsafe.Add(ref wordRef, 2);
        var b3 = Unsafe.Add(ref wordRef, 3);
        var lo = Vector128.Create(b0, b2);
        var hi = Vector128.Create(b1, b3);

        ref var keyRef = ref MemoryMarshal.GetArrayDataReference(_keySchedule);
        ref var tweakRef = ref MemoryMarshal.GetArrayDataReference(_tweakSchedule);

        for (var d = (72 / 4) - 1; d >= 1; d -= 2)
        {
            var dm5 = d % 5;
            var dm3 = d % 3;

            // Reverse the post subkey injection.
            lo -= Vector128.Create(
                Unsafe.Add(ref keyRef, dm5 + 1),
                Unsafe.Add(ref keyRef, dm5 + 3) + Unsafe.Add(ref tweakRef, dm3 + 2));
            hi -= Vector128.Create(
                Unsafe.Add(ref keyRef, dm5 + 2) + Unsafe.Add(ref tweakRef, dm3 + 1),
                Unsafe.Add(ref keyRef, dm5 + 4) + (ulong)d + 1);

            // Reverse the second 4 rounds (R14..R15, R12..R13, R10..R11, R8..R9).
            SimdRoundInverse(ref lo, ref hi, s_rotVec7);
            SimdRoundInverse(ref lo, ref hi, s_rotVec6);
            SimdRoundInverse(ref lo, ref hi, s_rotVec5);
            SimdRoundInverse(ref lo, ref hi, s_rotVec4);

            // Reverse the mid subkey injection.
            lo -= Vector128.Create(
                Unsafe.Add(ref keyRef, dm5),
                Unsafe.Add(ref keyRef, dm5 + 2) + Unsafe.Add(ref tweakRef, dm3 + 1));
            hi -= Vector128.Create(
                Unsafe.Add(ref keyRef, dm5 + 1) + Unsafe.Add(ref tweakRef, dm3),
                Unsafe.Add(ref keyRef, dm5 + 3) + (ulong)d);

            // Reverse the first 4 rounds (R6..R7, R4..R5, R2..R3, R0..R1).
            SimdRoundInverse(ref lo, ref hi, s_rotVec3);
            SimdRoundInverse(ref lo, ref hi, s_rotVec2);
            SimdRoundInverse(ref lo, ref hi, s_rotVec1);
            SimdRoundInverse(ref lo, ref hi, s_rotVec0);
        }

        // Reverse the initial subkey injection.
        lo -= Vector128.Create(
            Unsafe.Add(ref keyRef, 0),
            Unsafe.Add(ref keyRef, 2) + Unsafe.Add(ref tweakRef, 1));
        hi -= Vector128.Create(
            Unsafe.Add(ref keyRef, 1) + Unsafe.Add(ref tweakRef, 0),
            Unsafe.Add(ref keyRef, 3));

        ref var outWordRef = ref Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(output));
        Unsafe.Add(ref outWordRef, 0) = lo.GetElement(0);
        Unsafe.Add(ref outWordRef, 1) = hi.GetElement(0);
        Unsafe.Add(ref outWordRef, 2) = lo.GetElement(1);
        Unsafe.Add(ref outWordRef, 3) = hi.GetElement(1);
    }

    /// <summary>
    /// Executes one forward Threefish-256 round on the vectorised state and applies the inter-round lane swap that
    /// realigns <c>hi</c> for the next round's MIX pairing.
    /// </summary>
    /// <param name="lo">The even-position state register, updated in place.</param>
    /// <param name="hi">The odd-position state register, updated in place.</param>
    /// <param name="rotation">The two per-lane rotation amounts for this round, packed into a Vector128.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SimdRoundForward(ref Vector128<ulong> lo, ref Vector128<ulong> hi, Vector128<ulong> rotation)
    {
        lo += hi;
        hi = Avx512F.VL.RotateLeftVariable(hi, rotation) ^ lo;
        hi = Vector128.Shuffle(hi, s_hiSwapIndices);
    }

    /// <summary>
    /// Executes one inverse Threefish-256 round on the vectorised state. The inverse lane swap is applied before the
    /// UNMIX so it undoes the previous forward swap, restoring the layout the original MIX operated on.
    /// </summary>
    /// <param name="lo">The even-position state register, updated in place.</param>
    /// <param name="hi">The odd-position state register, updated in place.</param>
    /// <param name="rotation">The two per-lane rotation amounts originally applied in the forward round.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SimdRoundInverse(ref Vector128<ulong> lo, ref Vector128<ulong> hi, Vector128<ulong> rotation)
    {
        hi = Vector128.Shuffle(hi, s_hiSwapIndices);
        hi ^= lo;
        hi = Avx512F.VL.RotateRightVariable(hi, rotation);
        lo -= hi;
    }
}
