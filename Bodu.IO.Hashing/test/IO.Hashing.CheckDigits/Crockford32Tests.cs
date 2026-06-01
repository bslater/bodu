// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Crockford32Tests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.Checksums;

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Contains unit tests for the <see cref="Crockford32" /> check-digit algorithm.
/// </summary>
[TestClass]
public sealed class Crockford32Tests
    : AlphanumericCheckDigitAlgorithmTests<Crockford32Tests, Crockford32>
{
    /// <summary>
    /// Verifies that <see cref="Crockford32.Compute(ReadOnlySpan{char})" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the body contains a character outside the Crockford Base32
    /// alphabet.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsInvalidCharacter_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Crockford32.Compute("16!".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Crockford32.Compute(ReadOnlySpan{char})" /> rejects <c>'U'</c> in the body, because
    /// <c>'U'</c> is reserved as a check-only symbol and is excluded from the encoding alphabet.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyContainsLetterU_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Crockford32.Compute("1U".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that decoding is case-insensitive: a lowercase body produces the same check symbol as its uppercase
    /// equivalent.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyIsLowercase_ShouldMatchUppercase() => Assert.AreEqual(Crockford32.Compute("16J".AsSpan()), Crockford32.Compute("16j".AsSpan()));

    /// <summary>
    /// Verifies that the Crockford decode aliases hold: <c>'O'</c> maps to <c>0</c> and <c>'I'</c>/<c>'L'</c> map to
    /// <c>1</c>, so the aliased bodies decode identically to their canonical digit forms.
    /// </summary>
    [TestMethod]
    public void Compute_WhenBodyUsesDecodeAliases_ShouldMatchCanonicalDigits()
    {
        Assert.AreEqual(Crockford32.Compute("0".AsSpan()), Crockford32.Compute("O".AsSpan()));
        Assert.AreEqual(Crockford32.Compute("1".AsSpan()), Crockford32.Compute("I".AsSpan()));
        Assert.AreEqual(Crockford32.Compute("1".AsSpan()), Crockford32.Compute("L".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Crockford32.IsValid(ReadOnlySpan{char})" /> accepts a lowercase trailing check
    /// symbol, since check-symbol decoding is also case-insensitive.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenCheckSymbolIsLowercase_ShouldReturnTrue() => Assert.IsTrue(Crockford32.IsValid("16Jd".AsSpan()));

    /// <summary>
    /// Verifies that <see cref="Crockford32.IsValid(ReadOnlySpan{char})" /> rejects the empty span.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenSequenceIsEmpty_ShouldReturnFalse() => Assert.IsFalse(Crockford32.IsValid([]));

    /// <inheritdoc />
    protected override char ComputeStatic(ReadOnlySpan<char> body) =>
        Crockford32.Compute(body);

    /// <inheritdoc />
    protected override AlphanumericCheckDigitAlgorithmSpecification GetSpecification() => new()
    {
        AlgorithmName = "Crockford Base32",
        InputAlphabet = CheckDigitInputAlphabet.CrockfordBase32,
        OutputAlphabet = CheckDigitOutputAlphabet.CrockfordBase32Check,
        EmptyCheckDigit = '0',
        KnownAnswers =
        [
            new() { Name = "Canonical",     Body = "16J", ExpectedCheck = 'D' },
            new() { Name = "MaxDigit",      Body = "Z",   ExpectedCheck = 'Z' },
            new() { Name = "TwoSymbols",    Body = "ZZ",  ExpectedCheck = 'R' },
            new() { Name = "CheckStar",     Body = "10",  ExpectedCheck = '*' },
            new() { Name = "CheckTilde",    Body = "11",  ExpectedCheck = '~' },
            new() { Name = "CheckDollar",   Body = "12",  ExpectedCheck = '$' },
            new() { Name = "CheckEquals",   Body = "13",  ExpectedCheck = '=' },
            new() { Name = "CheckLetterU",  Body = "14",  ExpectedCheck = 'U' },
        ],
    };

    /// <inheritdoc />
    protected override bool IsValidStatic(ReadOnlySpan<char> valueIncludingCheck) =>
        Crockford32.IsValid(valueIncludingCheck);
}
