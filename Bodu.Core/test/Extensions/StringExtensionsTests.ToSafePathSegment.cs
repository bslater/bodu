// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.ToSafePathSegment.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafePathSegment(string)" /> replaces the primary directory
    /// separator with an underscore so the result cannot smuggle embedded path traversal.
    /// </summary>
    [TestMethod]
    public void ToSafePathSegment_WhenInputContainsPrimaryDirectorySeparator_ShouldReplaceWithUnderscore()
    {
        string input = $"foo{Path.DirectorySeparatorChar}bar";

        string actual = input.ToSafePathSegment();

        Assert.AreEqual("foo_bar", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafePathSegment(string)" /> replaces the alternate
    /// directory separator with an underscore.
    /// </summary>
    [TestMethod]
    public void ToSafePathSegment_WhenInputContainsAltDirectorySeparator_ShouldReplaceWithUnderscore()
    {
        string input = $"foo{Path.AltDirectorySeparatorChar}bar";

        string actual = input.ToSafePathSegment();

        Assert.AreEqual("foo_bar", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafePathSegment(string)" /> returns the input unchanged
    /// when it contains no invalid characters and no path separators.
    /// </summary>
    [TestMethod]
    public void ToSafePathSegment_WhenInputContainsNoInvalidChars_ShouldReturnInputUnchanged()
    {
        const string input = "report-2024.txt";

        string actual = input.ToSafePathSegment();

        Assert.AreEqual(input, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafePathSegment(string)" /> returns <c>"_"</c> for an
    /// empty input.
    /// </summary>
    [TestMethod]
    public void ToSafePathSegment_WhenInputIsEmpty_ShouldReturnSingleUnderscore() => Assert.AreEqual("_", string.Empty.ToSafePathSegment());

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ToSafePathSegment(string)" /> throws
    /// <see cref="ArgumentNullException" /> when invoked with <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ToSafePathSegment_WhenValueIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringExtensions.ToSafePathSegment(null!);
        });
    }
}
