// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SedolTests.LengthTolerant.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Verifies the length-tolerant streaming behaviour and non-alphanumeric handling of <see cref="Sedol" />.
/// </summary>
public sealed partial class SedolTests
{

    /// <summary>
    /// Verifies that streaming <see cref="Sedol.Append" /> rejects a non-alphanumeric body character with
    /// <see cref="ArgumentOutOfRangeException" />. The validation must occur on the streaming surface, not just
    /// the static <see cref="Sedol.Compute" /> helper.
    /// </summary>
    [TestMethod]
    public void Append_WhenBodyContainsNonAlphanumericCharacter_ShouldThrowExactly()
    {
        Sedol algorithm = new();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            algorithm.Append("B0WNL-".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that streaming <see cref="Sedol.Append" /> with a body longer than the SEDOL weight pattern
    /// (six positions) continues to compute a consistent check digit using a default weight of <c>1</c> for
    /// positions beyond the pattern.
    /// </summary>
    [TestMethod]
    public void Append_WhenBodyLengthExceedsSixPositions_ShouldUseUnitWeightForTrailingPositions()
    {
        Sedol algorithm = new();

        // Six body chars, then a seventh body char to exercise the default-weight branch in Append.
        algorithm.Append("B0WNLY".AsSpan());
        var checkAtSix = algorithm.GetCurrentCheckDigit();

        algorithm.Append("0".AsSpan());
        var checkAtSeven = algorithm.GetCurrentCheckDigit();

        // Appending a single '0' with weight 1 contributes 0 to the sum, so the check digit is unchanged.
        Assert.AreEqual(checkAtSix, checkAtSeven);
    }

    /// <summary>
    /// Verifies that <see cref="Sedol.Compute" /> with a body longer than six positions tolerates the trailing
    /// characters using a default weight of <c>1</c>.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyLengthExceedsSixPositions_ShouldUseUnitWeightForTrailingPositions()
    {
        var checkAtSix = Sedol.Compute("B0WNLY".AsSpan());
        var checkAtSeven = Sedol.Compute("B0WNLY0".AsSpan());

        Assert.AreEqual(checkAtSix, checkAtSeven);
    }

}
