// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.GetChars.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsExactly(System.Text.Encoding, ReadOnlySpan{byte}, Span{char})" />
    /// writes the expected characters when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void GetCharsExactly_WhenDestinationIsExactlySized_ShouldWriteAndReturnCount()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        int required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required];

        int written = System.Text.Encoding.UTF8.GetCharsExactly(bytes, destination);

        Assert.AreEqual(required, written);
        Assert.AreEqual(MultiByteText, new string(destination));
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsExactly(System.Text.Encoding, ReadOnlySpan{byte}, Span{char})" />
    /// throws <see cref="ArgumentException" /> when the destination is larger than the required size.
    /// </summary>
    [TestMethod]
    public void GetCharsExactly_WhenDestinationIsLargerThanRequired_ShouldThrowExactly()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(SampleText);
        int required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        char[] backing = new char[required + 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = System.Text.Encoding.UTF8.GetCharsExactly(bytes, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsExactly(System.Text.Encoding, ReadOnlySpan{byte}, Span{char})" />
    /// throws <see cref="ArgumentException" /> when the destination is smaller than the required size.
    /// </summary>
    [TestMethod]
    public void GetCharsExactly_WhenDestinationIsSmallerThanRequired_ShouldThrowExactly()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(SampleText);
        int required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        char[] backing = new char[required - 1];

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = System.Text.Encoding.UTF8.GetCharsExactly(bytes, backing);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsExactly(System.Text.Encoding, ReadOnlySpan{byte}, Span{char})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetCharsExactly_WhenEncodingIsNull_ShouldThrowExactly()
    {
        char[] backing = new char[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetCharsExactly(null!, [0x68], backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
