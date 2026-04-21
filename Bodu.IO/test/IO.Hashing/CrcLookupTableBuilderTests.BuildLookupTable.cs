namespace Bodu.IO.Hashing
{
    public partial class CrcLookupTableBuilderTests
    {
        [TestMethod]
        public void BuildLookupTable_AllOnesPolynomial_ShouldNotOverflow()
        {
            const int size = 16;
            ulong poly = (1UL << size) - 1;
            var table = CrcLookupTableBuilder.BuildLookupTable(size, poly, false);

            ulong mask = ulong.MaxValue >> (64 - size);
            foreach (var value in table)
            {
                Assert.IsTrue((value & ~mask) == 0);
            }
        }

        [TestMethod]
        public void BuildLookupTable_PolynomialHasExcessBits_ShouldStillMaskCorrectly()
        {
            const int size = 4;
            ulong poly = 0xFFUL; // Higher than 4 bits
            var table = CrcLookupTableBuilder.BuildLookupTable(size, poly, false);

            ulong mask = ulong.MaxValue >> (64 - size);
            foreach (ulong entry in table)
            {
                Assert.IsTrue((entry & ~mask) == 0, $"Entry {entry:X} exceeds size {size} bits.");
            }
        }

        [TestMethod]
        public void BuildLookupTable_ShouldMaskUpperBits()
        {
            const int size = 16;
            var table = CrcLookupTableBuilder.BuildLookupTable(size, 0x8005UL, false);
            ulong mask = ulong.MaxValue >> (64 - size);

            foreach (ulong value in table)
            {
                Assert.AreEqual(value, value & mask, $"Value {value:X} exceeds {size}-bit mask.");
            }
        }

        [TestMethod]
        public void BuildLookupTable_WhenPolynomialIsZero_ShouldStillGenerateTable()
        {
            var table = CrcLookupTableBuilder.BuildLookupTable(8, 0x00UL, false);

            Assert.IsTrue(Array.TrueForAll(table, v => v == 0), "All entries should be zero with zero polynomial.");
        }

        [TestMethod]
        public void BuildLookupTable_WhenReflected_ShouldProduceDifferentEntriesThanNonReflected()
        {
            const int size = 8;
            const ulong polynomial = 0x07;

            var reflected = CrcLookupTableBuilder.BuildLookupTable(size, polynomial, true);
            var nonReflected = CrcLookupTableBuilder.BuildLookupTable(size, polynomial, false);

            bool anyDifference = false;
            for (int i = 0; i < reflected.Length; i++)
            {
                if (reflected[i] != nonReflected[i])
                {
                    anyDifference = true;
                    break;
                }
            }

            Assert.IsTrue(anyDifference, "Reflected and non-reflected tables should differ.");
        }

        [TestMethod]
        public void BuildLookupTable_WhenSizeIs64_ShouldGenerateFullByteTable()
        {
            var table = CrcLookupTableBuilder.BuildLookupTable(64, 0x42F0E1EBA9EA3693UL, false);

            foreach (ulong value in table)
            {
                Assert.IsTrue(value <= ulong.MaxValue, "Value should not exceed 64 bits.");
            }
        }

        [TestMethod]
        [DataRow(int.MinValue)]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(65)]
        [DataRow(int.MaxValue)]
        public void BuildLookupTable_WhenSizeIsInvalid_ShouldThrow(int size)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = CrcLookupTableBuilder.BuildLookupTable(size, 0x1UL, false);
            });
        }

        [TestMethod]
        public void BuildLookupTable_WhenSizeIsOne_ShouldRespectBitMasking()
        {
            var table = CrcLookupTableBuilder.BuildLookupTable(1, 0x01, false);
            foreach (ulong entry in table)
            {
                Assert.IsTrue(entry == 0 || entry == 1, "Only 1-bit values should appear.");
            }
        }

        [TestMethod]
        [DataRow(1, 2)]     // Less than 8 bits: table size = 2
        [DataRow(2, 2)]
        [DataRow(4, 2)]
        [DataRow(7, 2)]
        [DataRow(8, 256)]   // 8 or more bits: table size = 256
        [DataRow(16, 256)]
        [DataRow(32, 256)]
        [DataRow(64, 256)]
        public void BuildLookupTable_WhenSizeIsValid_ShouldGenerateExpectedSize(int size, int expected)
        {
            var table = CrcLookupTableBuilder.BuildLookupTable(size, 0x1UL, false);

            Assert.AreEqual(expected, table.Length);
        }

        [TestMethod]
        public void BuildLookupTable_WithAndWithoutReflection_ShouldProduceDifferentTables()
        {
            var table1 = CrcLookupTableBuilder.BuildLookupTable(16, 0x1021UL, false);
            var table2 = CrcLookupTableBuilder.BuildLookupTable(16, 0x1021UL, true);

            CollectionAssert.AreNotEqual(table1, table2);
        }

        [TestMethod]
        public void BuildLookupTable_WithDifferentPolynomials_ShouldProduceDifferentTables()
        {
            var table1 = CrcLookupTableBuilder.BuildLookupTable(16, 0x1021UL, false);
            var table2 = CrcLookupTableBuilder.BuildLookupTable(16, 0x8005UL, false);

            CollectionAssert.AreNotEqual(table1, table2);
        }
    }
}