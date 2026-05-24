// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Utf8.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base58Tests
{

    /// <summary>
    /// Verifies that <see cref="Base58.DecodeFromUtf8" /> reports <c>bytesConsumed = 0</c> and <c>bytesWritten = 0</c>
    /// on <see cref="OperationStatus.DestinationTooSmall" />. Base58 cannot commit partial output (it decodes through
    /// big-integer divmod into a scratch buffer), so the contract is "all-or-nothing" — the caller retries with a
    /// larger destination.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReportNoCommittedProgress()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("9Ajdvzr");
        var destination = new byte[1];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out var bytesConsumed, out var bytesWritten);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />
    /// when the destination cannot fit the result.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReturnDestinationTooSmall()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("9Ajdvzr");
        var destination = new byte[1];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out var _, out var _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> for
    /// invalid input.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenInvalidInput_ShouldReturnInvalidData()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("9A0dvzr");
        var destination = new byte[10];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out var _, out var _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.DecodeFromUtf8" /> recovers bytes and reports counts.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenValidInput_ShouldReturnDoneWithCounts()
    {
        var utf8 = System.Text.Encoding.ASCII.GetBytes("9Ajdvzr");
        var destination = new byte[5];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out var bytesConsumed, out var bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(7, bytesConsumed);
        Assert.AreEqual(5, bytesWritten);
        CollectionAssert.AreEqual(Ascii("Hello"), destination);
    }
    /// <summary>
    /// Verifies that <see cref="Base58.EncodeToUtf8" /> returns the Bitcoin/Flickr output as ASCII bytes.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ShouldReturnAsciiBytesOfBitcoinFlickrOutput()
    {
        var actual = Base58.EncodeToUtf8(Ascii("Hello"));

        Assert.AreEqual("9Ajdvzr", System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that round-trip via the UTF-8 path recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_EncodeAndDecodeUtf8_ShouldRecoverOriginal()
    {
        var original = Ascii("Hello");

        var encoded = Base58.EncodeToUtf8(original);
        var destination = new byte[Base58.GetMaxDecodedLength(encoded.Length)];

        OperationStatus status = Base58.DecodeFromUtf8(encoded, destination, out _, out var bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        CollectionAssert.AreEqual(original, destination.AsSpan(0, bytesWritten).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryEncodeToUtf8" /> writes the expected bytes into the destination.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationLargeEnough_ShouldReturnTrueAndExpectedBytes()
    {
        var destination = new byte[Base58.GetMaxEncodedLength(5)];

        var ok = Base58.TryEncodeToUtf8(Ascii("Hello").AsSpan(), destination, out var bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual("9Ajdvzr", System.Text.Encoding.ASCII.GetString(destination, 0, bytesWritten));
    }

}
