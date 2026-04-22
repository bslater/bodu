// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComparableHelperTests.Coalesce.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿namespace Bodu.Extensions
{
    public partial class ComparableHelperTests
    {
        private static IEnumerable<object[]> GetCoalesceTestData()
        {
            yield return new object[] { 5, 10, 5 };     // First is non-null
            yield return new object[] { null, 10, 10 };  // First is null
            yield return new object[] { null, null, null }; // Both null
        }

        /// <summary>
        /// Verifies that <see cref="ComparableHelper.Coalesce" /> returns the first non-null value, the second value if the first is null, or null if both are null.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetCoalesceTestData), DynamicDataSourceType.Method)]
        public void Coalesce_WhenEvaluatingValues_ShouldReturnExpectedResult(int? first, int? second, int? expected)
        {
            var actual = ComparableHelper.Coalesce(first, second);
            Assert.AreEqual(expected, actual);
        }
    }
}