// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithmTests.IsValid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

public abstract partial class MultiCharCheckDigitAlgorithmTests<TTest, TAlgorithm>
{
    /// <summary>
    /// Verifies that the static <c>IsValid</c> helper returns the expected verdict for each vector supplied by
    /// <see cref="GetIsValidKnownAnswers" />.
    /// </summary>
    /// <param name="name">A descriptive name for the vector.</param>
    /// <param name="value">The full sequence in canonical form.</param>
    /// <param name="expectedIsValid">The verdict the algorithm is expected to render.</param>
    [TestMethod]
    [DynamicData(nameof(IsValidKnownAnswerData), DynamicDataDisplayName = nameof(GetKnownAnswerTestName))]
    public void IsValid_WhenKnownAnswer_ShouldReturnExpectedVerdict(string name, string value, bool expectedIsValid)
    {
        _ = name;
        Assert.AreEqual(expectedIsValid, IsValidStatic(value.AsSpan()), $"Unexpected verdict for '{value}'.");
    }
}
