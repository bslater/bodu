// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetContractTests{T}.TryParseExact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Contracts;

public abstract partial class CalendarValueSetContractTests<TSet>
{
    /// <summary>
    /// Verifies that <c>TryParseExact</c> answers <see langword="true" /> with the set for the text <c>ToString</c>
    /// writes in each format.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenTextIsInTheFormat_ShouldReturnTrueAndSet()
    {
        foreach (TSet set in SampleSets().Append(All).Append(Empty))
        {
            foreach (string format in new[] { "G", "g", "L", "l", "B", "b", "0", "1", "01" })
            {
                bool parsed = TryParseExact(Format(set, format), format, out TSet result);

                Assert.IsTrue(parsed, $"set {ToUInt64(set):X}, {format}");
                Assert.AreEqual(set, result, $"set {ToUInt64(set):X}, {format}");
            }
        }
    }

    /// <summary>
    /// Verifies that <c>TryParseExact</c> answers <see langword="false" />, without throwing, with the empty set for
    /// text that is not in the format it names.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenTextIsNotInTheFormat_ShouldReturnFalseAndEmptySet()
    {
        string range = string.Create(CultureInfo.InvariantCulture, $"{Minimum}-{Minimum + 2}");
        (string Text, string Format)[] rows =
        [
            (range, "L"),
            (new string('0', DomainSize - 1), "B"),
            (new string('0', DomainSize + 1), "B"),
            (new string('0', DomainSize - 1) + "2", "B"),
            (FormatReference([Minimum, Maximum]), "B"),
            ("x", "G"),
        ];

        foreach ((string text, string format) in rows)
        {
            bool parsed = TryParseExact(text, format, out TSet result);

            Assert.IsFalse(parsed, $"'{text}', {format}");
            Assert.AreEqual(Empty, result, $"'{text}', {format}");
        }
    }

    /// <summary>
    /// Verifies that <c>TryParseExact</c> answers <see langword="false" />, without throwing, for a format the numeric
    /// sets do not define.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenFormatIsUnsupported_ShouldReturnFalse()
    {
        foreach (string format in new[] { "X", "GG", "10", string.Empty, " G", "Q" })
        {
            bool parsed = TryParseExact(string.Empty, format, out TSet result);

            Assert.IsFalse(parsed, $"'{format}'");
            Assert.AreEqual(Empty, result, $"'{format}'");
        }
    }

    /// <summary>
    /// Verifies that <c>TryParseExact</c> answers <see langword="false" /> with the empty set for a
    /// <see langword="null" /> text or format.
    /// </summary>
    [TestMethod]
    public void TryParseExact_WhenTextOrFormatIsNull_ShouldReturnFalseAndEmptySet()
    {
        Assert.IsFalse(TryParseExact(null, "G", out TSet fromNullText));
        Assert.AreEqual(Empty, fromNullText);
        Assert.IsFalse(TryParseExact(string.Empty, null, out TSet fromNullFormat));
        Assert.AreEqual(Empty, fromNullFormat);
    }
}
