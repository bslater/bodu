// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayMultidimensional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayIsNull, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        Array? array = null;
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayHasRankTwo, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayHasRankTwo_ShouldThrowArgumentException()
    {
        Array array = new int[2, 3];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayHasRankThree, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayHasRankThree_ShouldThrowArgumentException()
    {
        Array array = new int[2, 2, 2];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayIsSingleDimensional, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayIsSingleDimensional_ShouldNotThrow()
    {
        Array array = new int[5];
        ThrowHelper.ThrowIfArrayMultidimensional(array);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayMultidimensional" />, when ArrayIsEmptySingleDimensional, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayMultidimensional_WhenArrayIsEmptySingleDimensional_ShouldNotThrow()
    {
        Array array = Array.Empty<int>();
        ThrowHelper.ThrowIfArrayMultidimensional(array);
    }
}
