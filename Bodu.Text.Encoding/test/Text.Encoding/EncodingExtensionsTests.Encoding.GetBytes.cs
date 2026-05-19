// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.GetBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        bool ok = System.Text.Encoding.UTF8.TryGetBytes(MultiByteText, destination, out int written);

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
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        byte[] backing = new byte[required - 1];

        bool ok = System.Text.Encoding.UTF8.TryGetBytes(MultiByteText, backing, out int written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryGetBytes(System.Text.Encoding, ReadOnlySpan{char}, Span{byte}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetBytes_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        byte[] backing = new byte[64];

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
        int required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        int written = System.Text.Encoding.UTF8.GetBytesExactly(MultiByteText, destination);

        Assert.AreEqual(required, written);
        CollectionAssert.AreEqual(System.Text.Encoding.UTF8.GetBytes(MultiByteText), destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetBytesExactly(System.Text.Encoding, ReadOnlySpan{char}, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is larger than the required size.
    /// </summary>
    [TestMethod]
    public void GetBytesExactly_WhenDestinationIsLargerThanRequired_ShouldThrowArgumentException()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(SampleText);
        byte[] backing = new byte[required + 1];

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
    public void GetBytesExactly_WhenDestinationIsSmallerThanRequired_ShouldThrowArgumentException()
    {
        int required = System.Text.Encoding.UTF8.GetByteCount(SampleText);
        byte[] backing = new byte[required - 1];

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
    public void GetBytesExactly_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        byte[] backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetBytesExactly(null!, SampleText, backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
