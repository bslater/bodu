// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.ISpanParsable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <see cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider, out TSelf)" />
    /// answers, for every parse input, what <see cref="IParsable{TSelf}.TryParse(string, IFormatProvider, out TSelf)" />
    /// answers for the same text, with the same set.
    /// </summary>
    [TestMethod]
    public void ISpanParsable_WhenTryParsingAnyText_ShouldMatchTheStringOverload()
    {
        foreach (string input in ParseInputs())
        {
            bool expected = TryParseString(input, null, out TSet fromString);
            bool actual = TSet.TryParse(input.AsSpan(), null, out TSet fromSpan);

            Assert.AreEqual(expected, actual, Describe(input));
            Assert.AreEqual(fromString, fromSpan, Describe(input));
        }
    }

    /// <summary>
    /// Verifies that <see cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider)" /> returns the set
    /// the string overload returns for every valid parse input.
    /// </summary>
    [TestMethod]
    public void ISpanParsable_WhenParsingValidText_ShouldReturnTheSetTheStringOverloadReturns()
    {
        foreach (string input in ParseInputs())
        {
            if (TryParseString(input, null, out TSet expected))
                Assert.AreEqual(expected, TSet.Parse(input.AsSpan(), null), Describe(input));
        }
    }

    /// <summary>
    /// Verifies that <see cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider)" /> throws, for every
    /// invalid parse input, the <see cref="FormatException" /> the string overload throws, with the same message.
    /// </summary>
    [TestMethod]
    public void ISpanParsable_WhenParsingInvalidText_ShouldThrowTheFormatExceptionTheStringOverloadThrows()
    {
        foreach (string input in ParseInputs())
        {
            if (TryParseString(input, null, out _))
                continue;

            var expected = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = ParseString(input, null);
            }, Describe(input));

            var actual = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = TSet.Parse(input.AsSpan(), null);
            }, Describe(input));

            Assert.AreEqual(expected.Message, actual.Message, Describe(input));
        }
    }
}
