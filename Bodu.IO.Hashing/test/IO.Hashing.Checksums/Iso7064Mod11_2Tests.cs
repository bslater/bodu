// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod11_2Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Iso7064Mod11_2" /> check-character algorithm.
/// </summary>
[TestClass]
public sealed partial class Iso7064Mod11_2Tests : AlphanumericCheckDigitAlgorithmTests<Iso7064Mod11_2Tests, Iso7064Mod11_2>
{
    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ISO 7064 MOD 11-2",
        InputAlphabet = CheckDigitInputAlphabet.DecimalDigits,
        OutputAlphabet = CheckDigitOutputAlphabet.DecimalDigitsOrX,
        EmptyCheckDigit = '1',
        KnownAnswers =
        [
            new() { Name = "StandardExample",     Body = "0794",      ExpectedCheck = '0' },
            new() { Name = "SingleZero",          Body = "0",         ExpectedCheck = '1' },
            new() { Name = "SingleOne",           Body = "1",         ExpectedCheck = 'X' },
            new() { Name = "LongerNumeric",       Body = "1234567890", ExpectedCheck = '8' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Iso7064Mod11_2.Compute(body);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Iso7064Mod11_2.IsValid(valueIncludingCheck);

    /// <summary>
    /// Verifies that <see cref="Iso7064Mod11_2.IsValid(ReadOnlySpan{char})" /> accepts the sentinel <c>'X'</c>
    /// in the trailing check position and rejects it elsewhere.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckPositionIsX_ShouldBeInterpretedAsTen()
    {
        // '1' computes to 'X'; concatenated sequence is therefore valid.
        Assert.IsTrue(Iso7064Mod11_2.IsValid("1X".AsSpan()));
        Assert.IsFalse(Iso7064Mod11_2.IsValid("X1".AsSpan()));
    }
}
