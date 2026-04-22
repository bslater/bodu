// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayIsNotZeroBased.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        /// <summary>
        /// Verifies that Throw If Array Is Not Zero Based, when Array Is Not Zero Based, throws Exactly.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetNonZeroBasedArrayTestData), DynamicDataSourceType.Method)]
        public void ThrowIfArrayIsNotZeroBased_WhenArrayIsNotZeroBased_ShouldThrowExactly(Array array)
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
            });
        }

        /// <summary>
        /// Verifies that Throw If Array Is Not Zero Based, when Array Is Zero Based, does not Throw.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetZeroBasedArrayTestData), DynamicDataSourceType.Method)]
        public void ThrowIfArrayIsNotZeroBased_WhenArrayIsZeroBased_ShouldNotThrow(Array array)
        {
            ThrowHelper.ThrowIfArrayIsNotZeroBased(array);
        }

        private static IEnumerable<object[]> GetNonZeroBasedArrayTestData()
        {
            yield return new object[] { Array.CreateInstance(typeof(int), new int[] { 5 }, new int[] { 1 }) };
            yield return new object[] { Array.CreateInstance(typeof(string), new int[] { 3 }, new int[] { -10 }) };
        }

        private static IEnumerable<object[]> GetZeroBasedArrayTestData()
        {
            yield return new object[] { new int[0] };
            yield return new object[] { new string[5] };
            yield return new object[] { Array.CreateInstance(typeof(double), new int[] { 4 }) }; // Zero-based by default
        }
    }
}