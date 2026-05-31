// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensionsTests.Encoding.GetChars.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class EncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryGetChars(System.Text.Encoding, ReadOnlySpan{byte}, Span{char}, out int)" />
    /// returns <see langword="true" /> and reports the character count when the destination fits.
    /// </summary>
    [TestMethod]
    public void TryGetChars_WhenDestinationFits_ShouldReturnTrueAndReportCount()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required];

        var ok = System.Text.Encoding.UTF8.TryGetChars(bytes, destination, out var written);

        Assert.IsTrue(ok);
        Assert.AreEqual(required, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryGetChars(System.Text.Encoding, ReadOnlySpan{byte}, Span{char}, out int)" />
    /// returns <see langword="false" /> when the destination is one character too small.
    /// </summary>
    [TestMethod]
    public void TryGetChars_WhenDestinationIsOneCharTooSmall_ShouldReturnFalse()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        var backing = new char[required - 1];

        var ok = System.Text.Encoding.UTF8.TryGetChars(bytes, backing, out var written);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.TryGetChars(System.Text.Encoding, ReadOnlySpan{byte}, Span{char}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryGetChars_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new char[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.TryGetChars(null!, [0x68], backing, out _);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="EncodingExtensions.GetCharsExactly(System.Text.Encoding, ReadOnlySpan{byte}, Span{char})" />
    /// writes the expected characters when the destination is exactly the right size.
    /// </summary>
    [TestMethod]
    public void GetCharsExactly_WhenDestinationIsExactlySized_ShouldWriteAndReturnCount()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(MultiByteText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        Span<char> destination = new char[required];

        var written = System.Text.Encoding.UTF8.GetCharsExactly(bytes, destination);

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
        var bytes = System.Text.Encoding.UTF8.GetBytes(SampleText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        var backing = new char[required + 1];

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
        var bytes = System.Text.Encoding.UTF8.GetBytes(SampleText);
        var required = System.Text.Encoding.UTF8.GetCharCount(bytes);
        var backing = new char[required - 1];

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
        var backing = new char[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = EncodingExtensions.GetCharsExactly(null!, [0x68], backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
