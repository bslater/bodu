// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayTypeIsNotCompatible.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayTypeIsNotCompatible" />, when ArrayTypeIsIncorrect, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetIncompatibleArrayTypeTestData))]
    public void ThrowIfArrayTypeIsNotCompatible_WhenArrayTypeIsIncorrect_ShouldThrowArgumentException(Array array)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayTypeIsNotCompatible" />, when ArrayTypeIsCorrect, NotThrow.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetCompatibleArrayTypeTestData))]
    public void ThrowIfArrayTypeIsNotCompatible_WhenArrayTypeIsCorrect_ShouldNotThrow(Array array) => ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array);

    private static IEnumerable<object[]> GetIncompatibleArrayTypeTestData()
    {
        yield return new object[] { new string[5] };
        yield return new object[] { new double[3] };
        yield return new object[] { Array.CreateInstance(typeof(object), 2) };
    }

    private static IEnumerable<object[]> GetCompatibleArrayTypeTestData()
    {
        yield return new object[] { new int[0] };
        yield return new object[] { new int[10] };
        yield return new object[] { Array.CreateInstance(typeof(int), 5) };
    }
}
