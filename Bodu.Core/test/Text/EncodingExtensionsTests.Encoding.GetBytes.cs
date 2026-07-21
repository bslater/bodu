// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.GetBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
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
    public void GetBytesExactly_WhenDestinationIsLargerThanRequired_ShouldThrowExactly()
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
    public void GetBytesExactly_WhenDestinationIsSmallerThanRequired_ShouldThrowExactly()
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
    public void GetBytesExactly_WhenEncodingIsNull_ShouldThrowExactly()
    {
        byte[] backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetBytesExactly(null!, SampleText, backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
