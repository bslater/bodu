// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Indexer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        /// <summary>
        /// Verifies that setting a valid <see cref="DayOfWeek" /> to <see langword="true" /> via the indexer
        /// causes the getter to return <see langword="true" /> for that day.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetValidDays), DynamicDataSourceType.Method)]
        public void IndexerSet_WhenSettingValidDayToTrue_ShouldReflectChange(DayOfWeek day)
        {
            var set = new DaysOfWeekSet();
            set[day] = true;
            Assert.IsTrue(set[day]);
        }

        /// <summary>
        /// Verifies that setting a previously selected day to <see langword="false" /> via the indexer clears
        /// it from the set, causing <see cref="DaysOfWeekSet.Count" /> to decrease accordingly.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetValidDays), DynamicDataSourceType.Method)]
        public void IndexerSet_WhenClearingSelectedDay_ShouldUnsetDay(DayOfWeek day)
        {
            var set = new DaysOfWeekSet(day);
            set[day] = false;

            Assert.IsFalse(set[day]);
            Assert.AreEqual(0, set.Count);
        }

        /// <summary>
        /// Verifies that setting the same <see cref="DayOfWeek" /> to <see langword="true" /> twice is
        /// idempotent and does not cause <see cref="DaysOfWeekSet.Count" /> to exceed one.
        /// </summary>
        [TestMethod]
        public void IndexerSet_WhenSameDaySetTwice_ShouldBeIdempotent()
        {
            var set = new DaysOfWeekSet();
            set[DayOfWeek.Wednesday] = true;
            set[DayOfWeek.Wednesday] = true;

            Assert.IsTrue(set[DayOfWeek.Wednesday]);
            Assert.AreEqual(1, set.Count);
        }

        /// <summary>
        /// Verifies that the indexer getter throws <see cref="ArgumentOutOfRangeException" /> when an invalid
        /// <see cref="DayOfWeek" /> value is used.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(7)]
        [DataRow(10)]
        public void IndexerGet_WhenInvalidDayIndex_ShouldThrowArgumentOutOfRangeException(int invalidDayIndex)
        {
            var set = new DaysOfWeekSet();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                var _ = set[(DayOfWeek)invalidDayIndex];
            });
        }

        /// <summary>
        /// Verifies that the indexer setter throws <see cref="ArgumentOutOfRangeException" /> when an invalid
        /// <see cref="DayOfWeek" /> value is used.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(7)]
        [DataRow(10)]
        public void IndexerSet_WhenInvalidDayIndex_ShouldThrowArgumentOutOfRangeException(int invalidDayIndex)
        {
            var set = new DaysOfWeekSet();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                set[(DayOfWeek)invalidDayIndex] = true;
            });
        }

        private static IEnumerable<object[]> GetValidDays()
        {
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                yield return new object[] { day };
        }
    }
}