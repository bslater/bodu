// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetContractTests{T}.ToString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Contracts;

public abstract partial class CalendarValueSetContractTests<TSet>
{
    /// <summary>
    /// Verifies that <c>ToString</c> writes the canonical text of each canonical row's set.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetHasCanonicalRow_ShouldWriteCanonicalText()
    {
        foreach (ValidKat<string, TSet> kat in CanonicalCases())
            Assert.AreEqual(kat.Input, kat.Expected.ToString(), kat.Name);
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes the empty string for the empty set.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetIsEmpty_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, Empty.ToString());
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes a set of one value as that value alone.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetSelectsOneValue_ShouldWriteThatValue()
    {
        foreach (int value in Domain)
            Assert.AreEqual(value.ToString(CultureInfo.InvariantCulture), Create(value).ToString());
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes two consecutive values as a range rather than a list.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTwoValuesAreConsecutive_ShouldWriteRange()
    {
        string expected = string.Create(CultureInfo.InvariantCulture, $"{Minimum}-{Minimum + 1}");

        Assert.AreEqual(expected, Create(Minimum, Minimum + 1).ToString());
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes each sample set as a plain reading of the format does, with the values
    /// read from the set's bits.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSetSelectsValues_ShouldMatchReferenceFormat()
    {
        foreach (TSet set in SampleSets())
        {
            string expected = FormatReference(ValuesOf(ToUInt64(set)));

            Assert.AreEqual(expected, set.ToString(), $"set {ToUInt64(set):X}");
        }
    }

    /// <summary>
    /// Verifies that <c>ToString</c> writes the same text whatever the current culture.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCurrentCultureChanges_ShouldWriteSameText()
    {
        TSet set = SampleSets().Last();
        string invariant = FormatReference(ValuesOf(ToUInt64(set)));
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            foreach (string name in new[] { "ar-SA", "fa-IR", "fr-FR", "hi-IN" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);

                Assert.AreEqual(invariant, set.ToString(), name);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// Verifies that <c>ToString</c> with a <see langword="null" /> or empty format writes the canonical text, as
    /// <c>ToString()</c> does.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsNullOrEmpty_ShouldWriteCanonicalText()
    {
        foreach (TSet set in SampleSets())
        {
            Assert.AreEqual(set.ToString(), Format(set, null), $"set {ToUInt64(set):X}, null");
            Assert.AreEqual(set.ToString(), Format(set, string.Empty), $"set {ToUInt64(set):X}, empty");
        }
    }

    /// <summary>
    /// Verifies that the <c>G</c> format, in either case, writes the canonical text, ranges included.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsG_ShouldWriteCanonicalText()
    {
        foreach (TSet set in SampleSets())
        {
            string expected = FormatReference(ValuesOf(ToUInt64(set)));

            Assert.AreEqual(expected, Format(set, "G"), $"set {ToUInt64(set):X}, G");
            Assert.AreEqual(expected, Format(set, "g"), $"set {ToUInt64(set):X}, g");
        }
    }

    /// <summary>
    /// Verifies that the <c>L</c> format, in either case, lists every value the set selects, ascending, without ranges.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsL_ShouldListEveryValue()
    {
        foreach (TSet set in SampleSets().Append(All).Append(Empty))
        {
            string expected = ValuesReference(ToUInt64(set));

            Assert.AreEqual(expected, Format(set, "L"), $"set {ToUInt64(set):X}, L");
            Assert.AreEqual(expected, Format(set, "l"), $"set {ToUInt64(set):X}, l");
        }
    }

    /// <summary>
    /// Verifies that each binary format, <c>B</c> in either case, <c>0</c>, <c>1</c> and <c>01</c>, writes one digit per
    /// value of the domain, lowest value first.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsBinary_ShouldWriteOneDigitPerValueLowestFirst()
    {
        foreach (TSet set in SampleSets().Append(All).Append(Empty))
        {
            string expected = BinaryReference(ToUInt64(set));

            foreach (string format in new[] { "B", "b", "0", "1", "01" })
                Assert.AreEqual(expected, Format(set, format), $"set {ToUInt64(set):X}, {format}");
        }
    }

    /// <summary>
    /// Verifies that the binary format writes the domain's lowest value as the first digit and its highest as the last.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsBinary_ShouldPutTheLowestValueFirst()
    {
        string lowest = Format(Create(Minimum), "B");
        string highest = Format(Create(Maximum), "B");

        Assert.AreEqual('1' + new string('0', DomainSize - 1), lowest);
        Assert.AreEqual(new string('0', DomainSize - 1) + '1', highest);
    }

    /// <summary>
    /// Verifies that <c>ToString</c> throws <see cref="FormatException" /> for a format the numeric sets do not define,
    /// with a message that quotes the format and names the type.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatIsUnsupported_ShouldThrowFormatException()
    {
        foreach (string format in new[] { "X", "GG", "10", "LG", " G", "S", "Q" })
        {
            var ex = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = Format(All, format);
            }, format);

            StringAssert.Contains(ex.Message, $"'{format}'");
            StringAssert.Contains(ex.Message, typeof(TSet).Name);
        }
    }
}
