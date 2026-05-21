// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Utf8.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.Text.Encoding;

public sealed partial class Base85Tests
{

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeFromUtf8" /> reports <c>bytesConsumed = 0</c> and <c>bytesWritten = 0</c>
    /// on <see cref="OperationStatus.DestinationTooSmall" />. Base85 cannot commit partial output, so the contract is
    /// "all-or-nothing" — the caller retries with a larger destination.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReportNoCommittedProgress()
    {
        var original = Ascii("Hello world!");
        var encoded = Base85.EncodeToUtf8(original);
        var destination = new byte[1];

        OperationStatus status = Base85.DecodeFromUtf8(encoded, destination, out var bytesConsumed, out var bytesWritten);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
        Assert.AreEqual(0, bytesConsumed);
        Assert.AreEqual(0, bytesWritten);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeFromUtf8" /> returns <see cref="OperationStatus.DestinationTooSmall" />
    /// when the destination cannot fit the result.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenDestinationTooSmall_ShouldReturnDestinationTooSmall()
    {
        var original = Ascii("Hello world!");
        var encoded = Base85.EncodeToUtf8(original);
        var destination = new byte[1];

        OperationStatus status = Base85.DecodeFromUtf8(encoded, destination, out var _, out var _);

        Assert.AreEqual(OperationStatus.DestinationTooSmall, status);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.DecodeFromUtf8" /> recovers bytes and reports counts.
    /// </summary>
    [TestMethod]
    public void DecodeFromUtf8_WhenValidInput_ShouldReturnDoneWithCounts()
    {
        var original = Ascii("Hello world!");
        var encoded = Base85.EncodeToUtf8(original);
        var destination = new byte[original.Length];

        OperationStatus status = Base85.DecodeFromUtf8(encoded, destination, out var bytesConsumed, out var bytesWritten);

        Assert.AreEqual(OperationStatus.Done, status);
        Assert.AreEqual(encoded.Length, bytesConsumed);
        Assert.AreEqual(original.Length, bytesWritten);
        CollectionAssert.AreEqual(original, destination);
    }
    /// <summary>
    /// Verifies that <see cref="Base85.EncodeToUtf8" /> returns the Ascii85 output as ASCII bytes.
    /// </summary>
    [TestMethod]
    public void EncodeToUtf8_ShouldReturnAsciiBytesOfAscii85Output()
    {
        var original = Ascii("Hello world!");

        var encoded = Base85.EncodeToUtf8(original);
        var asString = System.Text.Encoding.ASCII.GetString(encoded);

        Assert.AreEqual(Base85.Encode(original), asString);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryEncodeToUtf8" /> writes the expected bytes.
    /// </summary>
    [TestMethod]
    public void TryEncodeToUtf8_WhenDestinationLargeEnough_ShouldReturnTrueAndExpectedBytes()
    {
        var original = Ascii("Hello world!");
        var destination = new byte[Base85.GetMaxEncodedLength(original.Length)];

        var ok = Base85.TryEncodeToUtf8(original.AsSpan(), destination, out var bytesWritten);

        Assert.IsTrue(ok);
        var asString = System.Text.Encoding.ASCII.GetString(destination, 0, bytesWritten);
        Assert.AreEqual(Base85.Encode(original), asString);
    }

}
