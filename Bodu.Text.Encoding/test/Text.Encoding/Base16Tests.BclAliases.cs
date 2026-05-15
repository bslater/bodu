// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.BclAliases.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base16Tests
{

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.Done" /> for valid strict input and reports the consumed and written counts.
    /// </summary>
    [TestMethod]
    public void FromHexString_ForCharSpan_OperationStatus_ShouldReturnDoneWithCounts()
    {
        byte[] destination = new byte[4];

        OperationStatus status = Base16.FromHexString(
            CanonicalHexLower.AsSpan(),
            destination,
            out int charsConsumed,
            out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(8, charsConsumed);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> reports
    /// <see cref="OperationStatus.DestinationTooSmall" /> when the destination cannot fit the result.
    /// </summary>
    [TestMethod]
    public void FromHexString_ForCharSpan_WhenDestinationTooSmall_ShouldReturnDestinationTooSmall()
    {
        byte[] destination = new byte[1];

        OperationStatus status = Base16.FromHexString(
            CanonicalHexLower.AsSpan(),
            destination,
            out int _,
            out int _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> reports
    /// <see cref="OperationStatus.InvalidData" /> for non-hex input.
    /// </summary>
    [TestMethod]
    public void FromHexString_ForCharSpan_WhenInvalidCharacter_ShouldReturnInvalidData()
    {
        byte[] destination = new byte[4];

        OperationStatus status = Base16.FromHexString(
            "xyz!".AsSpan(),
            destination,
            out int _,
            out int _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(string)" /> rejects decorated input that the relaxed
    /// <see cref="Base16.Decode(string, BaseFormatStyles)" /> would accept.
    /// </summary>
    [TestMethod]
    public void FromHexString_ForDecoratedInput_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Base16.FromHexString("0xDEADBEEF");
        });
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(string)" /> performs strict decoding (no leniency).
    /// </summary>
    [TestMethod]
    public void FromHexString_ForString_ShouldStrictlyDecode()
    {
        byte[] actual = Base16.FromHexString(CanonicalHexUpper);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{byte})" /> decodes UTF-8 encoded hexadecimal input.
    /// </summary>
    [TestMethod]
    public void FromHexString_ForUtf8Source_ShouldDecodeAsAsciiHex()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(CanonicalHexLower);

        byte[] actual = Base16.FromHexString(utf8);

        CollectionAssert.AreEqual(CanonicalBytes, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.FromHexString(ReadOnlySpan{byte}, Span{byte}, out int, out int)" /> decodes
    /// UTF-8 input and returns <see cref="OperationStatus.Done" />.
    /// </summary>
    [TestMethod]
    public void FromHexString_ForUtf8Span_OperationStatus_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(CanonicalHexLower);
        byte[] destination = new byte[4];

        OperationStatus status = Base16.FromHexString(utf8, destination, out int bytesConsumed, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(8, bytesConsumed);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(CanonicalBytes, destination);
    }
    /// <summary>
    /// Verifies that <see cref="Base16.ToHexString(byte[])" /> returns the upper case hex form, matching
    /// <see cref="Convert.ToHexString(byte[])" /> conventions.
    /// </summary>
    [TestMethod]
    public void ToHexString_ForByteArray_ShouldReturnUpperCaseHex()
    {
        string actual = Base16.ToHexString(CanonicalBytes);

        Assert.AreEqual(CanonicalHexUpper, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.ToHexString(byte[], int, int)" /> encodes only the requested slice.
    /// </summary>
    [TestMethod]
    public void ToHexString_ForByteArraySlice_ShouldEncodeSliceOnly()
    {
        string actual = Base16.ToHexString(CanonicalBytes, 1, 2);

        Assert.AreEqual("ADBE", actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.ToHexString(ReadOnlySpan{byte})" /> returns the upper case hex form.
    /// </summary>
    [TestMethod]
    public void ToHexString_ForReadOnlySpan_ShouldReturnUpperCaseHex()
    {
        string actual = Base16.ToHexString(CanonicalBytes.AsSpan());

        Assert.AreEqual(CanonicalHexUpper, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.ToHexStringLower(byte[])" /> returns the lower case hex form.
    /// </summary>
    [TestMethod]
    public void ToHexStringLower_ForByteArray_ShouldReturnLowerCaseHex()
    {
        string actual = Base16.ToHexStringLower(CanonicalBytes);

        Assert.AreEqual(CanonicalHexLower, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryToHexString(ReadOnlySpan{byte}, Span{char}, out int)" /> writes upper case
    /// hex into the destination span.
    /// </summary>
    [TestMethod]
    public void TryToHexString_ForCharSpan_ShouldWriteUpperCaseHex()
    {
        char[] destination = new char[8];

        bool ok = Base16.TryToHexString(CanonicalBytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, charsWritten);
        Assert.AreEqual(CanonicalHexUpper, new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryToHexString(ReadOnlySpan{byte}, Span{byte}, out int)" /> writes upper case
    /// hex as UTF-8 bytes.
    /// </summary>
    [TestMethod]
    public void TryToHexString_ForUtf8Span_ShouldWriteUpperCaseHexAsAsciiBytes()
    {
        byte[] destination = new byte[8];

        bool ok = Base16.TryToHexString(CanonicalBytes.AsSpan(), destination.AsSpan(), out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, bytesWritten);
        Assert.AreEqual(CanonicalHexUpper, System.Text.Encoding.ASCII.GetString(destination));
    }

    /// <summary>
    /// Verifies that <see cref="Base16.TryToHexStringLower(ReadOnlySpan{byte}, Span{char}, out int)" /> writes lower
    /// case hex into the destination span.
    /// </summary>
    [TestMethod]
    public void TryToHexStringLower_ForCharSpan_ShouldWriteLowerCaseHex()
    {
        char[] destination = new char[8];

        bool ok = Base16.TryToHexStringLower(CanonicalBytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual(8, charsWritten);
        Assert.AreEqual(CanonicalHexLower, new string(destination));
    }

}
