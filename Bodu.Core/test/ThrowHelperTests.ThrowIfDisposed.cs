// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfDisposed.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDisposed" /> does not throw when the disposed flag is
    /// <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfDisposed_WhenDisposedIsFalse_ShouldNotThrow() => ThrowHelper.ThrowIfDisposed(false);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDisposed" /> does not throw when the disposed flag is
    /// <see langword="false" />, even when an object name is supplied.
    /// </summary>
    [TestMethod]
    public void ThrowIfDisposed_WhenDisposedIsFalseWithObjectName_ShouldNotThrow() => ThrowHelper.ThrowIfDisposed(false, "MyObject");
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDisposed" /> throws <see cref="ObjectDisposedException" />
    /// when the disposed flag is <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfDisposed_WhenDisposedIsTrue_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            ThrowHelper.ThrowIfDisposed(true);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfDisposed" /> includes the supplied object name in the
    /// exception when the disposed flag is <see langword="true" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfDisposed_WhenDisposedIsTrueWithObjectName_ShouldIncludeNameInException()
    {
        ObjectDisposedException ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            ThrowHelper.ThrowIfDisposed(true, "MyObject");
        });

        Assert.IsTrue(ex.Message.Contains("MyObject", StringComparison.Ordinal));
    }

}
