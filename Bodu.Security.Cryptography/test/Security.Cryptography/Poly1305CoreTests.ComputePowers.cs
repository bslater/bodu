// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.ComputePowers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Verifies that each layout <see cref="Poly1305Core.ComputePowers" /> fills — 10, 15, 20, 25, 40 and 45 limbs, of
    /// which the AdvSimd kernel reads 15, the AVX2 loops 20 and 25, and the AVX-512 kernel 45 — holds <c>r</c> to
    /// <c>rⁿ</c>, then <c>r²ⁿ</c> where there is room, modulo 2^130 − 5, as five limbs each narrow enough for the
    /// 32-bit multiplies, over seeded keys and the key whose clamped <c>r</c> is largest.
    /// </summary>
    /// <param name="length">The number of limbs in the layout.</param>
    [TestMethod]
    [DataRow(10)]
    [DataRow(15)]
    [DataRow(20)]
    [DataRow(25)]
    [DataRow(40)]
    [DataRow(45)]
    public void ComputePowers_WhenGivenEachLayout_ShouldHoldThePowersOfR(int length)
    {
        BigInteger p = (BigInteger.One << 130) - 5;
        int highest = length >= 40 ? 8 : length >= 20 ? 4 : 2;
        int[] exponents = [.. Enumerable.Range(1, highest), .. length % 10 == 5 ? new[] { 2 * highest } : []];
        var random = new Random(0x1305_000A + length);

        byte[] largest = new byte[Poly1305Core.KeyBytes];
        Array.Fill(largest, (byte)0xFF, 0, 16);
        IEnumerable<byte[]> keys = Enumerable.Range(0, 32).Select(_ => NextBytes(random, Poly1305Core.KeyBytes)).Append(largest);

        foreach (byte[] key in keys)
        {
            Poly1305Core core = default;
            core.Initialize(key);
            ulong[] powers = new ulong[length];
            core.ComputePowers(powers);

            BigInteger r = new BigInteger(key.AsSpan(0, 16), isUnsigned: true) & BigInteger.Parse("0ffffffc0ffffffc0ffffffc0fffffff", System.Globalization.NumberStyles.HexNumber);
            for (int i = 0; i < exponents.Length; i++)
            {
                ReadOnlySpan<ulong> limbs = powers.AsSpan(5 * i, 5);
                BigInteger value = BigInteger.Zero;
                for (int limb = 0; limb < 5; limb++)
                {
                    Assert.IsTrue(limbs[limb] < (limb < 4 ? 1UL << 26 : 1UL << 27), $"r^{exponents[i]}, limb {limb}");
                    value += new BigInteger(limbs[limb]) << (26 * limb);
                }

                Assert.AreEqual(BigInteger.ModPow(r, exponents[i], p), value % p, $"r^{exponents[i]}");
            }
        }
    }
}
