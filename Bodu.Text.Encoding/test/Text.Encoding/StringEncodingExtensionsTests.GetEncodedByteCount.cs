// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.GetEncodedByteCount.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetEncodedByteCount(string, System.Text.Encoding)" />
    /// matches the BCL <see cref="System.Text.Encoding.GetByteCount(string)" /> for every canonical encoding.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [TestMethod]
    [DynamicData(nameof(CanonicalEncodings))]
    public void GetEncodedByteCount_WhenInvoked_ShouldMatchBclEncoding(System.Text.Encoding encoding)
    {
        var expected = encoding.GetByteCount(MultiByteText);

        var actual = MultiByteText.GetEncodedByteCount(encoding);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetEncodedByteCount(string, System.Text.Encoding)" />
    /// returns zero for an empty string.
    /// </summary>
    [TestMethod]
    public void GetEncodedByteCount_WhenStringIsEmpty_ShouldReturnZero() => Assert.AreEqual(0, string.Empty.GetEncodedByteCount(System.Text.Encoding.UTF8));

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetEncodedByteCount(string, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetEncodedByteCount_WhenTextIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.GetEncodedByteCount(null!, System.Text.Encoding.UTF8);
        });

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.GetEncodedByteCount(string, System.Text.Encoding)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetEncodedByteCount_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.GetEncodedByteCount(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
