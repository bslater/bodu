// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IComparableExtensionsTests.IsEqualTo.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class IComparableExtensionsTests
{

    /// <summary>
    /// Verifies that a null comparer throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [TestMethod]
    public void IsEqualTo_WhenComparerIsNull_ShouldThrowExactly()
    {
        IComparer<int>? comparer = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = 5.IsEqualTo(5, comparer!);
        });

        Assert.AreEqual("comparer", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <c>IsEqualTo</c> treats values as equal when a case-insensitive <see cref="StringComparer"/>
    /// reports zero, even though <see cref="string.Equals(string)"/> would not.
    /// </summary>
    [TestMethod]
    public void IsEqualTo_WhenUsingCaseInsensitiveComparer_ShouldTreatDifferentCasingAsEqual()
    {
        IComparer<string> comparer = StringComparer.OrdinalIgnoreCase;

        Assert.IsTrue("Hello".IsEqualTo("hello", comparer));
        Assert.IsFalse("Hello".IsEqualTo("world", comparer));
    }
    // =========================================================================
    // IsEqualTo<T>(T, T, IComparer<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that <c>IsEqualTo</c> returns <see langword="true"/> when the default comparer reports equality
    /// and <see langword="false"/> otherwise.
    /// </summary>
    [TestMethod]
    public void IsEqualTo_WhenUsingDefaultComparer_ShouldReflectNaturalEquality()
    {
        IComparer<int> comparer = Comparer<int>.Default;

        Assert.IsTrue(5.IsEqualTo(5, comparer));
        Assert.IsFalse(5.IsEqualTo(6, comparer));
    }

}
