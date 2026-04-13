// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.IEnumerable.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class WeekPatternTests
    {
        /// <summary>
        /// Verifies that enumerating <see cref="WeekPattern.Empty" /> yields no elements.
        /// </summary>
        [TestMethod]
        public void GetEnumerator_WhenEmpty_ShouldYieldNoDays()
        {
            var days = WeekPattern.Empty.ToList();
            Assert.AreEqual(0, days.Count);
        }

        /// <summary>
        /// Verifies that enumerating a fully-populated <see cref="WeekPattern" /> yields all seven days of
        /// the week.
        /// </summary>
        [TestMethod]
        public void GetEnumerator_WhenAllDaysSelected_ShouldYieldAllSevenDays()
        {
            var days = WeekPattern.FromByte(0b1111111).ToList();
            Assert.AreEqual(7, days.Count);
        }

        /// <summary>
        /// Verifies that enumerating a partial <see cref="WeekPattern" /> yields only the selected days and
        /// no others.
        /// </summary>
        [TestMethod]
        public void GetEnumerator_WhenPartialSelection_ShouldYieldOnlySelectedDays()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            var days = pattern.ToList();

            Assert.AreEqual(3, days.Count);
            CollectionAssert.Contains(days, DayOfWeek.Monday);
            CollectionAssert.Contains(days, DayOfWeek.Wednesday);
            CollectionAssert.Contains(days, DayOfWeek.Friday);
            CollectionAssert.DoesNotContain(days, DayOfWeek.Sunday);
            CollectionAssert.DoesNotContain(days, DayOfWeek.Tuesday);
            CollectionAssert.DoesNotContain(days, DayOfWeek.Thursday);
            CollectionAssert.DoesNotContain(days, DayOfWeek.Saturday);
        }

        /// <summary>
        /// Verifies that enumeration yields days in Sunday-first order, consistent with the
        /// <see cref="DayOfWeek" /> enum and the default string representation.
        /// </summary>
        [TestMethod]
        public void GetEnumerator_WhenCalled_ShouldYieldDaysInSundayFirstOrder()
        {
            var pattern = WeekPattern.FromByte(0b1111111);
            var days = pattern.ToList();
            var expected = new[]
            {
                DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
            };

            CollectionAssert.AreEqual(expected, days,
                "Days must be enumerated in Sunday-first (DayOfWeek enum) order.");
        }

        /// <summary>
        /// Verifies that enumerating <see cref="WeekPattern.Weekdays" /> yields exactly Monday through Friday
        /// in the correct order.
        /// </summary>
        [TestMethod]
        public void GetEnumerator_WhenWeekdays_ShouldYieldWeekdaysInOrder()
        {
            var days = WeekPattern.Weekdays.ToList();
            var expected = new[]
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday
            };

            CollectionAssert.AreEqual(expected, days);
        }

        /// <summary>
        /// Verifies that the enumerated days exactly match the days reported as selected by
        /// <see cref="WeekPattern.Contains" />, across all 128 valid bitmask permutations.
        /// </summary>
        [TestMethod]
        public void GetEnumerator_WhenCalled_ShouldBeConsistentWithContains()
        {
            for (byte mask = 0; mask <= 127; mask++)
            {
                var pattern = WeekPattern.FromByte(mask);

                foreach (DayOfWeek day in pattern)
                    Assert.IsTrue(pattern.Contains(day),
                        $"Enumerator yielded {day} for mask {mask}, but Contains returned false.");

                foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
                    if (!pattern.Contains(day))
                        CollectionAssert.DoesNotContain(pattern.ToList(), day,
                            $"Contains returned false for {day} (mask {mask}), but enumerator yielded it.");
            }
        }

        /// <summary>
        /// Verifies that the number of elements yielded by the enumerator equals
        /// <see cref="WeekPattern.Count" /> for every valid bitmask.
        /// </summary>
        [TestMethod]
        public void GetEnumerator_WhenCalled_ShouldYieldExactlyCountElements()
        {
            for (byte mask = 0; mask <= 127; mask++)
            {
                var pattern = WeekPattern.FromByte(mask);
                Assert.AreEqual(pattern.Count, pattern.Count(),
                    $"Enumerated element count must equal Count for mask {mask}.");
            }
        }
    }
}