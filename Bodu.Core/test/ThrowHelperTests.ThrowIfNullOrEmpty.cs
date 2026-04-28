// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullOrEmpty.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> throws <see cref="ArgumentNullException" />
    /// when the value is <see langword="null" />.
    /// </summary>
    [DataRow(null)]
    [TestMethod]
    public void ThrowIfNullOrEmpty_WhenValueIsNull_ShouldThrowArgumentNullException(string? value)
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNullOrEmpty(value!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> throws <see cref="ArgumentException" />
    /// when the value is an empty string.
    /// </summary>
    [DataRow("")]
    [TestMethod]
    public void ThrowIfNullOrEmpty_WhenValueIsEmpty_ShouldThrowArgumentException(string value)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNullOrEmpty(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrEmpty" /> does not throw for any non-empty string,
    /// including whitespace-only values. Callers that must also reject whitespace should use
    /// <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> instead.
    /// </summary>
    [DataRow("a")]
    [DataRow("test")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [TestMethod]
    public void ThrowIfNullOrEmpty_WhenValueIsNonEmpty_ShouldNotThrow(string value)
    {
        ThrowHelper.ThrowIfNullOrEmpty(value);
    }
}
