// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iso7064Mod97_10Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Iso7064Mod97_10" /> check-code algorithm.
/// </summary>
[TestClass]
public sealed class Iso7064Mod97_10Tests : MultiCharCheckDigitAlgorithmTests<Iso7064Mod97_10Tests, Iso7064Mod97_10>
{
    /// <inheritdoc />
    protected override MultiCharCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ISO 7064 MOD 97-10",
        InputAlphabet = CheckDigitInputAlphabet.AlphanumericUppercase,
        CheckLength = 2,
        EmptyCheckDigits = "01",
        KnownAnswers =
        [
            new() { Name = "StandardExampleNumeric", Body = "794",                ExpectedCheck = "44" },
            new() { Name = "IbanRearranged",         Body = "WEST12345698765432GB", ExpectedCheck = "82" },
            new() { Name = "LeiBody",                Body = "54930084UKLVMY22DS",   ExpectedCheck = "16" },
            new() { Name = "AllLetters",             Body = "ABCDEFGHIJ",           ExpectedCheck = "46" },
        ],
    };

    /// <inheritdoc />
    protected override string ComputeStatic(ReadOnlySpan<char> body) =>
        Iso7064Mod97_10.Compute(body);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Iso7064Mod97_10.IsValid(valueIncludingCheck);
}
