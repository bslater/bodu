// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToSafeFileName.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafeFileName(string)" /> replaces every character reported
    /// by <see cref="Path.GetInvalidFileNameChars" /> with an underscore and leaves valid characters intact.
    /// </summary>
    [TestMethod]
    public void ToSafeFileName_WhenInputContainsInvalidChars_ShouldReplaceWithUnderscore()
    {
        var invalid = Path.GetInvalidFileNameChars()[0];
        var input = $"valid{invalid}name.txt";

        var actual = input.ToSafeFileName();

        Assert.AreEqual($"valid_name.txt", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafeFileName(string)" /> returns the input unchanged when
    /// it contains no invalid characters.
    /// </summary>
    [TestMethod]
    public void ToSafeFileName_WhenInputContainsNoInvalidChars_ShouldReturnInputUnchanged()
    {
        const string input = "report-2024.txt";

        var actual = input.ToSafeFileName();

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafeFileName(string)" /> returns <c>"_"</c> for an empty
    /// input so that the result is never itself an empty file name.
    /// </summary>
    [TestMethod]
    public void ToSafeFileName_WhenInputIsEmpty_ShouldReturnSingleUnderscore()
    {
        Assert.AreEqual("_", string.Empty.ToSafeFileName());
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafeFileName(string)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToSafeFileName_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.ToSafeFileName(null!);
        });
    }
}
