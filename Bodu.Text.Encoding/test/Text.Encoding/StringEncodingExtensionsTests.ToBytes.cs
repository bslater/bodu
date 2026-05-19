// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.ToBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytes(string, System.Text.Encoding)" /> matches the
    /// BCL <see cref="System.Text.Encoding.GetBytes(string)" /> for every canonical encoding.
    /// </summary>
    /// <param name="encoding">The encoding under test.</param>
    [DataTestMethod]
    [DynamicData(nameof(CanonicalEncodings), DynamicDataSourceType.Method)]
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

        Assert.AreEqual(0, actual.Length);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.ToBytes(string, System.Text.Encoding)" /> throws
    /// <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToBytes_WhenTextIsNull_ShouldThrowArgumentNullException()
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
    public void ToBytes_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.ToBytes(null!);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
