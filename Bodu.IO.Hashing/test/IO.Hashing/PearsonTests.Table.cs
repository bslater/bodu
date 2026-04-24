// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.Table.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class PearsonTests
{
    /// <summary>
    /// Verifies that constructing with a valid 256-byte permutation succeeds and that the resulting instance
    /// reports <see cref="Pearson.PearsonTableType.UserDefined" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPermutationTableIsValid_ShouldUseUserDefinedTableType()
    {
        byte[] validTable = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        Pearson algorithm = new(8, validTable);

        CollectionAssert.AreEqual(validTable, algorithm.Table);
        Assert.AreEqual(Pearson.PearsonTableType.UserDefined, algorithm.TableType);
    }

    /// <summary>
    /// Verifies that constructing with a permutation that is too short throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPermutationTableIsTooShort_ShouldThrow()
    {
        byte[] invalidTable = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

        Assert.ThrowsExactly<ArgumentException>(() => _ = new Pearson(8, invalidTable));
    }

    /// <summary>
    /// Verifies that constructing with a 256-byte table containing duplicates throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPermutationTableContainsDuplicates_ShouldThrow()
    {
        byte[] duplicateTable = Enumerable.Repeat((byte)42, 256).ToArray();

        Assert.ThrowsExactly<ArgumentException>(() => _ = new Pearson(8, duplicateTable));
    }

    /// <summary>
    /// Verifies that constructing with a <see langword="null" /> permutation table throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPermutationTableIsNull_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Pearson(8, null!));
    }

    /// <summary>
    /// Verifies that the <see cref="Pearson.Table" /> getter returns a defensive copy.
    /// </summary>
    [TestMethod]
    public void Table_WhenAccessed_ShouldReturnIndependentCopy()
    {
        Pearson algorithm = new();
        byte[] copy = algorithm.Table;

        copy[0] ^= 0xFF;

        Assert.AreNotEqual(copy[0], algorithm.Table[0]);
    }

    /// <summary>
    /// Verifies that every predefined permutation table is a 256-byte permutation of 0..255.
    /// </summary>
    /// <param name="variant">The predefined table type under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void Table_WhenBuiltIn_ShouldBeA256ByteUniquePermutation(Pearson.PearsonTableType variant)
    {
        Pearson algorithm = new(8, variant);
        byte[] table = algorithm.Table;

        Assert.AreEqual(256, table.Length);
        Assert.AreEqual(256, table.Distinct().Count());
    }

    /// <summary>
    /// Verifies that constructing with <see cref="Pearson.PearsonTableType.UserDefined" /> via the predefined
    /// overload throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTableTypeIsUserDefinedOnPredefinedOverload_ShouldThrow()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new Pearson(8, Pearson.PearsonTableType.UserDefined));
    }
}
