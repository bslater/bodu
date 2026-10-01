// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ed25519PointTests.ProjectivePoint.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Ed25519PointTests
{
    /// <summary>
    /// Verifies that doubling in projective coordinates, converted back to extended coordinates, matches the extended
    /// doubling and leaves a consistent extended coordinate for a further addition, for seeded points, every point of
    /// small order, and the identity.
    /// </summary>
    [TestMethod]
    public void Double_ForProjectivePoint_WhenPointsAreSeededOrOfSmallOrder_ShouldMatchTheExtendedDoubling()
    {
        foreach ((string name, Ed25519Point point, _, Ed25519Point further) in PointTriples(0x2551_9119))
        {
            Ed25519Point expected = point.Double();
            Ed25519Point actual = point.ToProjective().Double().ToExtended();

            CollectionAssert.AreEqual(Encoded(expected), Encoded(actual), name);
            CollectionAssert.AreEqual(Encoded(expected.Add(further)), Encoded(actual.Add(further)), $"further addition, {name}");
        }
    }

    /// <summary>
    /// Verifies that doublings chained through projective coordinates, as the fixed-base multiplication and
    /// verification chain them, match the same number of extended doublings.
    /// </summary>
    [TestMethod]
    public void Double_ForProjectivePoint_WhenChainedThroughProjectiveCoordinates_ShouldMatchRepeatedExtendedDoubling()
    {
        foreach ((string name, Ed25519Point point, _, _) in PointTriples(0x2551_911A))
        {
            Ed25519Point expected = point;
            Ed25519Point.CompletedPoint chained = point.ToProjective().Double();
            expected = expected.Double();
            for (int doubling = 1; doubling < 5; doubling++)
            {
                chained = chained.ToProjective().Double();
                expected = expected.Double();
            }

            CollectionAssert.AreEqual(Encoded(expected), Encoded(chained.ToExtended()), name);
        }
    }
}
