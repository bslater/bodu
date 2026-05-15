// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.GetDecodedLength.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{
    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> returns half of the
    /// input length for strict input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenStrictEvenInput_ShouldReturnHalf()
    {
        Assert.AreEqual(4, Base16.GetDecodedLength("DEADBEEF".AsSpan()));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> throws
    /// <see cref="FormatException" /> for an odd-length strict input.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenStrictOddInput_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.GetDecodedLength("abc".AsSpan());
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.AllowPrefix" /> strips the prefix before counting.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenAllowPrefix_ShouldExcludePrefixFromCount()
    {
        int actual = Base16.GetDecodedLength("0xDEADBEEF".AsSpan(), BaseFormatStyles.AllowPrefix);

        Assert.AreEqual(4, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetDecodedLength(ReadOnlySpan{char}, BaseFormatStyles)" /> with
    /// <see cref="BaseFormatStyles.IgnoreWhitespace" /> strips whitespace before counting.
    /// </summary>
    [TestMethod]
    public void GetDecodedLength_WhenIgnoreWhitespace_ShouldExcludeWhitespaceFromCount()
    {
        int actual = Base16.GetDecodedLength("DE AD BE EF".AsSpan(), BaseFormatStyles.IgnoreWhitespace);

        Assert.AreEqual(4, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryGetDecodedLength(ReadOnlySpan{char}, out int, BaseFormatStyles)" /> returns
    /// <see langword="true" /> for valid input and reports the byte count.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenStrictEvenInput_ShouldReturnTrueAndCount()
    {
        bool ok = Base16.TryGetDecodedLength("DEADBEEF".AsSpan(), out int byteCount);

        Assert.IsTrue(ok);
        Assert.AreEqual(4, byteCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryGetDecodedLength(ReadOnlySpan{char}, out int, BaseFormatStyles)" /> returns
    /// <see langword="false" /> rather than throwing on odd-length input.
    /// </summary>
    [TestMethod]
    public void TryGetDecodedLength_WhenOddInput_ShouldReturnFalseAndZero()
    {
        bool ok = Base16.TryGetDecodedLength("abc".AsSpan(), out int byteCount);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, byteCount);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.GetEncodedLength(int)" /> (the no-options overload) returns twice the byte
    /// count.
    /// </summary>
    [TestMethod]
    public void GetEncodedLength_WithoutOptionsOverload_ShouldReturnTwiceByteCount()
    {
        Assert.AreEqual(8, Base16.GetEncodedLength(4));
    }
}
