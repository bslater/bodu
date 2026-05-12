// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotAsciiAlphanumericUppercase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase" /> accepts every ASCII decimal
    /// digit and uppercase ASCII letter without throwing.
    /// </summary>
    [TestMethod]
    [DataRow('0')]
    [DataRow('5')]
    [DataRow('9')]
    [DataRow('A')]
    [DataRow('M')]
    [DataRow('Z')]
    public void ThrowIfNotAsciiAlphanumericUppercase_WhenCharIsAccepted_ShouldNotThrow(char value)
    {
        ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(value);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for lowercase letters and other non-accepted characters.
    /// </summary>
    [TestMethod]
    [DataRow('a')]
    [DataRow('z')]
    [DataRow('/')]
    [DataRow(':')]
    [DataRow('@')]
    [DataRow('[')]
    [DataRow(' ')]
    [DataRow('Æ')] // non-ASCII uppercase letter
    public void ThrowIfNotAsciiAlphanumericUppercase_WhenCharIsNotAccepted_ShouldThrowExactly(char value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(value);
        });
    }
}
