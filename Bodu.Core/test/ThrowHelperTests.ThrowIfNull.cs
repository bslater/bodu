// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNull.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNull{T}(T)" /> does not throw for non-null values across reference and value types.
    /// </summary>
    [TestMethod]
    [DataRow("test")]
    [DataRow(123)]
    [DataRow(typeof(string))]
    public void ThrowIfNull_WhenValueIsNotNull_ShouldNotThrow(object value) => ThrowHelper.ThrowIfNull(value);
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNull{T}(T)" /> throws <see cref="ArgumentNullException" /> when the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    [DataRow(null)]
    public void ThrowIfNull_WhenValueIsNull_ShouldThrowExactly(object? value)
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNull(value);
        });
    }

    /// <summary>
    /// Verifies that the message-taking overload does not throw for non-null values regardless of the supplied message.
    /// </summary>
    [TestMethod]
    [DataRow("hello", "Custom message")]
    [DataRow(99, "Another message")]
    public void ThrowIfNull_WithMessage_WhenValueIsNotNull_ShouldNotThrow(object value, string message) => ThrowHelper.ThrowIfNull(value, message);

    /// <summary>
    /// Verifies that the message-taking overload throws <see cref="ArgumentNullException" /> whose message contains the supplied custom text when the value is <see langword="null" />.
    /// </summary>
    [TestMethod]
    [DataRow(null, "Custom message")]
    public void ThrowIfNull_WithMessage_WhenValueIsNull_ShouldThrowExactly(object? value, string message)
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNull(value, message);
        });

        StringAssert.Contains(ex.Message, message);
    }

}
