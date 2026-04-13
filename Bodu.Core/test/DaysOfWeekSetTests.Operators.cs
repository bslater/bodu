// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSetTests.Operators.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        // --------------------------------------------------------------------
        // Bitwise operators
        // --------------------------------------------------------------------

        /// <summary>
        /// Verifies that applying the <c>|</c> operator with <see cref="DaysOfWeekSet.Empty" /> returns a
        /// set equal to the original.
        /// </summary>
        [TestMethod]
        public void OrOperator_WhenCombinedWithEmpty_ShouldReturnOriginal()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Tuesday);
            var actual = original | DaysOfWeekSet.Empty;
            Assert.AreEqual(original, actual);
        }

        /// <summary>
        /// Verifies that applying the <c>|</c> operator with itself returns a set equal to the original.
        /// </summary>
        [TestMethod]
        public void OrOperator_WhenCombinedWithItself_ShouldReturnSame()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Monday);
            var actual = original | original;
            Assert.AreEqual(original, actual);
        }

        /// <summary>
        /// Verifies that applying the <c>|</c> operator to two non-overlapping sets correctly unites all days
        /// from both operands.
        /// </summary>
        [TestMethod]
        public void OrOperator_WhenDisjointSets_ShouldUniteCorrectly()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var b = new DaysOfWeekSet(DayOfWeek.Friday);
            var result = a | b;

            Assert.IsTrue(result[DayOfWeek.Monday]);
            Assert.IsTrue(result[DayOfWeek.Wednesday]);
            Assert.IsTrue(result[DayOfWeek.Friday]);
            Assert.AreEqual(3, result.Count);
        }

        /// <summary>
        /// Verifies that the bitwise OR of <see cref="DaysOfWeekSet.Weekdays" /> and
        /// <see cref="DaysOfWeekSet.Weekend" /> produces a fully-populated set containing all seven days.
        /// </summary>
        [TestMethod]
        public void OrOperator_WhenWeekdaysCombinedWithWeekend_ShouldProduceFullWeek()
        {
            var full = DaysOfWeekSet.Weekdays | DaysOfWeekSet.Weekend;

            Assert.AreEqual(7, full.Count);
            Assert.AreEqual(DaysOfWeekSet.FromByte(0b1111111), full);
        }

        /// <summary>
        /// Verifies that applying the <c>&amp;</c> operator with <see cref="DaysOfWeekSet.Empty" /> returns an
        /// empty set.
        /// </summary>
        [TestMethod]
        public void AndOperator_WhenCombinedWithEmpty_ShouldReturnEmpty()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Monday);
            var actual = original & DaysOfWeekSet.Empty;
            Assert.AreEqual(DaysOfWeekSet.Empty, actual);
        }

        /// <summary>
        /// Verifies that applying the <c>&amp;</c> operator with itself returns a set equal to the original.
        /// </summary>
        [TestMethod]
        public void AndOperator_WhenCombinedWithItself_ShouldReturnSame()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Wednesday);
            var actual = original & original;
            Assert.AreEqual(original, actual);
        }

        /// <summary>
        /// Verifies that the bitwise AND of <see cref="DaysOfWeekSet.Weekdays" /> and
        /// <see cref="DaysOfWeekSet.Weekend" /> returns an empty set, confirming the two are disjoint.
        /// </summary>
        [TestMethod]
        public void AndOperator_WhenWeekdaysIntersectedWithWeekend_ShouldReturnEmpty()
        {
            var intersection = DaysOfWeekSet.Weekdays & DaysOfWeekSet.Weekend;
            Assert.AreEqual(DaysOfWeekSet.Empty, intersection);
        }

        /// <summary>
        /// Verifies that applying the <c>^</c> operator with <see cref="DaysOfWeekSet.Empty" /> returns a
        /// set equal to the original.
        /// </summary>
        [TestMethod]
        public void XorOperator_WhenCombinedWithEmpty_ShouldReturnOriginal()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Friday);
            var actual = original ^ DaysOfWeekSet.Empty;
            Assert.AreEqual(original, actual);
        }

        /// <summary>
        /// Verifies that applying the <c>^</c> operator with itself produces an empty set.
        /// </summary>
        [TestMethod]
        public void XorOperator_WhenCombinedWithItself_ShouldReturnEmpty()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Thursday);
            var actual = original ^ original;
            Assert.AreEqual(DaysOfWeekSet.Empty, actual);
        }

        /// <summary>
        /// Verifies that the <c>^</c> operator retains only days that appear in exactly one operand when
        /// the two sets partially overlap.
        /// </summary>
        [TestMethod]
        public void XorOperator_WhenPartialOverlap_ShouldRetainNonOverlappingDays()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var b = new DaysOfWeekSet(DayOfWeek.Wednesday, DayOfWeek.Friday);
            var result = a ^ b;

            Assert.IsTrue(result[DayOfWeek.Monday], "Monday appears only in a — should be retained.");
            Assert.IsFalse(result[DayOfWeek.Wednesday], "Wednesday appears in both — should be cancelled.");
            Assert.IsTrue(result[DayOfWeek.Friday], "Friday appears only in b — should be retained.");
        }

        /// <summary>
        /// Verifies that applying the <c>~</c> operator to <see cref="DaysOfWeekSet.Empty" /> produces a
        /// fully-populated set containing all seven days.
        /// </summary>
        [TestMethod]
        public void ComplementOperator_WhenAppliedToEmpty_ShouldReturnFullSet()
        {
            var actual = ~DaysOfWeekSet.Empty;
            Assert.AreEqual(DaysOfWeekSet.FromByte(0b1111111), actual);
        }

        /// <summary>
        /// Verifies that applying the <c>~</c> operator to a fully-populated set produces an empty set.
        /// </summary>
        [TestMethod]
        public void ComplementOperator_WhenAppliedToFullSet_ShouldReturnEmpty()
        {
            var actual = ~DaysOfWeekSet.FromByte(0b1111111);
            Assert.AreEqual(DaysOfWeekSet.Empty, actual);
        }

        /// <summary>
        /// Verifies that the bitwise complement of <see cref="DaysOfWeekSet.Weekdays" /> equals
        /// <see cref="DaysOfWeekSet.Weekend" />.
        /// </summary>
        [TestMethod]
        public void ComplementOperator_WhenAppliedToWeekdays_ShouldReturnWeekend()
        {
            Assert.AreEqual(DaysOfWeekSet.Weekend, ~DaysOfWeekSet.Weekdays);
        }

        /// <summary>
        /// Verifies that the bitwise complement of <see cref="DaysOfWeekSet.Weekend" /> equals
        /// <see cref="DaysOfWeekSet.Weekdays" />.
        /// </summary>
        [TestMethod]
        public void ComplementOperator_WhenAppliedToWeekend_ShouldReturnWeekdays()
        {
            Assert.AreEqual(DaysOfWeekSet.Weekdays, ~DaysOfWeekSet.Weekend);
        }

        /// <summary>
        /// Verifies that applying the <c>~</c> operator twice to the same instance returns the original value,
        /// confirming the double-complement identity.
        /// </summary>
        [TestMethod]
        public void ComplementOperator_WhenAppliedTwice_ShouldReturnOriginal()
        {
            var original = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Thursday);
            Assert.AreEqual(original, ~~original);
        }

        // --------------------------------------------------------------------
        // Equality operators
        // --------------------------------------------------------------------

        /// <summary>
        /// Verifies that the <c>==</c> operator returns <see langword="true" /> for two instances with
        /// identical selected days.
        /// </summary>
        [TestMethod]
        public void EqualityOperator_WhenSameDays_ShouldReturnTrue()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Friday);
            var b = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Friday);
            Assert.IsTrue(a == b);
        }

        /// <summary>
        /// Verifies that the <c>==</c> operator returns <see langword="false" /> for two instances with
        /// different selected days.
        /// </summary>
        [TestMethod]
        public void EqualityOperator_WhenDifferentDays_ShouldReturnFalse()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday);
            var b = new DaysOfWeekSet(DayOfWeek.Tuesday);
            Assert.IsFalse(a == b);
        }

        /// <summary>
        /// Verifies that the <c>!=</c> operator returns <see langword="true" /> for two instances with
        /// different selected days.
        /// </summary>
        [TestMethod]
        public void InequalityOperator_WhenDifferentDays_ShouldReturnTrue()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday);
            var b = new DaysOfWeekSet(DayOfWeek.Tuesday);
            Assert.IsTrue(a != b);
        }

        /// <summary>
        /// Verifies that the <c>!=</c> operator returns <see langword="false" /> for two instances with
        /// identical selected days.
        /// </summary>
        [TestMethod]
        public void InequalityOperator_WhenSameDays_ShouldReturnFalse()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Wednesday);
            var b = new DaysOfWeekSet(DayOfWeek.Wednesday);
            Assert.IsFalse(a != b);
        }

        // --------------------------------------------------------------------
        // Implicit conversions
        // --------------------------------------------------------------------

        /// <summary>
        /// Verifies that the implicit conversion to <see cref="byte" /> preserves the underlying bitmask
        /// value across a round-trip.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        [DataRow((byte)5)]
        [DataRow((byte)127)]
        public void ImplicitConversionToByte_WhenRoundTripped_ShouldMatchOriginal(byte original)
        {
            DaysOfWeekSet set = DaysOfWeekSet.FromByte(original);
            byte roundTripped = set;
            Assert.AreEqual(original, roundTripped);
        }

        /// <summary>
        /// Verifies that the implicit conversion to <see cref="int" /> preserves the underlying bitmask value.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)5)]
        [DataRow((byte)127)]
        public void ImplicitConversionToInt_WhenRoundTripped_ShouldMatchOriginal(byte original)
        {
            DaysOfWeekSet set = DaysOfWeekSet.FromByte(original);
            int converted = set;
            Assert.AreEqual((int)original, converted);
        }

        /// <summary>
        /// Verifies that the implicit conversion to <see cref="short" /> preserves the underlying bitmask
        /// value.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)5)]
        [DataRow((byte)127)]
        public void ImplicitConversionToShort_WhenRoundTripped_ShouldMatchOriginal(byte original)
        {
            DaysOfWeekSet set = DaysOfWeekSet.FromByte(original);
            short converted = set;
            Assert.AreEqual((short)original, converted);
        }

        // --------------------------------------------------------------------
        // Numeric conversion methods
        // --------------------------------------------------------------------

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToByte" /> returns the underlying bitmask value as a
        /// <see cref="byte" />.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        [DataRow((byte)127)]
        public void ToByte_WhenCalled_ShouldReturnUnderlyingBitmask(byte value)
        {
            var set = DaysOfWeekSet.FromByte(value);
            Assert.AreEqual(value, set.ToByte());
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToInt16" /> returns the underlying bitmask value as a
        /// <see cref="short" />.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        [DataRow((byte)127)]
        public void ToInt16_WhenCalled_ShouldReturnUnderlyingBitmaskAsShort(byte value)
        {
            var set = DaysOfWeekSet.FromByte(value);
            Assert.AreEqual((short)value, set.ToInt16());
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToInt32" /> returns the underlying bitmask value as an
        /// <see cref="int" />.
        /// </summary>
        [TestMethod]
        [DataRow((byte)0)]
        [DataRow((byte)1)]
        [DataRow((byte)127)]
        public void ToInt32_WhenCalled_ShouldReturnUnderlyingBitmaskAsInt(byte value)
        {
            var set = DaysOfWeekSet.FromByte(value);
            Assert.AreEqual((int)value, set.ToInt32());
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToByte" />, the implicit <see cref="byte" /> conversion,
        /// and the implicit <see cref="int" /> conversion all return the same underlying value for the same
        /// instance.
        /// </summary>
        [TestMethod]
        public void NumericConversions_WhenCalledOnSameInstance_ShouldProduceConsistentValues()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            byte viaToByte = set.ToByte();
            short viaToInt16 = set.ToInt16();
            int viaToInt32 = set.ToInt32();
            byte viaImplicitByte = set;
            int viaImplicitInt = set;
            short viaImplicitShort = set;

            Assert.AreEqual(viaToByte, (byte)viaToInt32, "ToByte and ToInt32 should agree.");
            Assert.AreEqual(viaToInt16, (short)viaToInt32, "ToInt16 and ToInt32 should agree.");
            Assert.AreEqual(viaToByte, viaImplicitByte, "ToByte and implicit byte should agree.");
            Assert.AreEqual(viaToInt32, viaImplicitInt, "ToInt32 and implicit int should agree.");
            Assert.AreEqual(viaToInt16, viaImplicitShort, "ToInt16 and implicit short should agree.");
        }

        // --------------------------------------------------------------------
        // Comparison operators
        // --------------------------------------------------------------------

        /// <summary>
        /// Verifies that the <c>&lt;</c> operator returns <see langword="true" /> when the left operand has a
        /// smaller underlying bitmask than the right operand. This is a regression test for a defect where the
        /// operator was absent, preventing compilation of any code that compared two
        /// <see cref="DaysOfWeekSet" /> instances ordinally.
        /// </summary>
        [TestMethod]
        public void OperatorLessThan_WhenLeftHasSmallerBitmask_ShouldReturnTrue()
        {
            var fewer = new DaysOfWeekSet(DayOfWeek.Monday);
            var more = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.IsTrue(fewer < more);
        }

        /// <summary>
        /// Verifies that the <c>&lt;</c> operator returns <see langword="false" /> when the left operand has
        /// a larger underlying bitmask than the right operand.
        /// </summary>
        [TestMethod]
        public void OperatorLessThan_WhenLeftHasLargerBitmask_ShouldReturnFalse()
        {
            var more = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var fewer = new DaysOfWeekSet(DayOfWeek.Monday);

            Assert.IsFalse(more < fewer);
        }

        /// <summary>
        /// Verifies that the <c>&lt;</c> operator returns <see langword="false" /> when both operands are equal.
        /// </summary>
        [TestMethod]
        public void OperatorLessThan_WhenBothOperandsAreEqual_ShouldReturnFalse()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var b = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.IsFalse(a < b);
        }

        /// <summary>
        /// Verifies that the <c>&lt;=</c> operator returns <see langword="true" /> when the left operand has
        /// a smaller underlying bitmask than the right operand.
        /// </summary>
        [TestMethod]
        public void OperatorLessThanOrEqual_WhenLeftHasSmallerBitmask_ShouldReturnTrue()
        {
            var fewer = new DaysOfWeekSet(DayOfWeek.Monday);
            var more = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.IsTrue(fewer <= more);
        }

        /// <summary>
        /// Verifies that the <c>&lt;=</c> operator returns <see langword="true" /> when both operands are equal.
        /// </summary>
        [TestMethod]
        public void OperatorLessThanOrEqual_WhenBothOperandsAreEqual_ShouldReturnTrue()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Friday);
            var b = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Friday);

            Assert.IsTrue(a <= b);
        }

        /// <summary>
        /// Verifies that the <c>&lt;=</c> operator returns <see langword="false" /> when the left operand has
        /// a larger underlying bitmask than the right operand.
        /// </summary>
        [TestMethod]
        public void OperatorLessThanOrEqual_WhenLeftHasLargerBitmask_ShouldReturnFalse()
        {
            var more = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var fewer = new DaysOfWeekSet(DayOfWeek.Monday);

            Assert.IsFalse(more <= fewer);
        }

        /// <summary>
        /// Verifies that the <c>&gt;</c> operator returns <see langword="true" /> when the left operand has a
        /// larger underlying bitmask than the right operand.
        /// </summary>
        [TestMethod]
        public void OperatorGreaterThan_WhenLeftHasLargerBitmask_ShouldReturnTrue()
        {
            var more = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var fewer = new DaysOfWeekSet(DayOfWeek.Monday);

            Assert.IsTrue(more > fewer);
        }

        /// <summary>
        /// Verifies that the <c>&gt;</c> operator returns <see langword="false" /> when both operands are equal.
        /// </summary>
        [TestMethod]
        public void OperatorGreaterThan_WhenBothOperandsAreEqual_ShouldReturnFalse()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday);
            var b = new DaysOfWeekSet(DayOfWeek.Monday);

            Assert.IsFalse(a > b);
        }

        /// <summary>
        /// Verifies that the <c>&gt;</c> operator returns <see langword="false" /> when the left operand has
        /// a smaller underlying bitmask than the right operand.
        /// </summary>
        [TestMethod]
        public void OperatorGreaterThan_WhenLeftHasSmallerBitmask_ShouldReturnFalse()
        {
            var fewer = new DaysOfWeekSet(DayOfWeek.Monday);
            var more = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.IsFalse(fewer > more);
        }

        /// <summary>
        /// Verifies that the <c>&gt;=</c> operator returns <see langword="true" /> when the left operand has
        /// a larger underlying bitmask than the right operand.
        /// </summary>
        [TestMethod]
        public void OperatorGreaterThanOrEqual_WhenLeftHasLargerBitmask_ShouldReturnTrue()
        {
            var more = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);
            var fewer = new DaysOfWeekSet(DayOfWeek.Monday);

            Assert.IsTrue(more >= fewer);
        }

        /// <summary>
        /// Verifies that the <c>&gt;=</c> operator returns <see langword="true" /> when both operands are equal.
        /// </summary>
        [TestMethod]
        public void OperatorGreaterThanOrEqual_WhenBothOperandsAreEqual_ShouldReturnTrue()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Friday);
            var b = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Friday);

            Assert.IsTrue(a >= b);
        }

        /// <summary>
        /// Verifies that the <c>&gt;=</c> operator returns <see langword="false" /> when the left operand has
        /// a smaller underlying bitmask than the right operand.
        /// </summary>
        [TestMethod]
        public void OperatorGreaterThanOrEqual_WhenLeftHasSmallerBitmask_ShouldReturnFalse()
        {
            var fewer = new DaysOfWeekSet(DayOfWeek.Monday);
            var more = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);

            Assert.IsFalse(fewer >= more);
        }

        /// <summary>
        /// Verifies that all four comparison operators are mutually consistent for a pair of distinct instances —
        /// exactly one of <c>&lt;</c> or <c>&gt;</c> is <see langword="true" /> when the operands differ.
        /// </summary>
        [TestMethod]
        public void OperatorComparisons_WhenOperandsDiffer_ShouldBeConsistentAcrossAllFourOperators()
        {
            var small = new DaysOfWeekSet(DayOfWeek.Monday);
            var large = DaysOfWeekSet.Weekdays;

            Assert.IsTrue(small < large);
            Assert.IsTrue(small <= large);
            Assert.IsFalse(small > large);
            Assert.IsFalse(small >= large);
        }

        /// <summary>
        /// Verifies that all four comparison operators are mutually consistent when both operands are equal —
        /// both <c>&lt;=</c> and <c>&gt;=</c> are <see langword="true" /> and both strict operators are
        /// <see langword="false" />.
        /// </summary>
        [TestMethod]
        public void OperatorComparisons_WhenOperandsAreEqual_ShouldBeConsistentAcrossAllFourOperators()
        {
            var a = DaysOfWeekSet.Weekend;
            var b = DaysOfWeekSet.Weekend;

            Assert.IsFalse(a < b);
            Assert.IsTrue(a <= b);
            Assert.IsFalse(a > b);
            Assert.IsTrue(a >= b);
        }

        /// <summary>
        /// Verifies that the comparison operators are consistent with the <see cref="System.IComparable{T}" />
        /// implementation, so that ordinal ordering is identical regardless of which API is used.
        /// </summary>
        [TestMethod]
        public void OperatorComparisons_WhenComparedViaIComparable_ShouldMatchOperatorResults()
        {
            var a = new DaysOfWeekSet(DayOfWeek.Monday);
            var b = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday);

            int cmp = a.CompareTo(b);

            Assert.IsTrue(cmp < 0, "IComparable.CompareTo should indicate a < b.");
            Assert.IsTrue(a < b, "operator < should agree with IComparable.");
            Assert.IsTrue(a <= b, "operator <= should agree with IComparable.");
            Assert.IsFalse(a > b, "operator > should agree with IComparable.");
            Assert.IsFalse(a >= b, "operator >= should agree with IComparable.");
        }
    }
}