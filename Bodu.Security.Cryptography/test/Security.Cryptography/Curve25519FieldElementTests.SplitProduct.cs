// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Curve25519FieldElementTests.SplitProduct.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Curve25519FieldElementTests
{
    /// <summary>The largest first factor the split accepts: 2^55 − 1, a doubled limb.</summary>
    private const ulong LargestLeftFactor = (1UL << 55) - 1;

    /// <summary>The largest second factor the split accepts: 2^59 − 1, above any limb scaled by 19.</summary>
    private const ulong LargestRightFactor = (1UL << 59) - 1;

    /// <summary>
    /// Verifies that splitting a product at bit 51 with four 64-bit multiplies, as processors without a high-half
    /// multiply instruction do, matches the full 128-bit product for factors at and around their bounds and the 32-bit
    /// word boundaries.
    /// </summary>
    /// <param name="left">The first factor.</param>
    /// <param name="right">The second factor.</param>
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
    [DataRow(0x7F_FFFF_0000_0000UL, 0x7FF_FFFF_0000_0000UL)]
    [DataRow((1UL << 51) - 1, 19 * ((1UL << 54) - 1))]
    public void SplitProduct_WhenFactorsAreAtTheirBounds_ShouldMatchTheFullProduct(ulong left, ulong right)
    {
        UInt128 product = (UInt128)left * right;

        ulong high = Curve25519FieldElement.SplitProduct(left, right, out ulong low);

        Assert.AreEqual((ulong)(product & ((1UL << 51) - 1)), low, "low 51 bits");
        Assert.AreEqual((ulong)(product >> 51), high, "product shifted right 51 bits");
    }

    /// <summary>
    /// Verifies that splitting a product at bit 51 with four 64-bit multiplies matches the full 128-bit product for
    /// seeded factors anywhere within their bounds.
    /// </summary>
    [TestMethod]
    public void SplitProduct_WhenFactorsAreRandomWithinTheirBounds_ShouldMatchTheFullProduct()
    {
        var random = new Random(0x2551_9004);

        for (int i = 0; i < 100_000; i++)
        {
            ulong left = (ulong)random.NextInt64() & LargestLeftFactor;
            ulong right = (ulong)random.NextInt64() & LargestRightFactor;
            UInt128 product = (UInt128)left * right;

            ulong high = Curve25519FieldElement.SplitProduct(left, right, out ulong low);

            if (low != (ulong)(product & ((1UL << 51) - 1)) || high != (ulong)(product >> 51))
                Assert.Fail($"The split of {left:X} × {right:X} is {high:X} · 2^51 + {low:X}.");
        }
    }
}
