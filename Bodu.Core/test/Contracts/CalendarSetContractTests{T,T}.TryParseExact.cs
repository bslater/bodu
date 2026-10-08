// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.TryParseExact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that
    /// <see cref="ICalendarValueSet{TSelf, TValue}.TryParseExact(ReadOnlySpan{char}, ReadOnlySpan{char}, out TSelf)" />
    /// returns <see langword="true" /> and every sample set for the text <c>ToString</c> writes in each format.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenSpanIsTextInItsFormat_ShouldReturnTrueAndTheSet()
    {
        foreach (TSet set in SampleSets())
        {
            foreach (string format in Formats)
            {
                string text = set.ToString(format, null);

                Assert.IsTrue(TSet.TryParseExact(text.AsSpan(), format.AsSpan(), out TSet result), $"'{text}', '{format}'");
                Assert.AreEqual(set, result, $"'{text}', '{format}'");
            }
        }
    }

    /// <summary>
    /// Verifies that
    /// <see cref="ICalendarValueSet{TSelf, TValue}.TryParseExact(ReadOnlySpan{char}, ReadOnlySpan{char}, out TSelf)" />
    /// answers, for every parse input in every format and in formats none of the sets accepts, what the string
    /// overload answers, with the same set.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenSpansAreAnyTextAndFormat_ShouldMatchTheStringOverload()
    {
        string[] formats = [.. Formats, .. UnsupportedFormats];

        foreach (string input in ParseInputs())
        {
            foreach (string format in formats)
            {
                bool expected = TSet.TryParseExact(input, format, out TSet fromString);
                bool actual = TSet.TryParseExact(input.AsSpan(), format.AsSpan(), out TSet fromSpan);

                Assert.AreEqual(expected, actual, $"{Describe(input)}, '{format}'");
                Assert.AreEqual(fromString, fromSpan, $"{Describe(input)}, '{format}'");
            }
        }
    }
}
