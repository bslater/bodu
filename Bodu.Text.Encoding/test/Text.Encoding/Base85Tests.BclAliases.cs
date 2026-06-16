// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.BclAliases.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        byte[] original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        string encoded = Base85.Encode(original);
        byte[] destination = new byte[4];

        OperationStatus status = Base85.FromBase85String(
            encoded.AsSpan(),
            destination,
            out int charsConsumed,
            out int bytesWritten);

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
        byte[] destination = new byte[10];

        OperationStatus status = Base85.FromBase85String(
            "\x01\x02\x03\x04\x05".AsSpan(),
            destination,
            out int _,
            out int _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.FromBase85String(ReadOnlySpan{byte})" /> decodes UTF-8 input.
    /// </summary>
    [TestMethod]
    public void FromBase85String_ForUtf8Source_ShouldDecode()
    {
        byte[] original = Ascii("Hello world!");
        string encoded = Base85.Encode(original);
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(encoded);

        byte[] actual = Base85.FromBase85String(utf8);

        CollectionAssert.AreEqual(original, actual);
    }
    /// <summary>
    /// Verifies that <see cref="Base85.ToBase85String(byte[])" /> returns the Ascii85 output.
    /// </summary>
    [TestMethod]
    public void ToBase85String_ForByteArray_ShouldReturnAscii85Output()
    {
        string actual = Base85.ToBase85String([0xDE, 0xAD, 0xBE, 0xEF]);

        Assert.IsFalse(string.IsNullOrEmpty(actual));
        byte[] roundTrip = Base85.FromBase85String(actual);
        CollectionAssert.AreEqual(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, roundTrip);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryToBase85String(ReadOnlySpan{byte}, Span{char}, out int)" /> writes Ascii85
    /// output into a char destination.
    /// </summary>
    [TestMethod]
    public void TryToBase85String_ForCharSpan_ShouldWriteExpectedOutput()
    {
        byte[] bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        char[] destination = new char[Base85.GetMaxEncodedLength(bytes.Length)];

        bool ok = Base85.TryToBase85String(bytes.AsSpan(), destination, out int charsWritten);

        Assert.IsTrue(ok);
        string encoded = new(destination, 0, charsWritten);
        CollectionAssert.AreEqual(bytes, Base85.FromBase85String(encoded));
    }

}
