// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.SelectBaseMultiple.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that <see cref="Ed25519Point.SelectBaseMultiple" /> returns, for every row of the fixed-base table and
    /// every signed digit from −8 to 8, the multiple <c>digit · 256^row · B</c> formed by doublings and additions
    /// without the table: the identity for zero, and the negated multiple for a negative digit.
    /// </summary>
    [TestMethod]
    public void SelectBaseMultiple_ForEverySignedDigitOfEveryRow_ShouldMatchAPlainLookup()
    {
        var multiples = new Ed25519Point[9];
        Ed25519Point rowBase = Ed25519Point.BasePoint;

        for (int row = 0; row < 32; row++)
        {
            multiples[0] = Ed25519Point.Identity;
            for (int m = 1; m < multiples.Length; m++)
                multiples[m] = multiples[m - 1].Add(rowBase);

            for (int digit = -8; digit <= 8; digit++)
            {
                Ed25519Point expected = digit >= 0 ? multiples[digit] : multiples[-digit].Negate();
                Ed25519Point.AffineNielsPoint selected = Ed25519Point.SelectBaseMultiple(row, digit);

                CollectionAssert.AreEqual(
                    Encoded(expected),
                    Encoded(Ed25519Point.Identity.Add(selected).ToExtended()),
                    $"row {row}, digit {digit}");
            }

            for (int t = 0; t < 8; t++)
                rowBase = rowBase.Double();
        }
    }

    /// <summary>
    /// Verifies that <see cref="Ed25519Point.SelectBaseMultiple" /> throws <see cref="ArgumentOutOfRangeException" />
    /// for a row outside the table's 32.
    /// </summary>
    /// <param name="row">The row.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(32)]
    public void SelectBaseMultiple_WhenRowIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int row)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Ed25519Point.SelectBaseMultiple(row, 1);
        });
    }
}
