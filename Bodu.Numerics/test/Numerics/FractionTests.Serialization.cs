// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionTests.Serialization.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

public partial class FractionTests
{
    /// <summary>
    /// Verifies that ToXml and FromXml round-trip a fraction through its XML representation.
    /// </summary>
    [TestMethod]
    [DataRow(3, 4)]
    [DataRow(-7, 8)]
    [DataRow(5, 1)]
    [DataRow(0, 1)]
    [DataRow(-11, 3)]
    public void ToXmlAndFromXml_WhenRoundTripped_ShouldPreserveValue(int numerator, int denominator)
    {
        var original = new Fraction<int>(numerator, denominator);

        Assert.AreEqual(original, Fraction<int>.FromXml(original.ToXml()));
    }

    /// <summary>
    /// Verifies that FromXml rejects a null argument.
    /// </summary>
    [TestMethod]
    public void FromXml_WhenArgumentIsNull_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Fraction<int>.FromXml(null!);
        });
    }
}
