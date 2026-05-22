// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullWithMessage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that the three-argument <see cref="ThrowHelper.ThrowIfNull{T}(T, string, string)" /> overload
    /// returns without throwing when the supplied value is non-<see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNull_WithCustomMessage_WhenValueIsNotNull_ShouldNotThrow()
    {
        object value = new();
        ThrowHelper.ThrowIfNull<object>(value, "unused message", "explicitParam");
    }

    /// <summary>
    /// Verifies that the three-argument <see cref="ThrowHelper.ThrowIfNull{T}(T, string, string)" /> overload
    /// surfaces both the supplied message and the explicit parameter name when the value is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNull_WithCustomMessage_WhenValueIsNull_ShouldThrowExactly()
    {
        object value = null!;
        const string Message = "custom message text";
        const string Param = "explicitParam";

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNull<object>(value, Message, Param);
        });

        Assert.AreEqual(Param, ex.ParamName);
        Assert.IsTrue(ex.Message.Contains(Message, StringComparison.Ordinal),
            $"Exception message did not contain the supplied custom text. Message: {ex.Message}");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNull{T}(T, string, string)" /> does not throw when the supplied
    /// value is non-<see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNullWithMessage_WhenValueIsNotNull_ShouldNotThrow()
    {
        object value = new();
        ThrowHelper.ThrowIfNull(value);
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNull{T}(T, string, string)" /> throws
    /// <see cref="ArgumentNullException" /> with the supplied message and parameter name when the value is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNullWithMessage_WhenValueIsNull_ShouldThrowExactly()
    {
        object value = null!;
        var paramName = "myParam";
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNull(value, paramName);
        });

        Assert.AreEqual(paramName, ex.ParamName);
    }

}
