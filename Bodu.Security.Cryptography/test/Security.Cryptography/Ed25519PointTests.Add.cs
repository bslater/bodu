// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that adding each multiple the fixed-base table serves, in affine Niels form, to seeded points matches
    /// the unified addition of the same multiple, and leaves a consistent extended coordinate for a further addition.
    /// </summary>
    [TestMethod]
    public void Add_WhenAddingAnAffineNielsMultiple_ShouldMatchTheUnifiedAddition()
    {
        var random = new Random(0x2551_9115);
        byte[] scalar = new byte[32];

        foreach (int row in new[] { 0, 1, 17, 31 })
        {
            Ed25519Point rowBase = Ed25519Point.BasePoint;
            for (int t = 0; t < 8 * row; t++)
                rowBase = rowBase.Double();

            Ed25519Point multiple = Ed25519Point.Identity;
            for (int digit = 0; digit <= 8; digit++)
            {
                random.NextBytes(scalar);
                Ed25519Point point = Ed25519Point.ScalarMultBase(scalar);
                random.NextBytes(scalar);
                Ed25519Point further = Ed25519Point.ScalarMultBase(scalar);

                Ed25519Point expected = point.Add(multiple);
                Ed25519Point actual = point.Add(Ed25519Point.SelectBaseMultiple(row, digit)).ToExtended();

                CollectionAssert.AreEqual(Encoded(expected), Encoded(actual), $"row {row}, digit {digit}");
                CollectionAssert.AreEqual(
                    Encoded(expected.Add(further)),
                    Encoded(actual.Add(further)),
                    $"further addition, row {row}, digit {digit}");

                multiple = multiple.Add(rowBase);
            }
        }
    }

    /// <summary>
    /// Verifies that adding a point in projective Niels form matches the unified addition, and leaves a consistent
    /// extended coordinate for a further addition, for seeded points and for every point of small order on either
    /// side.
    /// </summary>
    [TestMethod]
    public void Add_WhenAddingAProjectiveNielsPoint_ShouldMatchTheUnifiedAddition()
    {
        foreach ((string name, Ed25519Point point, Ed25519Point other, Ed25519Point further) in PointTriples(0x2551_9116))
        {
            Ed25519Point expected = point.Add(other);
            Ed25519Point actual = point.Add(other.ToProjectiveNiels()).ToExtended();

            CollectionAssert.AreEqual(Encoded(expected), Encoded(actual), name);
            CollectionAssert.AreEqual(Encoded(expected.Add(further)), Encoded(actual.Add(further)), $"further addition, {name}");
        }
    }
}
