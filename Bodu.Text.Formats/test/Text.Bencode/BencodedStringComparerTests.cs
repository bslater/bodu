// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedStringComparerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Behavioural tests for <see cref="BencodedStringComparer" />. Validates the raw-byte ordinal ordering used by
/// <see cref="BencodedDictionary" />.
/// </summary>
[TestClass]
public sealed class BencodedStringComparerTests
{

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.Compare" /> returns zero when both arguments are
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Compare_WhenBothNull_ShouldReturnZero()
    {
        Assert.AreEqual(0, BencodedStringComparer.Ordinal.Compare(null, null));
    }

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.Compare" /> orders by the first differing byte.
    /// </summary>
    [TestMethod]
    public void Compare_WhenDifferingBytes_ShouldOrderByFirstDifference()
    {
        BencodedString a = new(new byte[] { 0xAA, 0x00 });
        BencodedString b = new(new byte[] { 0xAA, 0x01 });

        Assert.IsTrue(BencodedStringComparer.Ordinal.Compare(a, b) < 0);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.Compare" /> returns zero for equal byte content.
    /// </summary>
    [TestMethod]
    public void Compare_WhenEqualBytes_ShouldReturnZero()
    {
        BencodedString a = new(new byte[] { 0xAA, 0xBB });
        BencodedString b = new(new byte[] { 0xAA, 0xBB });

        Assert.AreEqual(0, BencodedStringComparer.Ordinal.Compare(a, b));
    }

    /// <summary>
    /// Verifies that when one sequence is a prefix of the other, the shorter sequence sorts first.
    /// </summary>
    [TestMethod]
    public void Compare_WhenPrefix_ShouldOrderShorterFirst()
    {
        BencodedString a = new(new byte[] { 0xAA });
        BencodedString b = new(new byte[] { 0xAA, 0xBB });

        Assert.IsTrue(BencodedStringComparer.Ordinal.Compare(a, b) < 0);
        Assert.IsTrue(BencodedStringComparer.Ordinal.Compare(b, a) > 0);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.Compare" /> orders a <see langword="null" /> argument
    /// before any non-null argument.
    /// </summary>
    [TestMethod]
    public void Compare_WhenXIsNull_ShouldReturnNegative()
    {
        int result = BencodedStringComparer.Ordinal.Compare(null, new BencodedString(new byte[] { 0x01 }));

        Assert.IsTrue(result < 0);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.Compare" /> orders any non-null argument after a
    /// <see langword="null" /> argument.
    /// </summary>
    [TestMethod]
    public void Compare_WhenYIsNull_ShouldReturnPositive()
    {
        int result = BencodedStringComparer.Ordinal.Compare(new BencodedString(new byte[] { 0x01 }), null);

        Assert.IsTrue(result > 0);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.Equals(BencodedString?, BencodedString?)" /> agrees with
    /// <see cref="BencodedStringComparer.Compare" /> for equal sequences.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameBytes_ShouldReturnTrue()
    {
        BencodedString a = new(new byte[] { 0x01 });
        BencodedString b = new(new byte[] { 0x01 });

        Assert.IsTrue(BencodedStringComparer.Ordinal.Equals(a, b));
    }

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.GetHashCode(BencodedString)" /> rejects a
    /// <see langword="null" /> argument with <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = BencodedStringComparer.Ordinal.GetHashCode(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.GetHashCode(BencodedString)" /> returns equal hash codes
    /// for equal byte content.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenSameBytes_ShouldReturnSameHash()
    {
        BencodedString a = new(new byte[] { 0x01, 0x02 });
        BencodedString b = new(new byte[] { 0x01, 0x02 });

        Assert.AreEqual(BencodedStringComparer.Ordinal.GetHashCode(a), BencodedStringComparer.Ordinal.GetHashCode(b));
    }
    /// <summary>
    /// Verifies that <see cref="BencodedStringComparer.Ordinal" /> returns the same singleton instance on
    /// repeated access.
    /// </summary>
    [TestMethod]
    public void Ordinal_ShouldReturnSingletonInstance()
    {
        Assert.AreSame(BencodedStringComparer.Ordinal, BencodedStringComparer.Ordinal);
    }

}
