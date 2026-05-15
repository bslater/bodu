// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.BclAliases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.Done" /> and reports counts.
    /// </summary>
    [TestMethod]
    public void FromBase58String_ForCharSpan_OperationStatus_ShouldReturnDoneWithCounts()
    {
        byte[] destination = new byte[5];

        OperationStatus status = Base58.FromBase58String(
            "9Ajdvzr".AsSpan(),
            destination,
            out int charsConsumed,
            out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(7, charsConsumed);
        Assert.AreEqual(5, bytesWritten);
        CollectionAssert.AreEqual(Ascii("Hello"), destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.InvalidData" /> for invalid characters.
    /// </summary>
    [TestMethod]
    public void FromBase58String_ForCharSpan_OperationStatus_WhenInvalidChar_ShouldReturnInvalidData()
    {
        byte[] destination = new byte[10];

        OperationStatus status = Base58.FromBase58String(
            "9A0dvzr".AsSpan(),
            destination,
            out int _,
            out int _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(string)" /> performs strict decode.
    /// </summary>
    [TestMethod]
    public void FromBase58String_ForString_ShouldStrictlyDecode()
    {
        byte[] actual = Base58.FromBase58String("9Ajdvzr");

        CollectionAssert.AreEqual(Ascii("Hello"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(ReadOnlySpan{byte})" /> decodes UTF-8 input.
    /// </summary>
    [TestMethod]
    public void FromBase58String_ForUtf8Source_ShouldDecode()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("9Ajdvzr");

        byte[] actual = Base58.FromBase58String(utf8);

        CollectionAssert.AreEqual(Ascii("Hello"), actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.FromBase58String(ReadOnlySpan{byte}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.Done" /> for valid UTF-8 input.
    /// </summary>
    [TestMethod]
    public void FromBase58String_ForUtf8Span_OperationStatus_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("9Ajdvzr");
        byte[] destination = new byte[5];

        OperationStatus status = Base58.FromBase58String(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(7, bytesConsumed);
        Assert.AreEqual(5, bytesWritten);
        CollectionAssert.AreEqual(Ascii("Hello"), destination);
    }
    /// <summary>
    /// Verifies that <see cref="Base58.ToBase58String(byte[])" /> returns the Bitcoin/Flickr Base58 output.
    /// </summary>
    [TestMethod]
    public void ToBase58String_ForByteArray_ShouldReturnBitcoinFlickrOutput()
    {
        string actual = Base58.ToBase58String(Ascii("Hello"));

        Assert.AreEqual("9Ajdvzr", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryToBase58String(ReadOnlySpan{byte}, Span{char}, out int)" /> writes the
    /// Bitcoin/Flickr output into a destination char span.
    /// </summary>
    [TestMethod]
    public void TryToBase58String_ForCharSpan_ShouldWriteExpectedOutput()
    {
        char[] destination = new char[Base58.GetMaxEncodedLength(5)];

        bool ok = Base58.TryToBase58String(Ascii("Hello").AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual("9Ajdvzr", new string(destination, 0, charsWritten));
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryToBase58String(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes UTF-8
    /// bytes.
    /// </summary>
    [TestMethod]
    public void TryToBase58String_ForUtf8Span_ShouldWriteExpectedBytes()
    {
        byte[] destination = new byte[Base58.GetMaxEncodedLength(5)];

        bool ok = Base58.TryToBase58String(Ascii("Hello").AsSpan(), destination.AsSpan(), out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual("9Ajdvzr", System.Text.Encoding.ASCII.GetString(destination, 0, bytesWritten));
    }

}
