// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.Utf8.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("9Ajdvzr");
        byte[] destination = new byte[1];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

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
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("9Ajdvzr");
        byte[] destination = new byte[1];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out int _, out int _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.DecodeFromUtf8" /> returns <see cref="OperationStatus.InvalidData" /> for
    /// invalid input.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenInvalidInput_ShouldReturnInvalidData()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("9A0dvzr");
        byte[] destination = new byte[10];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out int _, out int _);

        Assert.AreEqual(OperationStatus.InvalidData, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base58.DecodeFromUtf8" /> recovers bytes and reports counts.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenValidInput_ShouldReturnDoneWithCounts()
    {
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("9Ajdvzr");
        byte[] destination = new byte[5];

        OperationStatus status = Base58.DecodeFromUtf8(utf8, destination, out int bytesConsumed, out int bytesWritten);

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
        byte[] actual = Base58.EncodeToUtf8(Ascii("Hello"));

        Assert.AreEqual("9Ajdvzr", System.Text.Encoding.ASCII.GetString(actual));
    }

    /// <summary>
    /// Verifies that round-trip via the UTF-8 path recovers the original bytes.
    /// </summary>
    [TestMethod]
    public void RoundTrip_EncodeAndDecodeUtf8_ShouldRecoverOriginal()
    {
        byte[] original = Ascii("Hello");

        byte[] encoded = Base58.EncodeToUtf8(original);
        byte[] destination = new byte[Base58.GetMaxDecodedLength(encoded.Length)];

        OperationStatus status = Base58.DecodeFromUtf8(encoded, destination, out _, out int bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        CollectionAssert.AreEqual(original, destination.AsSpan(0, bytesWritten).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="Base58.TryEncodeToUtf8" /> writes the expected bytes into the destination.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationLargeEnough_ShouldReturnTrueAndExpectedBytes()
    {
        byte[] destination = new byte[Base58.GetMaxEncodedLength(5)];

        bool ok = Base58.TryEncodeToUtf8(Ascii("Hello").AsSpan(), destination, out int bytesWritten);

        Assert.IsTrue(ok);
        Assert.AreEqual("9Ajdvzr", System.Text.Encoding.ASCII.GetString(destination, 0, bytesWritten));
    }

}
