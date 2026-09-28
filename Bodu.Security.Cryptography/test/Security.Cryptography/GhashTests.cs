// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GhashTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for <see cref="Ghash" />, grouped into member-named partial files. Every kernel the processor supports is held
/// to the published RFC 8452 and NIST SP 800-38D values and to a bit-serial reference, NIST SP 800-38D's Algorithm 1;
/// GCM's and GCM-SIV's known-answer suites pin the kernels end to end.
/// </summary>
[TestClass]
public sealed partial class GhashTests
{
    /// <summary>
    /// Gets every kernel this processor can run, so an x64 host checks the carry-less and scalar kernels and an ARM64
    /// host with the cryptography extension the polynomial-multiply and scalar kernels.
    /// </summary>
    /// <returns>One row per supported kernel, holding its name.</returns>
    public static IEnumerable<object[]> SupportedKernels() =>
        Enum.GetValues<Ghash.KernelKind>().Where(Ghash.IsSupported).Select(kernel => new object[] { kernel.ToString() });

    /// <summary>
    /// Computes GHASH one block at a time with the bit-serial reference multiply.
    /// </summary>
    /// <param name="h">The hash key.</param>
    /// <param name="state">The starting state, in GHASH's byte order.</param>
    /// <param name="data">The data; a final partial block is padded with zeros.</param>
    /// <returns>The resulting state.</returns>
    private static byte[] ReferenceGhash(byte[] h, byte[] state, byte[] data)
    {
        byte[] y = (byte[])state.Clone();
        for (int offset = 0; offset < data.Length; offset += 16)
        {
            for (int i = 0; i < Math.Min(16, data.Length - offset); i++)
                y[i] ^= data[offset + i];

            y = ReferenceMultiply(y, h);
        }

        return y;
    }

    /// <summary>
    /// Computes POLYVAL one block at a time through RFC 8452 Appendix A's isomorphism with GHASH, on the bit-serial
    /// reference multiply.
    /// </summary>
    /// <param name="h">The POLYVAL key.</param>
    /// <param name="state">The starting state, in POLYVAL's byte order.</param>
    /// <param name="data">The data; a final partial block is padded with zeros.</param>
    /// <returns>The resulting state.</returns>
    private static byte[] ReferencePolyval(byte[] h, byte[] state, byte[] data)
    {
        byte[] ghashKey = ReverseCopy(h);
        Ghash.MultiplyByX(ghashKey, ghashKey);

        byte[] y = ReverseCopy(state);
        for (int offset = 0; offset < data.Length; offset += 16)
        {
            byte[] block = new byte[16];
            Array.Copy(data, offset, block, 0, Math.Min(16, data.Length - offset));
            for (int i = 0; i < 16; i++)
                y[i] ^= block[15 - i];

            y = ReferenceMultiply(y, ghashKey);
        }

        return ReverseCopy(y);
    }

    /// <summary>
    /// Multiplies two GHASH field elements bit by bit, as NIST SP 800-38D's Algorithm 1 defines the product: bit 0 of an
    /// element is the most significant bit of its first byte, and each step shifts <c>V</c> right one bit, reducing by
    /// <c>R = 11100001 || 0¹²⁰</c> when a set bit leaves it.
    /// </summary>
    /// <param name="x">The first factor.</param>
    /// <param name="y">The second factor.</param>
    /// <returns>The product.</returns>
    private static byte[] ReferenceMultiply(byte[] x, byte[] y)
    {
        UInt128 xValue = BinaryPrimitives.ReadUInt128BigEndian(x);
        UInt128 v = BinaryPrimitives.ReadUInt128BigEndian(y);
        UInt128 z = 0;

        for (int i = 0; i < 128; i++)
        {
            if (((xValue >> (127 - i)) & 1) != 0)
                z ^= v;

            bool carry = (v & 1) != 0;
            v >>= 1;
            if (carry)
                v ^= (UInt128)0xE1 << 120;
        }

        byte[] product = new byte[16];
        BinaryPrimitives.WriteUInt128BigEndian(product, z);
        return product;
    }

    /// <summary>
    /// Returns the boundary operands: zero, every bit set, and each of the 128 single-bit elements, where a lost carry
    /// or a wrong reduction term would show.
    /// </summary>
    /// <returns>The operands.</returns>
    private static byte[][] BoundaryOperands()
    {
        var operands = new List<byte[]> { new byte[16], Enumerable.Repeat((byte)0xFF, 16).ToArray() };
        for (int bit = 0; bit < 128; bit++)
        {
            byte[] operand = new byte[16];
            operand[bit >> 3] = (byte)(1 << (7 - (bit & 7)));
            operands.Add(operand);
        }

        return operands.ToArray();
    }

    /// <summary>
    /// Returns a copy of a buffer with its bytes in reverse order.
    /// </summary>
    /// <param name="value">The buffer to reverse.</param>
    /// <returns>The reversed copy.</returns>
    private static byte[] ReverseCopy(byte[] value)
    {
        byte[] copy = (byte[])value.Clone();
        Array.Reverse(copy);
        return copy;
    }

    /// <summary>
    /// Returns a deterministic pseudo-random buffer.
    /// </summary>
    /// <param name="length">The buffer's length.</param>
    /// <param name="seed">The generator's seed.</param>
    /// <returns>The buffer.</returns>
    private static byte[] RandomBytes(int length, int seed)
    {
        byte[] buffer = new byte[length];
        new Random(seed).NextBytes(buffer);
        return buffer;
    }
}
