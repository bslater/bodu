// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base64Tests.BclAliases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base64Tests
{
    /// <summary>
    /// Verifies that <see cref="Base64.ToBase64String(byte[])" /> returns the canonical Standard variant output.
    /// </summary>
    [TestMethod]
    public void ToBase64String_ForByteArray_ShouldReturnStandardVariantOutput()
    {
        string actual = Base64.ToBase64String(Ascii("foobar"));

        Assert.AreEqual("Zm9vYmFy", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.ToBase64String(byte[], int, int)" /> encodes only the requested slice.
    /// </summary>
    [TestMethod]
    public void ToBase64String_ForByteArraySlice_ShouldEncodeSliceOnly()
    {
        byte[] bytes = Ascii("xxxxfoobaryyyy");

        string actual = Base64.ToBase64String(bytes, 4, 6);

        Assert.AreEqual("Zm9vYmFy", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.ToBase64String(ReadOnlySpan{byte})" /> returns the Standard variant output.
    /// </summary>
    [TestMethod]
    public void ToBase64String_ForReadOnlySpan_ShouldReturnStandardVariantOutput()
    {
        string actual = Base64.ToBase64String(Ascii("foobar").AsSpan());

        Assert.AreEqual("Zm9vYmFy", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(string)" /> performs strict Standard variant decode.
    /// </summary>
    [TestMethod]
    public void FromBase64String_ForString_ShouldStrictlyDecode()
    {
        byte[] actual = Base64.FromBase64String("Zm9vYmFy");

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(string)" /> rejects unpadded input.
    /// </summary>
    [TestMethod]
    public void FromBase64String_WhenPaddingOmitted_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base64.FromBase64String("Zm9vYmE");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(ReadOnlySpan{byte})" /> decodes the UTF-8 form.
    /// </summary>
    [TestMethod]
    public void FromBase64String_ForUtf8Source_ShouldDecode()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy");

        byte[] actual = Base64.FromBase64String(utf8);

        CollectionAssert.AreEqual(Ascii("foobar"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryToBase64String(ReadOnlySpan{byte}, Span{char}, out int)" /> writes the
    /// Standard variant output into a character destination.
    /// </summary>
    [TestMethod]
    public void TryToBase64String_ForCharSpan_ShouldWriteStandardOutput()
    {
        char[] destination = new char[8];

        bool ok = Base64.TryToBase64String(Ascii("foobar").AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, charsWritten);
        Assert.AreEqual("Zm9vYmFy", new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.TryToBase64String(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes the
    /// Standard variant output as UTF-8 bytes.
    /// </summary>
    [TestMethod]
    public void TryToBase64String_ForUtf8Span_ShouldWriteStandardOutputAsAsciiBytes()
    {
        byte[] destination = new byte[8];

        bool ok = Base64.TryToBase64String(Ascii("foobar").AsSpan(), destination.AsSpan(), out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, bytesWritten);
        Assert.AreEqual("Zm9vYmFy", System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> reports
    /// <see cref="OperationStatus.Done" /> for valid input.
    /// </summary>
    [TestMethod]
    public void FromBase64String_ForCharSpan_OperationStatus_ShouldReturnDoneWithCounts()
    {
        byte[] destination = new byte[6];

        OperationStatus status = Base64.FromBase64String(
            "Zm9vYmFy".AsSpan(),
            destination,
            out int charsConsumed,
            out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(8, charsConsumed);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.InvalidData" /> for invalid characters.
    /// </summary>
    [TestMethod]
    public void FromBase64String_ForCharSpan_OperationStatus_WhenInvalidChar_ShouldReturnInvalidData()
    {
        byte[] destination = new byte[6];

        OperationStatus status = Base64.FromBase64String(
            "Zm@vYmFy".AsSpan(),
            destination,
            out int _,
            out int _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base64.FromBase64String(ReadOnlySpan{byte}, Span{byte}, out int, out int)" /> reports
    /// counts and returns <see cref="OperationStatus.Done" /> for UTF-8 input.
    /// </summary>
    [TestMethod]
    public void FromBase64String_ForUtf8Span_OperationStatus_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("Zm9vYmFy");
        byte[] destination = new byte[6];

        OperationStatus status = Base64.FromBase64String(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(8, bytesConsumed);
        Assert.AreEqual(6, bytesWritten);
        CollectionAssert.AreEqual(Ascii("foobar"), destination);
    }
}
