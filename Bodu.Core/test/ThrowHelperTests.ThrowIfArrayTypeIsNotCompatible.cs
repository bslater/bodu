// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayTypeIsNotCompatible.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        [DynamicData(nameof(GetIncompatibleArrayTypeTestData), DynamicDataSourceType.Method)]
        public void ThrowIfArrayTypeIsNotCompatible_WhenArrayTypeIsIncorrect_ShouldThrowArgumentException(Array array)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array);
            });
        }

        [TestMethod]
        [DynamicData(nameof(GetCompatibleArrayTypeTestData), DynamicDataSourceType.Method)]
        public void ThrowIfArrayTypeIsNotCompatible_WhenArrayTypeIsCorrect_ShouldNotThrow(Array array)
        {
            ThrowHelper.ThrowIfArrayTypeIsNotCompatible<int>(array);
        }

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
}