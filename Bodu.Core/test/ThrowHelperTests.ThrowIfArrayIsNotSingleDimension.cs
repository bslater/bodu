// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayIsNotSingleDimension.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Array Is Not Single Dimension, when Array Is Multi Dimensional, throws Exactly.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetMultiDimensionalArrayTestData), DynamicDataSourceType.Method)]
        public void ThrowIfArrayIsNotSingleDimension_WhenArrayIsMultiDimensional_ShouldThrowExactly(Array array)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfArrayMultidimensional(array);
            });
        }

        /// <summary>
        /// Verifies that Throw If Array Is Not Single Dimension, when Array Is Single Dimension, does not Throw.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetSingleDimensionalArrayTestData), DynamicDataSourceType.Method)]
        public void ThrowIfArrayIsNotSingleDimension_WhenArrayIsSingleDimension_ShouldNotThrow(Array array)
        {
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        }

        private static IEnumerable<object[]> GetMultiDimensionalArrayTestData()
        {
            yield return new object[] { new int[2, 2] };
            yield return new object[] { new double[3, 1] };
            yield return new object[] { new string[1, 1] };
        }

        private static IEnumerable<object[]> GetSingleDimensionalArrayTestData()
        {
            yield return new object[] { new int[5] };
            yield return new object[] { new double[0] };
            yield return new object[] { new string[3] };
        }
    }
}