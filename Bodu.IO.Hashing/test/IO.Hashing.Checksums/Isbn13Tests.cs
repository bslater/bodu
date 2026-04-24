// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Isbn13Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Isbn13" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class Isbn13Tests : CheckDigitAlgorithmTests<Isbn13Tests, Isbn13>
{
    /// <inheritdoc />
    protected override CheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "ISBN-13",
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Empty",               Body = "",              ExpectedCheck = '0' },
            new() { Name = "SingleZero",          Body = "0",             ExpectedCheck = '0' },
            new() { Name = "WikipediaISBN13",     Body = "978030640615",  ExpectedCheck = '7' },
            new() { Name = "KnuthVolume1",        Body = "978020137962",  ExpectedCheck = '4' },
            new() { Name = "AllZeros",            Body = "000000000000",  ExpectedCheck = '0' },
        ],
    };

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> digits) =>
        Isbn13.Compute(digits);

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> digitsIncludingCheck) =>
        Isbn13.IsValid(digitsIncludingCheck);
}
