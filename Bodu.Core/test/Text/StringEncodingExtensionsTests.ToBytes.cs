// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.ToBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytes(string, System.Text.Encoding)" /> matches the
    /// BCL <see cref="System.Text.Encoding.GetBytes(string)" /> for every canonical encoding.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [TestMethod]
    [DynamicData(nameof(CanonicalEncodings))]
    public void ToBytes_WhenInvoked_ShouldMatchBclEncoding(System.Text.Encoding encoding)
    {
        byte[] expected = encoding.GetBytes(MultiByteText);

        byte[] actual = MultiByteText.ToBytes(encoding);

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytes(string, System.Text.Encoding)" /> returns an
    /// empty array for an empty string.
    /// </summary>
    [TestMethod]
    public void ToBytes_WhenStringIsEmpty_ShouldReturnEmptyArray()
    {
        byte[] actual = string.Empty.ToBytes(System.Text.Encoding.UTF8);

        Assert.IsEmpty(actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytes(string, System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToBytes_WhenTextIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.ToBytes(null!, System.Text.Encoding.UTF8);
        });

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytes(string, System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToBytes_WhenEncodingIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.ToBytes(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
