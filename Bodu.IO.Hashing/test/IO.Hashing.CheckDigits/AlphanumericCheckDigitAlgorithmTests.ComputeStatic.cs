// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithmTests.ComputeStatic.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;
public abstract partial class AlphanumericCheckDigitAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the static <c>Compute</c> helper returns the expected check character for every
    /// known-answer vector.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="body">The body characters.</param>
    /// <param name="expectedCheck">The expected check character.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(nameof(KnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void ComputeStatic_WhenKnownAnswer_ShouldReturnExpectedCheckDigit(string name, string body, char expectedCheck)
    {
        _ = name;
        Assert.AreEqual(expectedCheck, ComputeStatic(body.AsSpan()));
    }
}
