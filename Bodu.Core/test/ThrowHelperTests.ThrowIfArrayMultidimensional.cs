// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayMultidimensional.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class ThrowHelperTests
    {
        [TestMethod]
        public void ThrowIfArrayMultidimensional_WhenArrayIsNull_ShouldThrowArgumentNullException()
        {
            Array? array = null;
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                ThrowHelper.ThrowIfArrayMultidimensional(array);
            });
        }

        [TestMethod]
        public void ThrowIfArrayMultidimensional_WhenArrayHasRankTwo_ShouldThrowArgumentException()
        {
            Array array = new int[2, 3];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfArrayMultidimensional(array);
            });
        }

        [TestMethod]
        public void ThrowIfArrayMultidimensional_WhenArrayHasRankThree_ShouldThrowArgumentException()
        {
            Array array = new int[2, 2, 2];
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                ThrowHelper.ThrowIfArrayMultidimensional(array);
            });
        }

        [TestMethod]
        public void ThrowIfArrayMultidimensional_WhenArrayIsSingleDimensional_ShouldNotThrow()
        {
            Array array = new int[5];
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        }

        [TestMethod]
        public void ThrowIfArrayMultidimensional_WhenArrayIsEmptySingleDimensional_ShouldNotThrow()
        {
            Array array = Array.Empty<int>();
            ThrowHelper.ThrowIfArrayMultidimensional(array);
        }
    }
}
