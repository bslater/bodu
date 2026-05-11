// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Whirlpool.Constants.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Whirlpool
{
    /// <summary>
    /// The number of rounds used by the internal <c>W</c> block cipher in every published revision of Whirlpool.
    /// </summary>
    private const int RoundCount = 10;

    /// <summary>
    /// The reduction polynomial for the Galois field <c>GF(2^8)</c> used by Whirlpool:
    /// <c>x^8 + x^4 + x^3 + x^2 + 1</c>.
    /// </summary>
    private const int GaloisReductionPolynomial = 0x11D;

    /// <summary>
    /// The 256-entry S-box of the original <c>Whirlpool-0</c> function, reproduced verbatim from the 2000
    /// <c>NESSIE</c> submission.
    /// </summary>
    private static readonly byte[] s_sBoxWhirlpool0 =
    {
        0x68, 0xD0, 0xEB, 0x2B, 0x48, 0x9D, 0x6A, 0xE4, 0xE3, 0xA3, 0x56, 0x81, 0x7D, 0xF1, 0x85, 0x9E,
        0x2C, 0x8E, 0x78, 0xCA, 0x17, 0xA9, 0x61, 0xD5, 0x5D, 0x0B, 0x8C, 0x3C, 0x77, 0x51, 0x22, 0x42,
        0x3F, 0x54, 0x41, 0x80, 0xCC, 0x86, 0xB3, 0x18, 0x2E, 0x57, 0x06, 0x62, 0xF4, 0x36, 0xD1, 0x6B,
        0x1B, 0x65, 0x75, 0x10, 0xDA, 0x49, 0x26, 0xF9, 0xCB, 0x66, 0xE7, 0xBA, 0xAE, 0x50, 0x52, 0xAB,
        0x05, 0xF0, 0x0D, 0x73, 0x3B, 0x04, 0x20, 0xFE, 0xDD, 0xF5, 0xB4, 0x5F, 0x0A, 0xB5, 0xC0, 0xA0,
        0x71, 0xA5, 0x2D, 0x60, 0x72, 0x93, 0x39, 0x08, 0x83, 0x21, 0x5C, 0x87, 0xB1, 0xE0, 0x00, 0xC3,
        0x12, 0x91, 0x8A, 0x02, 0x1C, 0xE6, 0x45, 0xC2, 0xC4, 0xFD, 0xBF, 0x44, 0xA1, 0x4C, 0x33, 0xC5,
        0x84, 0x23, 0x7C, 0xB0, 0x25, 0x15, 0x35, 0x69, 0xFF, 0x94, 0x4D, 0x70, 0xA2, 0xAF, 0xCD, 0xD6,
        0x6C, 0xB7, 0xF8, 0x09, 0xF3, 0x67, 0xA4, 0xEA, 0xEC, 0xB6, 0xD4, 0xD2, 0x14, 0x1E, 0xE1, 0x24,
        0x38, 0xC6, 0xDB, 0x4B, 0x7A, 0x3A, 0xDE, 0x5E, 0xDF, 0x95, 0xFC, 0xAA, 0xD7, 0xCE, 0x07, 0x0F,
        0x3D, 0x58, 0x9A, 0x98, 0x9C, 0xF2, 0xA7, 0x11, 0x7E, 0x8B, 0x43, 0x03, 0xE2, 0xDC, 0xE5, 0xB2,
        0x4E, 0xC7, 0x6D, 0xE9, 0x27, 0x40, 0xD8, 0x37, 0x92, 0x8F, 0x01, 0x1D, 0x53, 0x3E, 0x59, 0xC1,
        0x4F, 0x32, 0x16, 0xFA, 0x74, 0xFB, 0x63, 0x9F, 0x34, 0x1A, 0x2A, 0x5A, 0x8D, 0xC9, 0xCF, 0xF6,
        0x90, 0x28, 0x88, 0x9B, 0x31, 0x0E, 0xBD, 0x4A, 0xE8, 0x96, 0xA6, 0x0C, 0xC8, 0x79, 0xBC, 0xBE,
        0xEF, 0x6E, 0x46, 0x97, 0x5B, 0xED, 0x19, 0xD9, 0xAC, 0x99, 0xA8, 0x29, 0x64, 0x1F, 0xAD, 0x55,
        0x13, 0xBB, 0xF7, 0x6F, 0xB9, 0x47, 0x2F, 0xEE, 0xB8, 0x7B, 0x89, 0x30, 0xD3, 0x7F, 0x76, 0x82,
    };

    /// <summary>
    /// The <c>E</c> mini-box used to construct the <c>Whirlpool-T</c> and <c>Whirlpool</c> S-box.
    /// </summary>
    private static readonly byte[] s_miniBoxE =
    {
        0x1, 0xB, 0x9, 0xC, 0xD, 0x6, 0xF, 0x3, 0xE, 0x8, 0x7, 0x4, 0xA, 0x2, 0x5, 0x0,
    };

    /// <summary>
    /// The <c>R</c> mini-box used to construct the <c>Whirlpool-T</c> and <c>Whirlpool</c> S-box.
    /// </summary>
    private static readonly byte[] s_miniBoxR =
    {
        0x7, 0xC, 0xB, 0xD, 0xE, 0x4, 0x9, 0xF, 0x6, 0x3, 0x8, 0xA, 0x2, 0x5, 0x1, 0x0,
    };

    /// <summary>
    /// The diffusion matrix coefficients common to <c>Whirlpool-0</c> and <c>Whirlpool-T</c>, expressed in the
    /// order <c>(c0, c1, …, c7)</c> used by <see cref="BuildMultiplicationTable"/>.
    /// </summary>
    private static readonly byte[] s_mdsOriginal =
    {
        0x01, 0x01, 0x03, 0x01, 0x05, 0x08, 0x09, 0x05,
    };

    /// <summary>
    /// The diffusion matrix coefficients used by the final <c>Whirlpool</c> function, expressed in the order
    /// <c>(c0, c1, …, c7)</c> used by <see cref="BuildMultiplicationTable"/>.
    /// </summary>
    private static readonly byte[] s_mdsFinal =
    {
        0x01, 0x01, 0x04, 0x01, 0x08, 0x05, 0x02, 0x09,
    };

    /// <summary>
    /// Constructs the <c>Whirlpool-T</c> / <c>Whirlpool</c> S-box from the <c>E</c>, <c>E^-1</c> and <c>R</c>
    /// mini-boxes.
    /// </summary>
    /// <returns>A 256-byte array containing the full 8-bit S-box.</returns>
    /// <remarks>
    /// The mini-box construction is the one published with <c>Whirlpool-T</c> in 2001 and retained by the
    /// standardised <c>Whirlpool</c> function in 2003. Each input byte is split into two 4-bit halves which
    /// are routed through <c>E</c>, <c>E^-1</c> and <c>R</c> in the pattern described in the original paper.
    /// </remarks>
    private static byte[] BuildMiniBoxSBox()
    {
        var einv = new byte[16];
        for (var i = 0; i < s_miniBoxE.Length; i++)
            einv[s_miniBoxE[i]] = (byte)i;

        var sbox = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            int left = s_miniBoxE[i >> 4];
            int right = einv[i & 0xF];
            int mid = s_miniBoxR[left ^ right];
            sbox[i] = (byte)((s_miniBoxE[left ^ mid] << 4) | einv[right ^ mid]);
        }

        return sbox;
    }

    /// <summary>
    /// Multiplies two bytes in the Whirlpool Galois field <c>GF(2^8)</c> defined by
    /// <c>x^8 + x^4 + x^3 + x^2 + 1</c>.
    /// </summary>
    /// <param name="x">The left multiplicand, treated as an unsigned 8-bit value.</param>
    /// <param name="y">The right multiplicand, treated as an unsigned 8-bit value.</param>
    /// <returns>The product <c>x · y</c> reduced modulo <see cref="GaloisReductionPolynomial"/>.</returns>
    private static byte GaloisMultiply(byte x, byte y)
    {
        int a = x;
        int b = y;
        var result = 0;

        while (b != 0)
        {
            if ((b & 1) != 0)
                result ^= a;

            var highBit = a & 0x80;
            a = (a << 1) & 0xFF;
            if (highBit != 0)
                a ^= GaloisReductionPolynomial & 0xFF;

            b >>= 1;
        }

        return (byte)result;
    }

    /// <summary>
    /// Builds the flat 8 × 256 multiplication table used by <see cref="ApplyRound"/> for the supplied
    /// <paramref name="sbox"/> and diffusion coefficients <paramref name="mds"/>.
    /// </summary>
    /// <param name="sbox">The 256-byte S-box selected for the configured variant.</param>
    /// <param name="mds">The eight-byte diffusion row for the configured variant.</param>
    /// <returns>
    /// A flat <see cref="ulong"/> array of length <c>8 × 256</c>, indexed as <c>[column &lt;&lt; 8 | value]</c>,
    /// where each entry is the diffusion-weighted S-box output rotated so it can be XORed directly into the
    /// corresponding state word.
    /// </returns>
    private static ulong[] BuildMultiplicationTable(byte[] sbox, byte[] mds)
    {
        var table = new ulong[8 * 256];

        for (var i = 0; i < 256; i++)
        {
            ulong vector = 0;
            for (var j = 0; j < 8; j++)
                vector |= (ulong)GaloisMultiply(sbox[i], mds[j]) << ((7 - j) * 8);

            for (var j = 0; j < 8; j++)
                table[(j << 8) | i] = System.Numerics.BitOperations.RotateRight(vector, j * 8);
        }

        return table;
    }

    /// <summary>
    /// Builds the ten 64-bit round constants for the <c>W</c> key schedule using the supplied
    /// <paramref name="sbox"/>.
    /// </summary>
    /// <param name="sbox">The 256-byte S-box selected for the configured variant.</param>
    /// <returns>An array of ten big-endian packed round constants.</returns>
    /// <remarks>
    /// Each round constant packs eight consecutive S-box outputs into the first column of the round key; the
    /// remaining seven columns are zero by definition of the Whirlpool key schedule.
    /// </remarks>
    private static ulong[] BuildRoundConstants(byte[] sbox)
    {
        var rcon = new ulong[RoundCount];
        for (var i = 0; i < RoundCount; i++)
        {
            ulong value = 0;
            for (var j = 0; j < 8; j++)
                value |= (ulong)sbox[(8 * i) + j] << ((7 - j) * 8);

            rcon[i] = value;
        }

        return rcon;
    }
}
