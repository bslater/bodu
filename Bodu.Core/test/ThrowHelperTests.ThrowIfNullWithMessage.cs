// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNullWithMessage.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNull{T}(T, string, string)" /> throws
    /// <see cref="ArgumentNullException" /> with the supplied message and parameter name when the value is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNullWithMessage_WhenValueIsNull_ShouldThrowExactlyWithMessage()
    {
        object value = null!;

        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfNull(value, "value must not be null");
        });

        Assert.AreEqual("value", ex.ParamName);
        StringAssert.Contains(ex.Message, "value must not be null");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNull{T}(T, string, string)" /> does not throw when the supplied
    /// value is non-<see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNullWithMessage_WhenValueIsNotNull_ShouldNotThrow()
    {
        object value = new();
        ThrowHelper.ThrowIfNull(value, "value must not be null");
    }
}
