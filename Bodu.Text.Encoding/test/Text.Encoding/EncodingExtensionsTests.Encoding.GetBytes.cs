// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.GetBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryGetBytes(System.Text.Encoding, ReadOnlySpan{char}, Span{byte}, out int)" />
    /// returns <see langword="true" /> and reports the byte count when the destination fits.
    /// </summary>
    [TestMethod]
    public void TryGetBytes_WhenDestinationFits_ShouldReturnTrueAndReportCount()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        var ok = System.Text.Encoding.UTF8.TryGetBytes(MultiByteText, destination, out var written);

        Assert.IsTrue(ok);
        Assert.AreEqual(required, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryGetBytes(System.Text.Encoding, ReadOnlySpan{char}, Span{byte}, out int)" />
    /// returns <see langword="false" /> when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void TryGetBytes_WhenDestinationIsOneByteTooSmall_ShouldReturnFalse()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        var backing = new byte[required - 1];

        var ok = System.Text.Encoding.UTF8.TryGetBytes(MultiByteText, backing, out var written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryGetBytes(System.Text.Encoding, ReadOnlySpan{char}, Span{byte}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetBytes_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.TryGetBytes(null!, SampleText, backing, out _);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesExactly(System.Text.Encoding, ReadOnlySpan{char}, Span{byte})" />
    /// writes the expected bytes when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void GetBytesExactly_WhenDestinationIsExactlySized_ShouldWriteAndReturnCount()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        var written = System.Text.Encoding.UTF8.GetBytesExactly(MultiByteText, destination);

        Assert.AreEqual(required, written);
        CollectionAssert.AreEqual(System.Text.Encoding.UTF8.GetBytes(MultiByteText), destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesExactly(System.Text.Encoding, ReadOnlySpan{char}, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is larger than the required size.
    /// </summary>
    [TestMethod]
    public void GetBytesExactly_WhenDestinationIsLargerThanRequired_ShouldThrowExactly()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(SampleText);
        var backing = new byte[required + 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = System.Text.Encoding.UTF8.GetBytesExactly(SampleText, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesExactly(System.Text.Encoding, ReadOnlySpan{char}, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is smaller than the required size.
    /// </summary>
    [TestMethod]
    public void GetBytesExactly_WhenDestinationIsSmallerThanRequired_ShouldThrowExactly()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(SampleText);
        var backing = new byte[required - 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = System.Text.Encoding.UTF8.GetBytesExactly(SampleText, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesExactly(System.Text.Encoding, ReadOnlySpan{char}, Span{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetBytesExactly_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetBytesExactly(null!, SampleText, backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
