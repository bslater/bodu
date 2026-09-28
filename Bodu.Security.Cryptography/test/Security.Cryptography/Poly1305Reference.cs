// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Reference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the radix-2^26 Poly1305 arithmetic that <see cref="Poly1305Core" /> replaced, five 26-bit limbs with 64-bit
/// products, kept verbatim as an independent oracle that the differential tests hold the 64-bit-limb core to.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "StyleCop.CSharp.ReadabilityRules",
    "SA1107:Code should not contain multiple statements on one line",
    Justification = "The limb and carry statements are kept verbatim from the replaced implementation, grouped as it grouped them.")]
internal static class Poly1305Reference
{
    /// <summary>The 26-bit mask applied to each limb of the radix-2^26 representation.</summary>
    private const uint Mask26 = 0x3ffffff;

    /// <summary>The number of message bytes in a Poly1305 block.</summary>
    private const int BlockBytes = 16;

    /// <summary>
    /// Computes the Poly1305 tag of a whole message under a one-time key.
    /// </summary>
    /// <param name="key">The 32-byte one-time key: <c>r</c>, which is clamped, then <c>s</c>.</param>
    /// <param name="message">The message.</param>
    /// <returns>The 16-byte tag.</returns>
    internal static byte[] ComputeTag(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message)
    {
        uint[] acc = new uint[5];
        uint[] r = new uint[5];
        uint[] s = new uint[4];
        uint[] pad = new uint[4];
        LoadKey(key, r, s, pad);

        int offset = 0;
        for (; offset + BlockBytes <= message.Length; offset += BlockBytes)
            ProcessBlock(message.Slice(offset, BlockBytes), acc, r, s);

        if (offset < message.Length)
            ProcessBlock(message[offset..], acc, r, s);

        return ProcessFinalBlock(acc, pad);
    }

    /// <summary>
    /// Splits the clamped key half <c>r</c> into five 26-bit limbs, precomputes <c>5 · r[1..4]</c>, and reads <c>s</c>
    /// as four 32-bit words.
    /// </summary>
    /// <param name="key">The 32-byte one-time key.</param>
    /// <param name="r">Receives the five limbs of <c>r</c>.</param>
    /// <param name="s">Receives <c>5 · r[1..4]</c>.</param>
    /// <param name="pad">Receives the four words of <c>s</c>.</param>
    private static void LoadKey(ReadOnlySpan<byte> key, uint[] r, uint[] s, uint[] pad)
    {
        uint t0 = BinaryPrimitives.ReadUInt32LittleEndian(key[..]);
        uint t1 = BinaryPrimitives.ReadUInt32LittleEndian(key[4..]);
        uint t2 = BinaryPrimitives.ReadUInt32LittleEndian(key[8..]);
        uint t3 = BinaryPrimitives.ReadUInt32LittleEndian(key[12..]);

        r[0] = t0 & 0x03FFFFFFU;
        r[1] = ((t0 >> 26) | (t1 << 6)) & 0x03FFFF03U;
        r[2] = ((t1 >> 20) | (t2 << 12)) & 0x03FFC0FFU;
        r[3] = ((t2 >> 14) | (t3 << 18)) & 0x03F03FFFU;
        r[4] = (t3 >> 8) & 0x000FFFFFU;

        s[0] = r[1] * 5;
        s[1] = r[2] * 5;
        s[2] = r[3] * 5;
        s[3] = r[4] * 5;

        pad[0] = BinaryPrimitives.ReadUInt32LittleEndian(key[16..]);
        pad[1] = BinaryPrimitives.ReadUInt32LittleEndian(key[20..]);
        pad[2] = BinaryPrimitives.ReadUInt32LittleEndian(key[24..]);
        pad[3] = BinaryPrimitives.ReadUInt32LittleEndian(key[28..]);
    }

    /// <summary>
    /// Adds one block to the accumulator and multiplies by <c>r</c> modulo 2^130 − 5; a block shorter than 16 bytes is
    /// padded with a single 1 byte and zeros and takes no 2^128 bit.
    /// </summary>
    /// <param name="block">The block, 1 to 16 bytes.</param>
    /// <param name="acc">The five limbs of the accumulator, updated in place.</param>
    /// <param name="r">The five limbs of <c>r</c>.</param>
    /// <param name="s">The precomputed <c>5 · r[1..4]</c>.</param>
    private static void ProcessBlock(ReadOnlySpan<byte> block, uint[] acc, uint[] r, uint[] s)
    {
        Span<byte> padded = stackalloc byte[BlockBytes];
        padded.Clear();
        block.CopyTo(padded);
        if (block.Length < BlockBytes)
            padded[block.Length] = 1;

        ulong h0 = acc[0], h1 = acc[1], h2 = acc[2], h3 = acc[3], h4 = acc[4];

        ulong t0 = BinaryPrimitives.ReadUInt64LittleEndian(padded);
        ulong t1 = BinaryPrimitives.ReadUInt64LittleEndian(padded[8..]);
        h0 += (uint)(t0 & 0x3ffffff);
        h1 += (uint)((t0 >> 26) & 0x3ffffff);
        h2 += (uint)(((t0 >> 52) | (t1 << 12)) & 0x3ffffff);
        h3 += (uint)((t1 >> 14) & 0x3ffffff);
        h4 += (uint)((t1 >> 40) & 0x3ffffff);

        if (block.Length == BlockBytes)
            h4 += 1 << 24;

        ulong r0 = r[0], r1 = r[1], r2 = r[2], r3 = r[3], r4 = r[4];

        ulong t00 = (h0 * r0) + (h1 * s[3]) + (h2 * s[2]) + (h3 * s[1]) + (h4 * s[0]);
        ulong t01 = (h0 * r1) + (h1 * r0) + (h2 * s[3]) + (h3 * s[2]) + (h4 * s[1]);
        ulong t02 = (h0 * r2) + (h1 * r1) + (h2 * r0) + (h3 * s[3]) + (h4 * s[2]);
        ulong t03 = (h0 * r3) + (h1 * r2) + (h2 * r1) + (h3 * r0) + (h4 * s[3]);
        ulong t04 = (h0 * r4) + (h1 * r3) + (h2 * r2) + (h3 * r1) + (h4 * r0);

        t01 += t00 >> 26; h0 = (uint)(t00 & Mask26);
        t02 += t01 >> 26; h1 = (uint)(t01 & Mask26);
        t03 += t02 >> 26; h2 = (uint)(t02 & Mask26);
        t04 += t03 >> 26; h3 = (uint)(t03 & Mask26);
        ulong carry = t04 >> 26; h4 = (uint)(t04 & Mask26);

        h0 += (uint)(carry * 5);
        carry = h0 >> 26; h0 &= Mask26;
        h1 += (uint)carry;

        acc[0] = (uint)h0; acc[1] = (uint)h1; acc[2] = (uint)h2; acc[3] = (uint)h3; acc[4] = (uint)h4;
    }

    /// <summary>
    /// Reduces the accumulator fully modulo 2^130 − 5, adds <c>s</c>, and writes the low 128 bits as the tag.
    /// </summary>
    /// <param name="acc">The five limbs of the accumulator.</param>
    /// <param name="pad">The four words of <c>s</c>.</param>
    /// <returns>The 16-byte tag.</returns>
    private static byte[] ProcessFinalBlock(uint[] acc, uint[] pad)
    {
        uint h0 = acc[0], h1 = acc[1], h2 = acc[2], h3 = acc[3], h4 = acc[4];

        h1 += h0 >> 26; h0 &= Mask26;
        h2 += h1 >> 26; h1 &= Mask26;
        h3 += h2 >> 26; h2 &= Mask26;
        h4 += h3 >> 26; h3 &= Mask26;

        h0 += 5;
        h1 += h0 >> 26; h0 &= Mask26;
        h2 += h1 >> 26; h1 &= Mask26;
        h3 += h2 >> 26; h2 &= Mask26;
        h4 += h3 >> 26; h3 &= Mask26;

        byte[] tag = new byte[16];

        long c = ((int)(h4 >> 26) - 1) * 5L;

        c += (long)pad[0] + (h0 | (h1 << 26));
        BinaryPrimitives.WriteUInt32LittleEndian(tag.AsSpan(0), (uint)c);
        c >>= 32;

        c += (long)pad[1] + ((h1 >> 6) | (h2 << 20));
        BinaryPrimitives.WriteUInt32LittleEndian(tag.AsSpan(4), (uint)c);
        c >>= 32;

        c += (long)pad[2] + ((h2 >> 12) | (h3 << 14));
        BinaryPrimitives.WriteUInt32LittleEndian(tag.AsSpan(8), (uint)c);
        c >>= 32;

        c += (long)pad[3] + ((h3 >> 18) | (h4 << 8));
        BinaryPrimitives.WriteUInt32LittleEndian(tag.AsSpan(12), (uint)c);

        return tag;
    }
}
