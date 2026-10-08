// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSetContractTests{T,T}.IUtf8SpanParsable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Contracts;

public abstract partial class CalendarSetContractTests<TSet, TElement>
{
    /// <summary>
    /// Verifies that <see cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider, out TSelf)" />
    /// answers, for the UTF-8 encoding of every parse input, what the string overload answers for the text, with the
    /// same set.
    /// </summary>
    [TestMethod]
    public void IUtf8SpanParsable_WhenTryParsingAnyText_ShouldMatchTheStringOverload()
    {
        foreach (string input in ParseInputs())
        {
            bool expected = TryParseString(input, null, out TSet fromString);
            bool actual = TSet.TryParse(Encoding.UTF8.GetBytes(input), null, out TSet fromUtf8);

            Assert.AreEqual(expected, actual, Describe(input));
            Assert.AreEqual(fromString, fromUtf8, Describe(input));
        }
    }

    /// <summary>
    /// Verifies that <see cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider)" /> returns, for
    /// the UTF-8 encoding of every valid parse input, the set the string overload returns.
    /// </summary>
    [TestMethod]
    public void IUtf8SpanParsable_WhenParsingValidText_ShouldReturnTheSetTheStringOverloadReturns()
    {
        foreach (string input in ParseInputs())
        {
            if (TryParseString(input, null, out TSet expected))
                Assert.AreEqual(expected, TSet.Parse(Encoding.UTF8.GetBytes(input), null), Describe(input));
        }
    }

    /// <summary>
    /// Verifies that <see cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider)" /> throws, for
    /// the UTF-8 encoding of every invalid parse input, the <see cref="FormatException" /> the string overload throws,
    /// with the same message.
    /// </summary>
    [TestMethod]
    public void IUtf8SpanParsable_WhenParsingInvalidText_ShouldThrowTheFormatExceptionTheStringOverloadThrows()
    {
        foreach (string input in ParseInputs())
        {
            if (TryParseString(input, null, out _))
                continue;

            byte[] utf8 = Encoding.UTF8.GetBytes(input);
            var expected = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = ParseString(input, null);
            }, Describe(input));

            var actual = Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = TSet.Parse(utf8, null);
            }, Describe(input));

            Assert.AreEqual(expected.Message, actual.Message, Describe(input));
        }
    }

    /// <summary>
    /// Verifies that <see cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider, out TSelf)" />
    /// returns <see langword="false" /> and the empty set for bytes that are not valid UTF-8, alone or after a valid
    /// character.
    /// </summary>
    [TestMethod]
    public void IUtf8SpanParsable_WhenTryParsingInvalidUtf8_ShouldReturnFalseAndEmptySet()
    {
        foreach (byte[] bytes in InvalidUtf8())
        {
            string description = Convert.ToHexString(bytes);

            Assert.IsFalse(TSet.TryParse(bytes, null, out TSet result), description);
            Assert.AreEqual(Empty, result, description);
        }
    }

    /// <summary>
    /// Verifies that <see cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider)" /> throws
    /// <see cref="FormatException" /> for bytes that are not valid UTF-8, alone or after a valid character.
    /// </summary>
    [TestMethod]
    public void IUtf8SpanParsable_WhenParsingInvalidUtf8_ShouldThrowFormatException()
    {
        foreach (byte[] bytes in InvalidUtf8())
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = TSet.Parse(bytes, null);
            }, Convert.ToHexString(bytes));
        }
    }

    /// <summary>
    /// Returns byte sequences that are not valid UTF-8: a byte that never occurs, a truncated sequence, an invalid
    /// byte after a digit, an encoded surrogate, a five-byte sequence, and a stray continuation byte after the text of
    /// the full set.
    /// </summary>
    /// <returns>The invalid sequences.</returns>
    private static IEnumerable<byte[]> InvalidUtf8() =>
    [
        [0xFF],
        [0xC3],
        [0x31, 0xFF],
        [0xED, 0xA0, 0x80],
        [0xF8, 0x88, 0x80, 0x80, 0x80],
        [.. Encoding.ASCII.GetBytes(All.ToString()), 0x80],
    ];
}
