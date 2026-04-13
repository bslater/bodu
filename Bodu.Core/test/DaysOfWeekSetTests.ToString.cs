namespace Bodu
{
    public partial class DaysOfWeekSetTests
    {
        [TestMethod]
        public void ToString_WhenCalledWithoutParameters_ShouldUseDefaultFormat()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday);
            string output = set.ToString();
            Assert.AreEqual("SM_____", output);
        }

        [TestMethod]
        [DataRow("S", "SM____S")] // Sunday-first
        [DataRow("s", "SM____S")]
        [DataRow("M", "M____SS")] // Monday-first
        [DataRow("m", "M____SS")]
        [DataRow("B", "1100001")] // Binary (Sunday + Monday + Saturday selected)
        [DataRow("b", "1100001")]
        public void ToString_WhenValidFormat_ShouldFormatCorrectly(string format, string expected)
        {
            var set = new DaysOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
            string output = set.ToString(format);
            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void ToString_WhenInvalidFormat_ShouldThrowExactly()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Monday);
            Assert.ThrowsExactly<ArgumentException>(() => set.ToString("X"));
        }

        [TestMethod]
        [DataRow("S", "SM____S")] // Sunday-first
        [DataRow("M", "M____SS")] // Monday-first
        [DataRow("B", "1100001")] // Binary
        public void ToString_WhenFormatAndProviderProvided_ShouldFormatCorrectly(string format, string expected)
        {
            var set = new DaysOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Saturday);
            string output = set.ToString(format, null);
            Assert.AreEqual(expected, output);
        }

        [TestMethod]
        public void ToString_WhenOnlyProviderProvided_ShouldUseDefaultFormat()
        {
            var set = new DaysOfWeekSet(DayOfWeek.Sunday, DayOfWeek.Monday);
            string output = set.ToString(provider: null);
            Assert.AreEqual("SM_____", output);
        }

        [TestMethod]
        [DynamicData(nameof(GetAllBitmaskPermutationTestData), DynamicDataSourceType.Method)]
        public void ToString_WhenCalled_ShouldReturnUpperString(byte value, string expected, string _)
        {
            var set = DaysOfWeekSet.FromByte(value);
            Assert.AreEqual(expected, set.ToString());
        }

        [TestMethod]
        [DynamicData(nameof(GetAllBitmaskPermutationTestData), DynamicDataSourceType.Method)]
        public void ToString_WhenBinaryFormat_ShouldReturnUpperString(byte value, string _, string expected)
        {
            var set = DaysOfWeekSet.FromByte(value);
            Assert.AreEqual(expected, set.ToString("b"));
        }

        /// <summary>
        /// Verifies that the default <see cref="DaysOfWeekSet.ToString()" /> overload returns a Sunday-first string with underscore
        /// for unselected days.
        /// </summary>
        [TestMethod]
        public void ToString_WhenCalledWithNoArguments_ShouldReturnSundayFirstWithUnderscores()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            Assert.AreEqual("_M_W_F_", days.ToString());
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> with the <c>'S'</c> format returns a correct Sunday-first
        /// string. This is a regression test for a defect where the documented example showed the wrong output (<c>"S_M_W_F"</c>
        /// instead of <c>"_M_W_F_"</c>).
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsSundayFirst_ShouldReturnCorrectSundayFirstString()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            Assert.AreEqual("_M_W_F_", days.ToString("S"));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> with the <c>'M'</c> format returns a correct Monday-first
        /// string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsMondayFirst_ShouldReturnCorrectMondayFirstString()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            // Monday-first: M _ W _ F _ _  (Sat and Sun unselected)
            Assert.AreEqual("M_W_F__", days.ToString("M"));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> with the two-character dash format (<c>"MD"</c>) returns a
        /// correct Monday-first string with dashes for unselected days.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsMondayFirstWithDash_ShouldReturnCorrectString()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            Assert.AreEqual("M-W-F--", days.ToString("MD"));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> with the <c>'0'</c> binary format specifier returns the
        /// correct binary string. This is a regression test for a defect where the documented <c>'0'</c> specifier was not
        /// recognised and caused an <see cref="ArgumentException" /> to be thrown.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIs0_ShouldReturnBinaryString()
        {
            // Monday, Wednesday, Friday: Sun=0 Mon=1 Tue=0 Wed=1 Thu=0 Fri=1 Sat=0 → "0101010"
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            Assert.AreEqual("0101010", days.ToString("0"));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> with the <c>'1'</c> binary format specifier returns the
        /// correct binary string. This is a regression test for the same defect as
        /// <see cref="ToString_WhenFormatIs0_ShouldReturnBinaryString" />.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIs1_ShouldReturnBinaryString()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            Assert.AreEqual("0101010", days.ToString("1"));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> with the <c>'B'</c> binary format specifier returns the
        /// correct binary string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsB_ShouldReturnBinaryString()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            Assert.AreEqual("0101010", days.ToString("B"));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> with the <c>"01"</c> binary format specifier returns the
        /// correct binary string.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIs01_ShouldReturnBinaryString()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            Assert.AreEqual("0101010", days.ToString("01"));
        }

        /// <summary>
        /// Verifies that all recognised binary format specifiers (<c>'0'</c>, <c>'1'</c>, <c>'B'</c>, <c>"01"</c>) return
        /// identical strings for the same instance.
        /// </summary>
        [TestMethod]
        public void ToString_WhenAllBinarySpecifiersUsedOnSameInstance_ShouldProduceIdenticalOutput()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            string s0 = days.ToString("0");
            string s1 = days.ToString("1");
            string sB = days.ToString("B");
            string s01 = days.ToString("01");

            Assert.AreEqual(s0, s1, "Formats '0' and '1' should produce identical output.");
            Assert.AreEqual(s0, sB, "Formats '0' and 'B' should produce identical output.");
            Assert.AreEqual(s0, s01, "Formats '0' and \"01\" should produce identical output.");
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> with the weekday set returns the correct binary output
        /// for the known <c>Weekdays</c> static instance.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIs0AndAllWeekdaysSelected_ShouldReturnCorrectBinaryString()
        {
            // Weekdays = Monday–Friday. Binary Sunday-first: 0111110
            Assert.AreEqual("0111110", DaysOfWeekSet.Weekdays.ToString("0"));
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet.ToString(string)" /> throws <see cref="ArgumentException" /> when the format
        /// string is unrecognised.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormatIsUnrecognised_ShouldThrowArgumentException()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday);

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                _ = days.ToString("Z");
            });
        }

        /// <summary>
        /// Verifies that <see cref="DaysOfWeekSet" /> implements <see cref="IFormattable" /> and that composite format strings
        /// correctly invoke <see cref="DaysOfWeekSet.ToString(string, IFormatProvider)" />. This is a regression test for a
        /// defect where <c>IFormattable</c> was not declared on the struct, causing <c>string.Format</c> to ignore the format
        /// specifier and always call <see cref="DaysOfWeekSet.ToString()" />.
        /// </summary>
        [TestMethod]
        public void ToString_WhenFormattedViaStringFormat_ShouldApplyFormatSpecifier()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);

            // Without IFormattable: string.Format ignores the specifier and calls ToString()
            //   → "_M_W_F_" (Sunday-first, the default)
            // With IFormattable: format 'M' is forwarded to ToString("M", provider)
            //   → "M_W_F__" (Monday-first)
            string result = string.Format("{0:M}", days);

            Assert.AreEqual("M_W_F__", result,
                "string.Format should apply the format specifier via IFormattable.");
        }

        /// <summary>
        /// Verifies that the <see cref="DaysOfWeekSet" /> struct explicitly implements <see cref="IFormattable" />.
        /// </summary>
        [TestMethod]
        public void ToString_WhenCastToIFormattable_ShouldSucceed()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday);

            // Boxing a value type that implements IFormattable must not return null.
            var formattable = days as IFormattable;

            Assert.IsNotNull(formattable,
                "DaysOfWeekSet must implement IFormattable for composite formatting to work.");
        }

        /// <summary>
        /// Verifies that calling <see cref="IFormattable.ToString(string, IFormatProvider)" /> directly produces the same
        /// result as calling the concrete <see cref="DaysOfWeekSet.ToString(string, IFormatProvider)" /> overload.
        /// </summary>
        [TestMethod]
        public void ToString_WhenCalledViaIFormattableInterface_ShouldMatchConcreteOverload()
        {
            var days = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
            IFormattable formattable = days;

            string viaInterface = formattable.ToString("M", null);
            string viaConcrete = days.ToString("M", null);

            Assert.AreEqual(viaConcrete, viaInterface);
        }
    }
}