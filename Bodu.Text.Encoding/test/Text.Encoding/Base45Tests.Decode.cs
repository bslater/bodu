// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base45Tests.Decode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base45Tests
{
    /// <summary>
    /// Verifies that <see cref="Base45.Decode(string, BaseFormatStyles)" /> throws for a null string.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullString_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base45.Decode((string)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base45.Decode(char[], int, int, BaseFormatStyles)" /> throws for a null character
    /// array.
    /// </summary>
    [TestMethod]
    public void Decode_WhenNullCharArray_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Base45.Decode((char[])null!, 0, 0);
        });
    }

    /// <summary>
    /// Verifies that decoding a three-character group greater than <c>65535</c> — the RFC 9285 §6 example
    /// <c>"GGW"</c> (65536) — throws a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenTripletOverflows_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base45.Decode("GGW");
        });

        Assert.IsTrue(ex.Message.Contains("range", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that decoding a two-character group greater than <c>255</c> throws a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenTrailingPairOverflows_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base45.Decode("::");
        });
    }

    /// <summary>
    /// Verifies that decoding an input whose length modulo three is one throws a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenLengthModuloThreeIsOne_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base45.Decode("A");
        });
    }

    /// <summary>
    /// Verifies that decoding an input containing a character outside the alphabet (lower case) throws a
    /// <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Decode_WhenCharacterNotInAlphabet_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base45.Decode("ab");
        });

        Assert.IsTrue(ex.Message.Contains("Base45 alphabet", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that <see cref="Base45.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> returns
    /// <see langword="false" /> when the destination buffer is too small, without throwing.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        Span<byte> destination = new byte[1];

        var success = Base45.TryDecode("BB8".AsSpan(), destination, out var written);

        Assert.IsFalse(success);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="Base45.Decode(char[], int, int, BaseFormatStyles)" /> decodes only the requested
    /// segment.
    /// </summary>
    [TestMethod]
    public void Decode_WhenOffsetAndCount_ShouldDecodeSegmentOnly()
    {
        char[] text = ['X', 'B', 'B', '8', 'X'];

        CollectionAssert.AreEqual("AB"u8.ToArray(), Base45.Decode(text, 1, 3));
    }
}
