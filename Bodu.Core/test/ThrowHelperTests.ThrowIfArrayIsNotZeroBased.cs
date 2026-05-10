// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayIsNotZeroBased.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayIsNotZeroBased" />, when ArrayIsNotZeroBased, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetNonZeroBasedArrayTestData))]
    public void ThrowIfArrayIsNotZeroBased_WhenArrayIsNotZeroBased_ShouldThrowExactly(Array array)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayIsNotZeroBased" />, when ArrayIsZeroBased, NotThrow.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetZeroBasedArrayTestData))]
    public void ThrowIfArrayIsNotZeroBased_WhenArrayIsZeroBased_ShouldNotThrow(Array array) => ThrowHelper.ThrowIfArrayIsNotZeroBased(array);

    private static IEnumerable<object[]> GetNonZeroBasedArrayTestData()
    {
        yield return new object[] { Array.CreateInstance(typeof(int), [5], [1]) };
        yield return new object[] { Array.CreateInstance(typeof(string), [3], [-10]) };
    }

    private static IEnumerable<object[]> GetZeroBasedArrayTestData()
    {
        yield return new object[] { Array.Empty<int>() };
        yield return new object[] { new string[5] };
        yield return new object[] { Array.CreateInstance(typeof(double), [4]) }; // Zero-based by default
    }
}
