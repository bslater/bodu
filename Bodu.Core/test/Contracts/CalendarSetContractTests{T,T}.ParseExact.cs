// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.ParseExact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <see cref="ICalendarValueSet{TSelf, TValue}.ParseExact(ReadOnlySpan{char}, ReadOnlySpan{char})" />
    /// reads every sample set back from the text <c>ToString</c> writes in each format.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenSpanIsTextInItsFormat_ShouldReadTheSetBack()
    {
        foreach (TSet set in SampleSets())
        {
            foreach (string format in Formats)
            {
                string text = set.ToString(format, null);

                Assert.AreEqual(set, TSet.ParseExact(text.AsSpan(), format.AsSpan()), $"'{text}', '{format}'");
            }
        }
    }

    /// <summary>
    /// Verifies that
    /// <see cref="ICalendarValueSet{TSelf, TValue}.ParseExact(ReadOnlySpan{char}, ReadOnlySpan{char})" /> throws, for
    /// text the string overload rejects in a format and for a format it rejects, the <see cref="FormatException" />
    /// the string overload throws, with the same message.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenSpansAreRejected_ShouldThrowTheFormatExceptionTheStringOverloadThrows()
    {
        string[] inputs = ["x", "1,2,", new string('1', DomainSize + 1), All.ToString()];
        string[] formats = [.. Formats, .. UnsupportedFormats];

        foreach (string input in inputs)
        {
            foreach (string format in formats)
            {
                if (TSet.TryParseExact(input, format, out _))
                    continue;

                var expected = Assert.ThrowsExactly<FormatException>(() =>
                {
                    _ = TSet.ParseExact(input, format);
                }, $"{Describe(input)}, '{format}'");

                var actual = Assert.ThrowsExactly<FormatException>(() =>
                {
                    _ = TSet.ParseExact(input.AsSpan(), format.AsSpan());
                }, $"{Describe(input)}, '{format}'");

                Assert.AreEqual(expected.Message, actual.Message, $"{Describe(input)}, '{format}'");
            }
        }
    }
}
