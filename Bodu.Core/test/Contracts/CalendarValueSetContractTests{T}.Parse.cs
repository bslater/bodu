// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetContractTests{T}.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Contracts;

public abstract partial class CalendarValueSetContractTests<TSet>
{
    /// <summary>
    /// Verifies that <c>Parse</c> reads each canonical text row as its expected set.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextIsCanonical_ShouldReturnExpectedSet()
    {
        foreach (ValidKat<string, TSet> kat in CanonicalCases())
            Assert.AreEqual(kat.Expected, Parse(kat.Input), kat.Name);
    }

    /// <summary>
    /// Verifies that <c>Parse</c> accepts text outside the canonical form: whitespace, any order, repeated values,
    /// overlapping ranges, adjacent values and leading zeros.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextIsNotCanonical_ShouldReturnExpectedSet()
    {
        foreach (ValidKat<string, TSet> kat in LenientCases())
            Assert.AreEqual(kat.Expected, Parse(kat.Input), kat.Name);
    }

    /// <summary>
    /// Verifies that <c>Parse</c> throws <see cref="FormatException" /> for text that is not a list of values and ranges
    /// of the domain.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextIsMalformed_ShouldThrowFormatException()
    {
        foreach (InvalidKat<string> kat in MalformedCases())
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = Parse(kat.Input);
            }, kat.Name);
        }
    }

    /// <summary>
    /// Verifies that the <see cref="FormatException" /> message quotes the text and names the domain's bounds.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextIsMalformed_ShouldNameTextAndDomainInMessage()
    {
        const string Text = "x";

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Parse(Text);
        });

        StringAssert.Contains(ex.Message, "'x'");
        StringAssert.Contains(ex.Message, Minimum.ToString(CultureInfo.CurrentCulture));
        StringAssert.Contains(ex.Message, Maximum.ToString(CultureInfo.CurrentCulture));
    }

    /// <summary>
    /// Verifies that <c>Parse</c> throws <see cref="ArgumentNullException" /> naming its parameter for
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Parse(null!);
        });

        Assert.AreEqual("s", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>Parse</c> reads the text <c>ToString</c> writes back as an equal set.
    /// </summary>
    [TestMethod]
    public void Parse_WhenGivenToStringOfSet_ShouldReturnEqualSet()
    {
        foreach (TSet set in SampleSets())
            Assert.AreEqual(set, Parse(set.ToString()!), $"set {ToUInt64(set):X}");
    }

    /// <summary>
    /// Verifies that every range and every pair of values in the domain survives a round trip through
    /// <c>ToString</c> and <c>Parse</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Parse_WhenGivenToStringOfEveryRangeAndPair_ShouldReturnEqualSet()
    {
        for (int low = 0; low < DomainSize; low++)
        {
            for (int high = low; high < DomainSize; high++)
            {
                ulong range = (DomainBits >> (DomainSize - (high - low + 1))) << low;
                ulong pair = (1UL << low) | (1UL << high);

                foreach (ulong bits in new[] { range, pair })
                {
                    TSet set = FromUInt64(bits);
                    string text = set.ToString()!;

                    Assert.AreEqual(set, Parse(text), $"bits {bits:X}, text '{text}'");
                }
            }
        }
    }
}
