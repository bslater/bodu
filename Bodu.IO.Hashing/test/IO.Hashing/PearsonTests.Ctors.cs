// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public partial class PearsonTests
{

    /// <summary>
    /// Verifies that constructing with a hash size outside [8, 2048] or not divisible by 8 throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    /// <param name="bits">The unsupported hash size, in bits.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(9)]
    [DataRow(2056)]
    [DataRow(-8)]
    public void Ctor_WhenHashSizeIsOutOfRange_ShouldThrowExactly(int bits)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new Pearson(bits, Pearson.PearsonTableType.Pearson));
    }

    /// <summary>
    /// Verifies that constructing with a 256-byte table containing duplicates throws
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPermutationTableContainsDuplicates_ShouldThrowExactly()
    {
        var duplicateTable = Enumerable.Repeat((byte)42, 256).ToArray();

        Assert.ThrowsExactly<ArgumentException>(() => _ = new Pearson(8, duplicateTable));
    }

    /// <summary>
    /// Verifies that constructing with a <see langword="null" /> permutation table throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPermutationTableIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Pearson(8, null!));
    }

    /// <summary>
    /// Verifies that constructing with a permutation that is too short throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPermutationTableIsTooShort_ShouldThrowExactly()
    {
        var invalidTable = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

        Assert.ThrowsExactly<ArgumentException>(() => _ = new Pearson(8, invalidTable));
    }

    /// <summary>
    /// Verifies that constructing with a valid 256-byte permutation succeeds and that the resulting instance
    /// reports <see cref="Pearson.PearsonTableType.UserDefined" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPermutationTableIsValid_ShouldUseUserDefinedTableType()
    {
        var validTable = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

        Pearson algorithm = new(8, validTable);

        CollectionAssert.AreEqual(validTable, algorithm.Table);
        Assert.AreEqual(Pearson.PearsonTableType.UserDefined, algorithm.TableType);
    }

    /// <summary>
    /// Verifies that constructing with an undefined <see cref="Pearson.PearsonTableType" /> enum value throws
    /// <see cref="ArgumentOutOfRangeException" /> from the protected permutation-table selector.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTableTypeIsUndefined_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            _ = new Pearson(8, (Pearson.PearsonTableType)42));
    }

    /// <summary>
    /// Verifies that constructing with <see cref="Pearson.PearsonTableType.UserDefined" /> via the predefined
    /// overload throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTableTypeIsUserDefinedOnPredefinedOverload_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
            _ = new Pearson(8, Pearson.PearsonTableType.UserDefined));
    }

}
