// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.Table.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class PearsonTests
{

    /// <summary>
    /// Verifies that the <see cref="Pearson.Table" /> getter returns a defensive copy.
    /// </summary>
    [TestMethod]
    public void Table_WhenAccessed_ShouldReturnIndependentCopy()
    {
        Pearson algorithm = new();
        var copy = algorithm.Table;

        copy[0] ^= 0xFF;

        Assert.AreNotEqual(copy[0], algorithm.Table[0]);
    }

    /// <summary>
    /// Verifies that every predefined permutation table is a 256-byte permutation of 0..255.
    /// </summary>
    /// <param name="variant">The predefined table type under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void Table_WhenBuiltIn_ShouldBeA256ByteUniquePermutation(Pearson.PearsonTableType variant)
    {
        Pearson algorithm = new(8, variant);
        var table = algorithm.Table;

        Assert.AreEqual(256, table.Length);
        Assert.AreEqual(256, table.Distinct().Count());
    }

}
