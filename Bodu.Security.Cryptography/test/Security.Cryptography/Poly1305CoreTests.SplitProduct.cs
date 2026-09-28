// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.SplitProduct.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>The largest accumulator limb the split accepts: 2^46 − 1.</summary>
    private const ulong LargestLeftFactor = (1UL << 46) - 1;

    /// <summary>The largest limb of <c>r</c> or <c>20 · r</c> the split accepts: 2^49 − 1.</summary>
    private const ulong LargestRightFactor = (1UL << 49) - 1;

    /// <summary>
    /// Verifies that splitting a product at bit 44 with three 64-bit multiplies, as processors without a high-half
    /// multiply instruction do, matches the full 128-bit product for factors at and around their bounds and the 32-bit
    /// word boundaries.
    /// </summary>
    /// <param name="left">The accumulator limb.</param>
    /// <param name="right">The limb of <c>r</c> or <c>20 · r</c>.</param>
    [TestMethod]
    [DataRow(0UL, 0UL)]
    [DataRow(1UL, 1UL)]
    [DataRow(LargestLeftFactor, LargestRightFactor)]
    [DataRow(LargestLeftFactor, 0UL)]
    [DataRow(0UL, LargestRightFactor)]
    [DataRow(0xFFFF_FFFFUL, 0xFFFF_FFFFUL)]
    [DataRow(0x1_0000_0000UL, 0x1_0000_0000UL)]
    [DataRow(LargestLeftFactor, 0xFFFF_FFFFUL)]
    [DataRow(0xFFFF_FFFFUL, LargestRightFactor)]
    [DataRow(0x3FFF_0000_0000UL, 0x1_FFFF_0000_0000UL)]
    [DataRow(0x0FFF_FFFF_FFFFUL, 0x0FFF_FFFF_FFFFUL)]
    public void SplitProduct_WhenFactorsAreAtTheirBounds_ShouldMatchTheFullProduct(ulong left, ulong right)
    {
        UInt128 product = (UInt128)left * right;

        ulong high = Poly1305Core.SplitProduct(left, right, out ulong low);

        Assert.AreEqual((ulong)(product & ((1UL << 44) - 1)), low, "low 44 bits");
        Assert.AreEqual((ulong)(product >> 44), high, "product shifted right 44 bits");
    }

    /// <summary>
    /// Verifies that splitting a product at bit 44 with three 64-bit multiplies matches the full 128-bit product for
    /// seeded factors anywhere within their bounds.
    /// </summary>
    [TestMethod]
    public void SplitProduct_WhenFactorsAreRandomWithinTheirBounds_ShouldMatchTheFullProduct()
    {
        var random = new Random(0x5011_0001);

        for (int i = 0; i < 100_000; i++)
        {
            ulong left = (ulong)random.NextInt64() & LargestLeftFactor;
            ulong right = (ulong)random.NextInt64() & LargestRightFactor;
            UInt128 product = (UInt128)left * right;

            ulong high = Poly1305Core.SplitProduct(left, right, out ulong low);

            if (low != (ulong)(product & ((1UL << 44) - 1)) || high != (ulong)(product >> 44))
                Assert.Fail($"The split of {left:X} × {right:X} is {high:X} · 2^44 + {low:X}.");
        }
    }
}
