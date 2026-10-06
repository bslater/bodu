// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetContractTests{T}.ParseExact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Contracts;

public abstract partial class CalendarValueSetContractTests<TSet>
{
    /// <summary>
    /// Verifies that <c>ParseExact</c> with the <c>G</c> format, in either case, reads every valid list, canonical or
    /// not, ranges included.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsG_ShouldReadListsWithRanges()
    {
        foreach (ValidKat<string, TSet> kat in CanonicalCases().Concat(LenientCases()))
        {
            Assert.AreEqual(kat.Expected, ParseExact(kat.Input, "G"), $"{kat.Name}, G");
            Assert.AreEqual(kat.Expected, ParseExact(kat.Input, "g"), $"{kat.Name}, g");
        }
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> with the <c>L</c> format reads the text the <c>L</c> format writes back as an
    /// equal set.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsL_ShouldReadValueLists()
    {
        foreach (TSet set in SampleSets().Append(All).Append(Empty))
            Assert.AreEqual(set, ParseExact(ValuesReference(ToUInt64(set)), "L"), $"set {ToUInt64(set):X}");
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> with the <c>L</c> format throws <see cref="FormatException" /> for a list that
    /// holds a range, which only the <c>G</c> format reads.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsLAndTextHasARange_ShouldThrowFormatException()
    {
        string text = string.Create(CultureInfo.InvariantCulture, $"{Minimum}-{Minimum + 2}");

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = ParseExact(text, "L");
        });
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> with each binary format reads one digit per value of the domain, lowest value
    /// first.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsBinary_ShouldReadOneDigitPerValueLowestFirst()
    {
        foreach (TSet set in SampleSets().Append(All).Append(Empty))
        {
            string text = BinaryReference(ToUInt64(set));

            foreach (string format in new[] { "B", "b", "0", "1", "01" })
                Assert.AreEqual(set, ParseExact(text, format), $"set {ToUInt64(set):X}, {format}");
        }
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> with a binary format throws <see cref="FormatException" /> for text one digit
    /// shorter or longer than the domain, with a message that gives the length it requires.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryTextHasTheWrongLength_ShouldThrowFormatException()
    {
        foreach (int length in new[] { DomainSize - 1, DomainSize + 1, 0 })
        {
            string text = new('0', length);

            var ex = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = ParseExact(text, "B");
            }, $"length {length}");

            StringAssert.Contains(ex.Message, DomainSize.ToString(CultureInfo.CurrentCulture));
        }
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> with a binary format throws <see cref="FormatException" /> for a character other
    /// than <c>0</c> or <c>1</c>, with a message that quotes the character and gives its one-based position.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenBinaryTextHasAnotherCharacter_ShouldThrowFormatException()
    {
        string text = new string('0', DomainSize - 1) + "2";

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = ParseExact(text, "B");
        });

        StringAssert.Contains(ex.Message, "'2'");
        StringAssert.Contains(ex.Message, DomainSize.ToString(CultureInfo.CurrentCulture));
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> reads a text only in the format it names: the binary text is not a list, and a
    /// list is not the binary text.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenTextIsInAnotherFormat_ShouldThrowFormatException()
    {
        TSet set = Create(Minimum, Maximum);
        string binary = BinaryReference(ToUInt64(set));
        string list = FormatReference(ValuesOf(ToUInt64(set)));

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = ParseExact(binary, "G");
        }, "binary text under G");
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = ParseExact(list, "B");
        }, "list under B");
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> throws <see cref="FormatException" /> for a format the numeric sets do not
    /// define, with a message that quotes the format and names the type.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsUnsupported_ShouldThrowFormatException()
    {
        foreach (string format in new[] { "X", "GG", "10", string.Empty, " G", "Q" })
        {
            var ex = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = ParseExact(string.Empty, format);
            }, $"'{format}'");

            StringAssert.Contains(ex.Message, $"'{format}'");
            StringAssert.Contains(ex.Message, typeof(TSet).Name);
        }
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> throws <see cref="ArgumentNullException" /> naming its parameter for a
    /// <see langword="null" /> text.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ParseExact(null!, "G");
        });

        Assert.AreEqual("s", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>ParseExact</c> throws <see cref="ArgumentNullException" /> naming its parameter for a
    /// <see langword="null" /> format.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenFormatIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ParseExact(string.Empty, null!);
        });

        Assert.AreEqual("format", ex.ParamName);
    }

    /// <summary>
    /// Verifies that every set of one value, and every sample set, survives a round trip through <c>ToString</c> and
    /// <c>ParseExact</c> in each format.
    /// </summary>
    [TestMethod]
    public void ParseExact_WhenGivenToStringOfSetInEachFormat_ShouldReturnEqualSet()
    {
        IEnumerable<TSet> sets = Domain.Select(value => Create(value)).Concat(SampleSets()).Append(All).Append(Empty);
        foreach (TSet set in sets)
        {
            foreach (string format in new[] { "G", "L", "B" })
                Assert.AreEqual(set, ParseExact(Format(set, format), format), $"set {ToUInt64(set):X}, {format}");
        }
    }
}
