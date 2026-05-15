// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Decode.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{
    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with the default strict mode decodes the
    /// canonical lower case input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictLowerCaseString_ShouldReturnExpectedBytes()
    {
        byte[] actual = Base16.Decode(CanonicalHexLower);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> decodes upper case input without
    /// requiring a flag.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictUpperCaseString_ShouldReturnExpectedBytes()
    {
        byte[] actual = Base16.Decode(CanonicalHexUpper);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> decodes mixed-case input without
    /// requiring a flag.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictMixedCaseString_ShouldReturnExpectedBytes()
    {
        byte[] actual = Base16.Decode("DeAdBeEf");

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> throws <see cref="FormatException" />
    /// when the input has an odd number of hex digits.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictOddLengthString_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("abc");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> throws <see cref="FormatException" />
    /// for any non-hex character.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictInvalidCharacter_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("xx");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> strips a leading <c>0x</c>.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndPrefixPresent_ShouldStripPrefix()
    {
        byte[] actual = Base16.Decode("0xDEADBEEF", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> tolerates the upper case <c>0X</c> prefix.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndUpperCasePrefix_ShouldStripPrefix()
    {
        byte[] actual = Base16.Decode("0XDEADBEEF", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> preserves a leading <c>0</c> digit when no <c>x</c> follows.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndLeadingZeroNoX_ShouldPreserveDigit()
    {
        byte[] actual = Base16.Decode("0FAB", BaseFormatStyles.AllowPrefix);

        CollectionAssert.AreEqual(new byte[] { 0x0F, 0xAB }, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> strips spaces, tabs, and newlines.
    /// </summary>
    [TestMethod]
    public void Decode_WhenIgnoreWhitespace_ShouldStripAllAsciiWhitespace()
    {
        byte[] actual = Base16.Decode("DE AD\tBE\nEF\r", BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> with both leniency flags accepts a fully
    /// decorated input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenAllowPrefixAndIgnoreWhitespace_ShouldStripBoth()
    {
        byte[] actual = Base16.Decode(
            "0x DE AD BE EF",
            BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(string, BaseFormatStyles)" /> in strict mode rejects a prefix-decorated
    /// input.
    /// </summary>
    [TestMethod]
    public void Decode_WhenStrictAndPrefixPresent_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("0xDEADBEEF");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(ReadOnlySpan{char}, BaseFormatStyles)" /> decodes a stack-allocated
    /// span correctly.
    /// </summary>
    [TestMethod]
    public void Decode_WhenReadOnlySpan_ShouldReturnExpectedBytes()
    {
        ReadOnlySpan<char> chars = CanonicalHexLower.AsSpan();

        byte[] actual = Base16.Decode(chars);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(char[], int, int, BaseFormatStyles)" /> decodes only the bytes inside
    /// the requested slice.
    /// </summary>
    [TestMethod]
    public void Decode_WhenSliceForCharArray_ShouldReturnSliceOnly()
    {
        char[] chars = "00deadbeef00".ToCharArray();

        byte[] actual = Base16.Decode(chars, 2, 8);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(char[], int, int, BaseFormatStyles)" /> rejects a count larger than the
    /// array length with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCountExceedsCharArrayLength_ShouldThrowArgumentOutOfRangeException()
    {
        char[] chars = "abcd".ToCharArray();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Base16.Decode(chars, 2, 10);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.Decode(char[], int, int, BaseFormatStyles)" /> rejects a slice whose offset
    /// plus count overflows the array bounds with <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenOffsetPlusCountOverflowsCharArray_ShouldThrowArgumentException()
    {
        char[] chars = "abcd".ToCharArray();

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Base16.Decode(chars, 2, 3);
        });
    }

    /// <summary>
    /// Verifies that lenient decoding still rejects an odd digit count after decorations are stripped.
    /// </summary>
    [TestMethod]
    public void Decode_WhenLenientYieldsOddDigitCount_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.Decode("0x ABC", BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);
        });
    }
}
