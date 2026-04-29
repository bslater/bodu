// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullOrWhiteSpace.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> throws <see cref="ArgumentNullException" />
    /// when the value is <see langword="null" />.
    /// </summary>
    [DataRow(null)]
    [TestMethod]
    public void ThrowIfNullOrWhiteSpace_WhenValueIsNull_ShouldThrowArgumentNullException(string? value)
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNullOrWhiteSpace(value!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> throws <see cref="ArgumentException" />
    /// when the value is empty or contains only whitespace characters.
    /// </summary>
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\n")]
    [TestMethod]
    public void ThrowIfNullOrWhiteSpace_WhenValueIsEmptyOrWhitespace_ShouldThrowArgumentException(string value)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNullOrWhiteSpace(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNullOrWhiteSpace" /> does not throw when the value contains
    /// at least one non-whitespace character.
    /// </summary>
    [DataRow("Valid")]
    [DataRow("x")]
    [DataRow("  trimmed")]
    [DataRow("middle space")]
    [TestMethod]
    public void ThrowIfNullOrWhiteSpace_WhenValueIsValid_ShouldNotThrow(string value)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(value);
    }
}
