// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GaloisField128.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides multiplication in the binary field <c>GF(2¹²⁸)</c> defined by the GCM/GHASH reduction polynomial
/// <c>x¹²⁸ + x⁷ + x² + x + 1</c> with big-endian bit ordering, as used by GHASH (NIST SP 800-38D) and, via reflection,
/// POLYVAL (RFC 8452).
/// </summary>
/// <remarks>
/// <para>
/// Two implementations are provided and produce bit-identical results: a hardened, branch-free scalar multiply (<see cref="MultiplyScalar" />)
/// and a carry-less multiply (<see cref="MultiplyClmul" />) built on the <see cref="Pclmulqdq" /> instruction.
/// <see cref="Multiply" /> dispatches to the accelerated path when <see cref="SimdCapabilities.Pclmulqdq" /> reports it
/// available and otherwise falls back to the scalar path, so the process-wide
/// <see cref="SimdCapabilities.DisableSimdSwitchName" /> switch pins execution to the scalar reference.
/// </para>
/// <para>
/// Both paths are constant-time with respect to their operands: the scalar path folds every secret-dependent decision
/// through <c>0x00</c>/<c>0xFF</c> masks, and the carry-less path performs no data-dependent branching or memory
/// access. This makes the multiply safe to call with secret field elements (GHASH state, POLYVAL state, and hash keys).
/// </para>
/// </remarks>
internal static class GaloisField128
{
    /// <summary>The byte-reversal shuffle mask mapping the GHASH big-endian block layout to the little-endian polynomial order the carry-less multiply operates on.</summary>
    private static readonly Vector128<byte> ByteReverseMask =
        Vector128.Create((byte)15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

    /// <summary>
    /// Multiplies <paramref name="x" /> by <paramref name="h" /> in <c>GF(2¹²⁸)</c>, writing the product into
    /// <paramref name="result" />. Dispatches to the carry-less path when hardware acceleration is available and the
    /// process-wide SIMD switch is off; otherwise uses the scalar reference multiply.
    /// </summary>
    /// <param name="x">The left operand block (16 bytes).</param>
    /// <param name="h">The right operand block, typically the hash subkey <c>H</c> (16 bytes).</param>
    /// <param name="result">The destination span (16 bytes); may alias <paramref name="x" />.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Multiply(ReadOnlySpan<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
    {
        if (SimdCapabilities.Pclmulqdq)
            MultiplyClmul(x, h, result);
        else
            MultiplyScalar(x, h, result);
    }

    /// <summary>
    /// Multiplies <paramref name="x" /> by <paramref name="h" /> in <c>GF(2¹²⁸)</c> using a branch-free bit-serial
    /// shift-and-XOR algorithm. This is the constant-time scalar reference the carry-less path must match exactly.
    /// </summary>
    /// <param name="x">The left operand block (16 bytes).</param>
    /// <param name="h">The right operand block (16 bytes).</param>
    /// <param name="result">The destination span (16 bytes); may alias <paramref name="x" />.</param>
    internal static void MultiplyScalar(ReadOnlySpan<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
    {
        Span<byte> z = stackalloc byte[16];
        Span<byte> v = stackalloc byte[16];

        try
        {
            h.CopyTo(v);

            // Constant-time bit-serial multiply: every iteration touches z and v unconditionally. The two
            // secret-dependent decisions — whether bit i of x is set, and whether the bit shifted out of v is set —
            // are folded in through 0x00/0xFF masks instead of branches, so neither control flow nor memory-access
            // pattern depends on x, h, or the running state.
            for (int i = 0; i < 128; i++)
            {
                // bit i of x in big-endian bit order; bitMask is 0xFF when set, 0x00 otherwise.
                byte bitMask = (byte)(-((x[i >> 3] >> (7 - (i & 7))) & 1));
                for (int j = 0; j < 16; j++)
                    z[j] ^= (byte)(v[j] & bitMask);

                // lsbMask is 0xFF when the bit about to be shifted out of v is set, 0x00 otherwise.
                byte lsbMask = (byte)(-(v[15] & 0x01));

                for (int j = 15; j > 0; j--)
                    v[j] = (byte)((v[j] >> 1) | ((v[j - 1] & 0x01) << 7));
                v[0] >>= 1;

                // Reduce by R = 0xE1 || 0…0 (representing x⁷ + x² + x + 1) when that bit was set.
                v[0] ^= (byte)(0xE1 & lsbMask);
            }

            z.CopyTo(result);
        }
        finally
        {
            CryptographyHelper.Clear(v);
            CryptographyHelper.Clear(z);
        }
    }

    /// <summary>
    /// Multiplies <paramref name="x" /> by <paramref name="h" /> in <c>GF(2¹²⁸)</c> using the <see cref="Pclmulqdq" />
    /// carry-less multiply. The operands are byte-reversed into the reflected polynomial representation, multiplied
    /// with the Gueron–Kounavis reduction, and reversed back, producing output bit-identical to
    /// <see cref="MultiplyScalar" />.
    /// </summary>
    /// <param name="x">The left operand block (16 bytes).</param>
    /// <param name="h">The right operand block (16 bytes).</param>
    /// <param name="result">The destination span (16 bytes); may alias <paramref name="x" />.</param>
    internal static void MultiplyClmul(ReadOnlySpan<byte> x, ReadOnlySpan<byte> h, Span<byte> result)
    {
        // Because GHASH stores bits most-significant-first within each byte, a plain byte reversal yields the full
        // bit-reversal of the field element — no intra-byte bit swap is required — mapping the big-endian block to the
        // little-endian polynomial order the multiply expects.
        Vector128<byte> a = Ssse3.Shuffle(Vector128.Create(x), ByteReverseMask);
        Vector128<byte> b = Ssse3.Shuffle(Vector128.Create(h), ByteReverseMask);

        Vector128<byte> product = MultiplyReflected(a, b);

        Ssse3.Shuffle(product, ByteReverseMask).CopyTo(result);
    }

    /// <summary>
    /// Multiplies two 128-bit field elements already in the reflected (byte-reversed) representation and reduces the
    /// 256-bit carry-less product modulo the GCM polynomial. Implements the Gueron–Kounavis schoolbook multiply plus
    /// two-phase shift reduction.
    /// </summary>
    /// <param name="aBytes">The reflected left operand.</param>
    /// <param name="bBytes">The reflected right operand.</param>
    /// <returns>The reflected product <c>a · b mod (x¹²⁸ + x⁷ + x² + x + 1)</c>.</returns>
    private static Vector128<byte> MultiplyReflected(Vector128<byte> aBytes, Vector128<byte> bBytes)
    {
        Vector128<ulong> a = aBytes.AsUInt64();
        Vector128<ulong> b = bBytes.AsUInt64();

        // Schoolbook 128×128 carry-less product: lo holds a0·b0, hi holds a1·b1, mid the two cross terms folded in.
        Vector128<ulong> lo = Pclmulqdq.CarrylessMultiply(a, b, 0x00);
        Vector128<ulong> hi = Pclmulqdq.CarrylessMultiply(a, b, 0x11);
        Vector128<ulong> mid = Sse2.Xor(
            Pclmulqdq.CarrylessMultiply(a, b, 0x10),
            Pclmulqdq.CarrylessMultiply(a, b, 0x01));

        lo = Sse2.Xor(lo, Sse2.ShiftLeftLogical128BitLane(mid.AsByte(), 8).AsUInt64());
        hi = Sse2.Xor(hi, Sse2.ShiftRightLogical128BitLane(mid.AsByte(), 8).AsUInt64());

        // Shift the full 256-bit product left by one bit to compensate for the reflected representation.
        Vector128<uint> loW = lo.AsUInt32();
        Vector128<uint> hiW = hi.AsUInt32();
        Vector128<uint> loCarry = Sse2.ShiftRightLogical(loW, 31);
        Vector128<uint> hiCarry = Sse2.ShiftRightLogical(hiW, 31);
        loW = Sse2.ShiftLeftLogical(loW, 1);
        hiW = Sse2.ShiftLeftLogical(hiW, 1);
        Vector128<uint> crossCarry = Sse2.ShiftRightLogical128BitLane(loCarry.AsByte(), 12).AsUInt32();
        hiCarry = Sse2.ShiftLeftLogical128BitLane(hiCarry.AsByte(), 4).AsUInt32();
        loCarry = Sse2.ShiftLeftLogical128BitLane(loCarry.AsByte(), 4).AsUInt32();
        loW = Sse2.Or(loW, loCarry);
        hiW = Sse2.Or(hiW, hiCarry);
        hiW = Sse2.Or(hiW, crossCarry);

        // First reduction phase: fold the low half's high bits produced by the polynomial's x⁷/x²/x¹ terms.
        Vector128<uint> t1 = Sse2.ShiftLeftLogical(loW, 31);
        Vector128<uint> t2 = Sse2.ShiftLeftLogical(loW, 30);
        Vector128<uint> t3 = Sse2.ShiftLeftLogical(loW, 25);
        t1 = Sse2.Xor(Sse2.Xor(t1, t2), t3);
        Vector128<uint> spill = Sse2.ShiftRightLogical128BitLane(t1.AsByte(), 4).AsUInt32();
        t1 = Sse2.ShiftLeftLogical128BitLane(t1.AsByte(), 12).AsUInt32();
        loW = Sse2.Xor(loW, t1);

        // Second reduction phase: complete the fold into the high half.
        Vector128<uint> r1 = Sse2.ShiftRightLogical(loW, 1);
        Vector128<uint> r2 = Sse2.ShiftRightLogical(loW, 2);
        Vector128<uint> r3 = Sse2.ShiftRightLogical(loW, 7);
        r1 = Sse2.Xor(Sse2.Xor(r1, r2), r3);
        r1 = Sse2.Xor(r1, spill);
        loW = Sse2.Xor(loW, r1);
        hiW = Sse2.Xor(hiW, loW);

        return hiW.AsByte();
    }
}
