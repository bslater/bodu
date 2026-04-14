// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPatternTests.ToString.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu
{
    public partial class WeekPatternTests
    {
        /// <summary>
        /// Verifies that the default <see cref="WeekPattern.ToString()" /> overload returns a Sunday-first
        /// string with underscore for unselected days.
        /// </summary>
        [TestMethod]
        public void ToString_WhenCalledWithoutParameters_ShouldUseDefaultFormat()
        {
            var pattern = new WeekPattern(DayOfWeek.Sunday, DayOfWeek.Monday);
            Assert.AreEqual("SM_____", pattern.ToString());
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> produces the correct output for each
        /// recognised format specifier.
        /// </summary>
        [TestMethod]
        [DataRow("S", "SM____S")] // Sunday-first
        [DataRow("s", "SM____S")]
        [DataRow("M", "M____SS")] // Monday-first
        [DataRow("m", "M____SS")]
        [DataRow("B", "1100001")] // Binary (Sunday + Monday + Saturday selected)
        [DataRow("b", "1100001")]
        public void ToString_WhenValidFormat_ShouldFormatCorrectly(string format, string expected)
        {
            var pattern = new WeekPattern(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
            Assert.AreEqual(expected, pattern.ToString(format));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> throws <see cref="ArgumentException" />
        /// when an unrecognised format specifier is provided.
        /// </summary>
        [TestMethod]
        public void ToString_WhenInvalidFormat_ShouldThrowArgumentException()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday);
            Assert.ThrowsExactly<ArgumentException>(() => pattern.ToString("X"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string, IFormatProvider)" /> produces the correct
        /// output when both format and provider are specified.
        /// </summary>
        [TestMethod]
        [DataRow("S", "SM____S")]
        [DataRow("M", "M____SS")]
        [DataRow("B", "1100001")]
        public void ToString_WhenFormatAndProviderProvided_ShouldFormatCorrectly(string format, string expected)
        {
            var pattern = new WeekPattern(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
            Assert.AreEqual(expected, pattern.ToString(format, null));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(IFormatProvider)" /> uses the default Sunday-first
        /// format when only a provider is supplied.
        /// </summary>
        [TestMethod]
        public void ToString_WhenOnlyProviderProvided_ShouldUseDefaultFormat()
        {
            var pattern = new WeekPattern(DayOfWeek.Sunday, DayOfWeek.Monday);
            Assert.AreEqual("SM_____", pattern.ToString(provider: null));
        }

        /// <summary>
        /// Verifies that the default <see cref="WeekPattern.ToString()" /> overload returns the expected
        /// symbol string for every valid bitmask permutation.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetAllBitmaskPermutationTestData), DynamicDataSourceType.Method)]
        public void ToString_WhenCalled_ShouldReturnExpectedSymbolString(byte value, string expected, string _)
        {
            Assert.AreEqual(expected, WeekPattern.FromByte(value).ToString());
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the binary format <c>'b'</c>
        /// returns the expected binary string for every valid bitmask permutation.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(GetAllBitmaskPermutationTestData), DynamicDataSourceType.Method)]
        public void ToString_WhenBinaryFormat_ShouldReturnExpectedBinaryString(byte value, string _, string expected)
        {
            Assert.AreEqual(expected, WeekPattern.FromByte(value).ToString("b"));
        }

        /// <summary>
        /// Verifies that the default <see cref="WeekPattern.ToString()" /> overload returns a Sunday-first
        /// string with underscore for unselected days.
        /// </summary>
        [TestMethod]
        public void ToString_WhenCalledWithNoArguments_ShouldReturnSundayFirstWithUnderscores()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Assert.AreEqual("_M_W_F_", pattern.ToString());
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the <c>'S'</c> format returns a
        /// correct Sunday-first string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsSundayFirst_ShouldReturnCorrectSundayFirstString()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Assert.AreEqual("_M_W_F_", pattern.ToString("S"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the <c>'M'</c> format returns a
        /// correct Monday-first string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsMondayFirst_ShouldReturnCorrectMondayFirstString()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Assert.AreEqual("M_W_F__", pattern.ToString("M"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the two-character dash format
        /// (<c>"MD"</c>) returns a correct Monday-first string with dashes for unselected days.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsMondayFirstWithDash_ShouldReturnCorrectString()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Assert.AreEqual("M-W-F--", pattern.ToString("MD"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the <c>'0'</c> binary format
        /// specifier returns the correct binary string. This is a regression test for a defect where the
        /// documented <c>'0'</c> specifier was not recognised and caused an <see cref="ArgumentException" />.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIs0_ShouldReturnBinaryString()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Assert.AreEqual("0101010", pattern.ToString("0"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the <c>'1'</c> binary format
        /// specifier returns the correct binary string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIs1_ShouldReturnBinaryString()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Assert.AreEqual("0101010", pattern.ToString("1"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the <c>'B'</c> binary format
        /// specifier returns the correct binary string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsB_ShouldReturnBinaryString()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Assert.AreEqual("0101010", pattern.ToString("B"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the <c>"01"</c> binary format
        /// specifier returns the correct binary string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIs01_ShouldReturnBinaryString()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Assert.AreEqual("0101010", pattern.ToString("01"));
        }

        /// <summary>
        /// Verifies that all recognised binary format specifiers (<c>'0'</c>, <c>'1'</c>, <c>'B'</c>,
        /// <c>"01"</c>) return identical strings for the same instance.
        /// </summary>
        [TestMethod]
        public void ToString_WhenAllBinarySpecifiersUsedOnSameInstance_ShouldProduceIdenticalOutput()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            string s0 = pattern.ToString("0");
            string s1 = pattern.ToString("1");
            string sB = pattern.ToString("B");
            string s01 = pattern.ToString("01");

            Assert.AreEqual(s0, s1, "Formats '0' and '1' should produce identical output.");
            Assert.AreEqual(s0, sB, "Formats '0' and 'B' should produce identical output.");
            Assert.AreEqual(s0, s01, "Formats '0' and \"01\" should produce identical output.");
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> with the <c>'0'</c> format and the
        /// <see cref="WeekPattern.Weekdays" /> instance returns the correct binary string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIs0AndAllWeekdaysSelected_ShouldReturnCorrectBinaryString()
        {
            Assert.AreEqual("0111110", WeekPattern.Weekdays.ToString("0"));
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern.ToString(string)" /> throws <see cref="ArgumentException" />
        /// when the format string is unrecognised.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsUnrecognised_ShouldThrowArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                _ = new WeekPattern(DayOfWeek.Monday).ToString("Z");
            });
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern" /> implements <see cref="IFormattable" /> and that composite
        /// format strings correctly invoke <see cref="WeekPattern.ToString(string, IFormatProvider)" />. This
        /// is a regression test for a defect where <c>IFormattable</c> was not declared, causing
        /// <c>string.Format</c> to always call the default <see cref="WeekPattern.ToString()" />.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormattedViaStringFormat_ShouldApplyFormatSpecifier()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            string result = string.Format("{0:M}", pattern);

            Assert.AreEqual("M_W_F__", result,
                "string.Format should apply the format specifier via IFormattable.");
        }

        /// <summary>
        /// Verifies that <see cref="WeekPattern" /> explicitly implements <see cref="IFormattable" />.
        /// </summary>
        [TestMethod]
        public void ToString_WhenCastToIFormattable_ShouldSucceed()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday);
            var formattable = pattern as IFormattable;

            Assert.IsNotNull(formattable,
                "WeekPattern must implement IFormattable for composite formatting to work.");
        }

        /// <summary>
        /// Verifies that calling <see cref="IFormattable.ToString(string, IFormatProvider)" /> directly
        /// produces the same result as the concrete <see cref="WeekPattern.ToString(string, IFormatProvider)" />
        /// overload.
        /// </summary>
        [TestMethod]
        public void ToString_WhenCalledViaIFormattableInterface_ShouldMatchConcreteOverload()
        {
            var pattern = new WeekPattern(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            IFormattable formattable = pattern;

            Assert.AreEqual(pattern.ToString("M", null), formattable.ToString("M", null));
        }
    }
}