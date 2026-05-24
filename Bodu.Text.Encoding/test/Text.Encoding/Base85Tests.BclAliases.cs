// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.BclAliases.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{

    /// <summary>
    /// Verifies that <see cref="Base85.FromBase85String(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.Done" /> for valid input.
    /// </summary>
    [TestMethod]
    public void FromBase85String_ForCharSpan_OperationStatus_ShouldReturnDoneWithCounts()
    {
        var original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var encoded = Base85.Encode(original);
        var destination = new byte[4];

        OperationStatus status = Base85.FromBase85String(
            encoded.AsSpan(),
            destination,
            out var charsConsumed,
            out var bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(encoded.Length, charsConsumed);
        Assert.AreEqual(4, bytesWritten);
        CollectionAssert.AreEqual(original, destination);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.FromBase85String(ReadOnlySpan{char}, Span{byte}, out int, out int)" /> returns
    /// <see cref="OperationStatus.InvalidData" /> for non-alphabet characters.
    /// </summary>
    [TestMethod]
    public void FromBase85String_ForCharSpan_OperationStatus_WhenInvalidChar_ShouldReturnInvalidData()
    {
        var destination = new byte[10];

        OperationStatus status = Base85.FromBase85String(
            "\x01\x02\x03\x04\x05".AsSpan(),
            destination,
            out var _,
            out var _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.FromBase85String(ReadOnlySpan{byte})" /> decodes UTF-8 input.
    /// </summary>
    [TestMethod]
    public void FromBase85String_ForUtf8Source_ShouldDecode()
    {
        var original = Ascii("Hello world!");
        var encoded = Base85.Encode(original);
        var utf8 = System.Text.Encoding.ASCII.GetBytes(encoded);

        var actual = Base85.FromBase85String(utf8);

        CollectionAssert.AreEqual(original, actual);
    }
    /// <summary>
    /// Verifies that <see cref="Base85.ToBase85String(byte[])" /> returns the Ascii85 output.
    /// </summary>
    [TestMethod]
    public void ToBase85String_ForByteArray_ShouldReturnAscii85Output()
    {
        var actual = Base85.ToBase85String(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

        Assert.IsFalse(string.IsNullOrEmpty(actual));
        var roundTrip = Base85.FromBase85String(actual);
        CollectionAssert.AreEqual(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryToBase85String(ReadOnlySpan{byte}, Span{char}, out int)" /> writes Ascii85
    /// output into a char destination.
    /// </summary>
    [TestMethod]
    public void TryToBase85String_ForCharSpan_ShouldWriteExpectedOutput()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var destination = new char[Base85.GetMaxEncodedLength(bytes.Length)];

        var ok = Base85.TryToBase85String(bytes.AsSpan(), destination, out var charsWritten);

        Assert.IsTrue(ok);
        string encoded = new(destination, 0, charsWritten);
        CollectionAssert.AreEqual(bytes, Base85.FromBase85String(encoded));
    }

}
