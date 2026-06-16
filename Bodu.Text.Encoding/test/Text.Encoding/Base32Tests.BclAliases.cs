// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base32Tests.BclAliases.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base32Tests
{

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.Done" /> and reports consumed and written counts for valid input.
    /// </summary>
    [TestMethod]
    public void FromBase32String_ForCharSpan_OperationStatus_ShouldReturnDoneWithCounts()
    {
        byte[] destination = new byte[6];

        OperationStatus status = Base32.FromBase32String(
            "MZXW6YTBOI======".AsSpan(),
            destination,
            out int charsConsumed,
            out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(16, charsConsumed);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.InvalidData" /> for non-alphabet characters.
    /// </summary>
    [TestMethod]
    public void FromBase32String_ForCharSpan_OperationStatus_WhenInvalidChar_ShouldReturnInvalidData()
    {
        byte[] destination = new byte[6];

        OperationStatus status = Base32.FromBase32String(
            "MZ!W6YTBOI======".AsSpan(),
            destination,
            out int _,
            out int _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(string)" /> performs strict Standard variant decode.
    /// </summary>
    [TestMethod]
    public void FromBase32String_ForString_ShouldStrictlyDecode()
    {
        byte[] actual = Base32.FromBase32String("MZXW6YTBOI======");

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(ReadOnlySpan{byte})" /> decodes the UTF-8 encoded form.
    /// </summary>
    [TestMethod]
    public void FromBase32String_ForUtf8Source_ShouldDecode()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6YTBOI======");

        byte[] actual = Base32.FromBase32String(utf8);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(ReadOnlySpan{byte}, Span{byte}, out int, out int)" /> decodes
    /// UTF-8 input and returns <see cref="OperationStatus.Done" />.
    /// </summary>
    [TestMethod]
    public void FromBase32String_ForUtf8Span_OperationStatus_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("MZXW6YTBOI======");
        byte[] destination = new byte[6];

        OperationStatus status = Base32.FromBase32String(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(16, bytesConsumed);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.FromBase32String(string)" /> rejects unpadded input.
    /// </summary>
    [TestMethod]
    public void FromBase32String_WhenPaddingOmitted_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base32.FromBase32String("MZXW6YTBOI");
        });
    }
    /// <summary>
    /// Verifies that <see cref="Base32.ToBase32String(byte[])" /> returns the canonical Standard variant output.
    /// </summary>
    [TestMethod]
    public void ToBase32String_ForByteArray_ShouldReturnStandardVariantOutput()
    {
        string actual = Base32.ToBase32String(Ascii("foobar"));

        Assert.AreEqual("MZXW6YTBOI======", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.ToBase32String(byte[], int, int)" /> encodes only the requested slice.
    /// </summary>
    [TestMethod]
    public void ToBase32String_ForByteArraySlice_ShouldEncodeSliceOnly()
    {
        byte[] bytes = Ascii("xxxfoobaryyy");

        string actual = Base32.ToBase32String(bytes, 3, 6);

        Assert.AreEqual("MZXW6YTBOI======", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.ToBase32String(ReadOnlySpan{byte})" /> returns the Standard variant output.
    /// </summary>
    [TestMethod]
    public void ToBase32String_ForReadOnlySpan_ShouldReturnStandardVariantOutput()
    {
        string actual = Base32.ToBase32String(Ascii("foobar").AsSpan());

        Assert.AreEqual("MZXW6YTBOI======", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryToBase32String(ReadOnlySpan{byte}, Span{char}, out int)" /> writes Standard
    /// variant output into the destination span.
    /// </summary>
    [TestMethod]
    public void TryToBase32String_ForCharSpan_ShouldWriteStandardOutput()
    {
        char[] destination = new char[16];

        bool ok = Base32.TryToBase32String(Ascii("foobar").AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(16, charsWritten);
        Assert.AreEqual("MZXW6YTBOI======", new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base32.TryToBase32String(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes
    /// Standard variant output as UTF-8 bytes.
    /// </summary>
    [TestMethod]
    public void TryToBase32String_ForUtf8Span_ShouldWriteStandardOutputAsAsciiBytes()
    {
        byte[] destination = new byte[16];

        bool ok = Base32.TryToBase32String(Ascii("foobar").AsSpan(), destination.AsSpan(), out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(16, bytesWritten);
        Assert.AreEqual("MZXW6YTBOI======", System.Text.Encoding.ASCII.GetString(destination));
    }

}
