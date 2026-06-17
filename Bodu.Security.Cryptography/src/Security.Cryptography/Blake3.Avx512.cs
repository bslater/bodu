// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3.Avx512.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// AVX-512 vectorised implementation of the BLAKE3 compression function. The 16-word working state is laid out as four
/// <see cref="Vector128{T}" /> rows (<c>a, b, c, d</c>), so the four parallel <c>G</c> calls in each column step
/// collapse into a single SIMD <c>G</c> operation. The diagonal step reuses the same kernel by shifting the
/// <c>b, c, d</c> rows by 1/2/3 lanes via <c>PSHUFD</c>, running <c>G</c>, then shifting back — the canonical
/// ChaCha-style vectorisation pattern that BLAKE3 inherits from BLAKE2.
/// </summary>
/// <remarks>
/// <para>
/// Gated on <see cref="Avx512F.VL.IsSupported" /> because the rotations use the AVX-512VL VPRORD variant on
/// <see cref="Vector128{T}" />. The SIMD <c>G</c> kernel is identical to <see cref="Blake2s" />'s because BLAKE3 reuses
/// the same four rotation amounts (16, 12, 8, 7); only the round count, message schedule, and finalization differ.
/// </para>
/// </remarks>
public sealed partial class Blake3
{
    /// <summary>The first rotation amount (16) used by Blake3's <c>G</c> function, broadcast across all four lanes.</summary>
    /// <remarks>
    /// The four rotation amounts are taken from §2.4 of the BLAKE3 specification and are encoded as broadcast vectors
    /// so that <c>Avx512F.VL.RotateRightVariable</c> lowers each rotate to a single VPRORD instruction. The values
    /// match <see cref="Blake2s" />.
    /// </remarks>
    private static readonly Vector128<uint> s_ror16 = Vector128.Create(16U);

    /// <summary>The second rotation amount (12) used by Blake3's <c>G</c> function, broadcast across all four lanes.</summary>
    private static readonly Vector128<uint> s_ror12 = Vector128.Create(12U);

    /// <summary>The third rotation amount (8) used by Blake3's <c>G</c> function, broadcast across all four lanes.</summary>
    private static readonly Vector128<uint> s_ror8 = Vector128.Create(8U);

    /// <summary>The fourth rotation amount (7) used by Blake3's <c>G</c> function, broadcast across all four lanes.</summary>
    private static readonly Vector128<uint> s_ror7 = Vector128.Create(7U);

    /// <summary>
    /// AVX-512 vectorised implementation of the BLAKE3 compression function used when the host supports AVX-512F + VL.
    /// Returns the same 16-word post-compression state as <see cref="CompressScalar" />.
    /// </summary>
    /// <param name="cv">The 8-word input chaining value.</param>
    /// <param name="blockWords">The 16-word message block.</param>
    /// <param name="counter">The 64-bit chunk counter.</param>
    /// <param name="blockLen">The valid byte length of the message block (≤ 64).</param>
    /// <param name="flags">The 32-bit domain-separation flags.</param>
    /// <returns>A 16-element array holding the post-compression state.</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static uint[] CompressAvx512(uint[] cv, uint[] blockWords, ulong counter, uint blockLen, uint flags)
    {
        ref uint cvRef = ref MemoryMarshal.GetArrayDataReference(cv);
        ref uint mRef = ref MemoryMarshal.GetArrayDataReference(blockWords);

        // Build the four rows of the working state.
        //   a = cv[0..3], b = cv[4..7] (upper half: chaining value)
        //   c = IV[0..3] (lower half row 0: IV)
        //   d = (counter_lo, counter_hi, blockLen, flags) (lower half row 1: domain)
        var a = Vector128.Create(
            Unsafe.Add(ref cvRef, 0),
            Unsafe.Add(ref cvRef, 1),
            Unsafe.Add(ref cvRef, 2),
            Unsafe.Add(ref cvRef, 3));
        var b = Vector128.Create(
            Unsafe.Add(ref cvRef, 4),
            Unsafe.Add(ref cvRef, 5),
            Unsafe.Add(ref cvRef, 6),
            Unsafe.Add(ref cvRef, 7));
        var c = Vector128.Create(s_iv[0], s_iv[1], s_iv[2], s_iv[3]);
        var d = Vector128.Create((uint)counter, (uint)(counter >> 32), blockLen, flags);

        // Seven rounds, each consisting of a column step followed by a diagonal step. Each step applies
        // the SIMD G kernel once across all four columns / diagonals in parallel.
        for (int round = 0; round < 7; round++)
        {
            // Column step.
            var mx = Vector128.Create(
                Unsafe.Add(ref mRef, s_msgSchedule[round, 0]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 2]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 4]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 6]));
            var my = Vector128.Create(
                Unsafe.Add(ref mRef, s_msgSchedule[round, 1]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 3]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 5]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 7]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);

            // Diagonalize: shift b left by 1 lane, c left by 2, d left by 3 so diagonals become columns.
            b = Sse2.Shuffle(b.AsInt32(), 0x39).AsUInt32();  // lanes (1, 2, 3, 0)
            c = Sse2.Shuffle(c.AsInt32(), 0x4E).AsUInt32();  // lanes (2, 3, 0, 1)
            d = Sse2.Shuffle(d.AsInt32(), 0x93).AsUInt32();  // lanes (3, 0, 1, 2)

            mx = Vector128.Create(
                Unsafe.Add(ref mRef, s_msgSchedule[round, 8]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 10]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 12]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 14]));
            my = Vector128.Create(
                Unsafe.Add(ref mRef, s_msgSchedule[round, 9]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 11]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 13]),
                Unsafe.Add(ref mRef, s_msgSchedule[round, 15]));
            GSimd(ref a, ref b, ref c, ref d, mx, my);

            // Undiagonalize: invert the lane shifts to restore canonical row layout for the next round.
            b = Sse2.Shuffle(b.AsInt32(), 0x93).AsUInt32();
            c = Sse2.Shuffle(c.AsInt32(), 0x4E).AsUInt32();
            d = Sse2.Shuffle(d.AsInt32(), 0x39).AsUInt32();
        }

        // BLAKE3 finalization differs from BLAKE2: the upper half of the output also has the input
        // chaining value mixed in, so that callers reading state[8..16] get an extended-output stream
        // distinct from a plain BLAKE2 fold.
        //   state[i]     ^= state[i + 8]   for i in 0..7  →  a ^= c, b ^= d
        //   state[i + 8] ^= cv[i]          for i in 0..7  →  c ^= cv_lo, d ^= cv_hi
        var cvLo = Vector128.Create(
            Unsafe.Add(ref cvRef, 0),
            Unsafe.Add(ref cvRef, 1),
            Unsafe.Add(ref cvRef, 2),
            Unsafe.Add(ref cvRef, 3));
        var cvHi = Vector128.Create(
            Unsafe.Add(ref cvRef, 4),
            Unsafe.Add(ref cvRef, 5),
            Unsafe.Add(ref cvRef, 6),
            Unsafe.Add(ref cvRef, 7));
        a ^= c;
        b ^= d;
        c ^= cvLo;
        d ^= cvHi;

        // Materialise the 16-word output state in canonical row order.
        uint[] state = new uint[16];
        ref uint outRef = ref MemoryMarshal.GetArrayDataReference(state);
        Unsafe.Add(ref outRef, 0) = a.GetElement(0);
        Unsafe.Add(ref outRef, 1) = a.GetElement(1);
        Unsafe.Add(ref outRef, 2) = a.GetElement(2);
        Unsafe.Add(ref outRef, 3) = a.GetElement(3);
        Unsafe.Add(ref outRef, 4) = b.GetElement(0);
        Unsafe.Add(ref outRef, 5) = b.GetElement(1);
        Unsafe.Add(ref outRef, 6) = b.GetElement(2);
        Unsafe.Add(ref outRef, 7) = b.GetElement(3);
        Unsafe.Add(ref outRef, 8) = c.GetElement(0);
        Unsafe.Add(ref outRef, 9) = c.GetElement(1);
        Unsafe.Add(ref outRef, 10) = c.GetElement(2);
        Unsafe.Add(ref outRef, 11) = c.GetElement(3);
        Unsafe.Add(ref outRef, 12) = d.GetElement(0);
        Unsafe.Add(ref outRef, 13) = d.GetElement(1);
        Unsafe.Add(ref outRef, 14) = d.GetElement(2);
        Unsafe.Add(ref outRef, 15) = d.GetElement(3);
        return state;
    }

    /// <summary>
    /// Applies the BLAKE3 <c>G</c> mixing function in parallel across four columns (or four diagonals, after the caller
    /// has applied the appropriate lane shifts). Identical kernel shape to <see cref="Blake2s" /> since BLAKE3 reuses
    /// BLAKE2s's rotation amounts.
    /// </summary>
    /// <param name="a">The top row of the working state, updated in place.</param>
    /// <param name="b">The second row of the working state, updated in place.</param>
    /// <param name="c">The third row of the working state, updated in place.</param>
    /// <param name="d">The bottom row of the working state, updated in place.</param>
    /// <param name="mx">The four <c>x</c> message words for this step, one per column.</param>
    /// <param name="my">The four <c>y</c> message words for this step, one per column.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GSimd(
        ref Vector128<uint> a, ref Vector128<uint> b, ref Vector128<uint> c, ref Vector128<uint> d,
        Vector128<uint> mx, Vector128<uint> my)
    {
        a += b + mx;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror16);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror12);
        a += b + my;
        d = Avx512F.VL.RotateRightVariable(d ^ a, s_ror8);
        c += d;
        b = Avx512F.VL.RotateRightVariable(b ^ c, s_ror7);
    }
}
