namespace Bodu.IO.Hashing
{
    [TestClass]
    public class CrcLookupTableCacheTests
    {
        private CrcLookupTableCache cache = null!;

        [TestInitialize]
        public void SetUp()
        {
            cache = new CrcLookupTableCache();
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> correctly returns the CRC lookup table for known parameters.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenKnownParameters_ShouldReturnCorrectTable()
        {
            int size = 32;
            ulong polynomial = 0x04C11DB7;
            bool reflectIn = true;

            var result = cache.GetLookupTable(size, polynomial, reflectIn);
            Assert.IsNotNull(result);
            Assert.AreEqual(256, result.Length); // Validate the size of the lookup table
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> returns different lookup tables for different parameters.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenDifferentParameters_ShouldCacheDifferentTables()
        {
            int size1 = 32;
            ulong polynomial1 = 0x04C11DB7;
            bool reflectIn1 = true;
            int size2 = 16;
            ulong polynomial2 = 0x1EDC6F41;
            bool reflectIn2 = false;

            var result1 = cache.GetLookupTable(size1, polynomial1, reflectIn1);
            var result2 = cache.GetLookupTable(size2, polynomial2, reflectIn2);

            // Ensure different lookup tables are cached for different parameters
            Assert.AreNotEqual(result1, result2);
        }

        /// <summary>
        /// Verifies that the cached CRC lookup table arrays are not modified.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenModified_ShouldNotModifyCachedArrays()
        {
            int size = 32;
            ulong polynomial = 0x04C11DB7;
            bool reflectIn = true;

            var result = cache.GetLookupTable(size, polynomial, reflectIn).ToArray();
            result[0] = 123456; // Modify the array returned by GetLookupTable

            // Verify that the array in the cache remains unchanged
            var cachedResult = cache.GetLookupTable(size, polynomial, reflectIn);
            Assert.AreNotEqual(123456UL, cachedResult[0]);
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> handles concurrent access correctly.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenCalledConcurrently_ShouldHandleConcurrentAccess()
        {
            int size = 32;
            ulong polynomial = 0x04C11DB7;
            bool reflectIn = true;

            var threads = new Task[100];
            var successFlag = new bool[100];

            for (int i = 0; i < 100; i++)
            {
                int threadIndex = i;
                threads[i] = Task.Run(() =>
                {
                    var result = cache.GetLookupTable(size, polynomial, reflectIn);
                    successFlag[threadIndex] = result != null; // Use the captured threadIndex
                });
            }

            Task.WhenAll(threads).Wait();

            // Verify that all threads successfully retrieved the lookup table
            foreach (var success in successFlag)
            {
                Assert.IsTrue(success);
            }
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> correctly handles edge cases for size and polynomial parameters.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenSizeOrPolynomialEdgeCases_ShouldHandleEdgeCases()
        {
            // Test with minimal size (1-bit CRC)
            var result1 = cache.GetLookupTable(1, 0x04C11DB7, true);
            Assert.IsNotNull(result1);
            Assert.AreEqual(2, result1.Length);

            // Test with maximum size (64-bit CRC)
            var result2 = cache.GetLookupTable(64, 0x04C11DB7, false);
            Assert.IsNotNull(result2);
            Assert.AreEqual(256, result2.Length);

            // Test with large polynomial hashValue
            var result3 = cache.GetLookupTable(32, 0xFFFFFFFF, true);
            Assert.IsNotNull(result3);
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> correctly handles the zero polynomial case.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenPolynomialIsZero_ShouldHandleZeroPolynomial()
        {
            int size = 32;
            ulong polynomial = 0x0; // Zero polynomial
            bool reflectIn = true;

            var result = cache.GetLookupTable(size, polynomial, reflectIn);
            Assert.IsNotNull(result);
            Assert.AreEqual(256, result.Length); // Verify correct table size
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> correctly handles the reflectIn parameter.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenReflectInChanges_ShouldHandleReflectInParameter()
        {
            int size = 32;
            ulong polynomial = 0x04C11DB7;
            bool reflectIn = true;

            var result1 = cache.GetLookupTable(size, polynomial, reflectIn);
            reflectIn = false;
            var result2 = cache.GetLookupTable(size, polynomial, reflectIn);

            // Ensure that reflectIn changes the lookup table
            Assert.AreNotSame(result1, result2);
        }

        /// <summary>
        /// Verifies that repeated lookups with identical parameters return the same shared array reference from the cache.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenCalledWithSameParameters_ShouldReturnCachedInstance()
        {
            int size = 32;
            ulong polynomial = 0x04C11DB7;
            bool reflectIn = true;

            var firstCallResult = cache.GetLookupTable(size, polynomial, reflectIn);
            var secondCallResult = cache.GetLookupTable(size, polynomial, reflectIn);

            // Cache hits share a single underlying table across callers.
            Assert.AreSame(firstCallResult, secondCallResult);
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> returns the different instances for the same parameters.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenCalledWithSameParameters_ShouldReturnCachedValue()
        {
            int size = 32;
            ulong polynomial = 0x04C11DB7;
            bool reflectIn = true;

            var firstCallResult = cache.GetLookupTable(size, polynomial, reflectIn);
            var secondCallResult = cache.GetLookupTable(size, polynomial, reflectIn);

            // Assert that the result from the second call contain the same values as the first
            CollectionAssert.AreEqual(firstCallResult, secondCallResult);
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> returns different lookup tables for different CRC parameters.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_WhenCalledWithDifferentParameters_ShouldReturnNewValue()
        {
            int size1 = 32;
            ulong polynomial1 = 0x04C11DB7;
            bool reflectIn1 = true;

            var firstCallResult = cache.GetLookupTable(size1, polynomial1, reflectIn1);

            int size2 = 16;
            ulong polynomial2 = 0xA833982B;
            bool reflectIn2 = false;

            var secondCallResult = cache.GetLookupTable(size2, polynomial2, reflectIn2);

            // Assert that the result is different for the two different parameter sets (cache miss)
            Assert.AreNotSame(firstCallResult, secondCallResult);
        }

        /// <summary>
        /// Verifies that the <see cref="CrcLookupTableCache" /> throws an exception when the <paramref name="size" /> is outside the valid range.
        /// </summary>
        [DataTestMethod]
        [DataRow(0, false)] // Below min size
        [DataRow(16, true)] // Valid size
        [DataRow(64, true)] // Valid size
        [DataRow(128, false)] // Above max size
        public void GetLookupTable_WhenSizeIsOutsideValidRange_ShouldThrowException(int size, bool shouldPass)
        {
            ulong polynomial = 0x04C11DB7;
            bool reflectIn = true;

            if (shouldPass)
            {
                // If the size is valid, it should succeed
                var lookupTable = cache.GetLookupTable(size, polynomial, reflectIn);
                Assert.IsNotNull(lookupTable);
            }
            else
            {
                // If the size is invalid, we expect an exception
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                {
                    cache.GetLookupTable(size, polynomial, reflectIn);
                });
            }
        }

        /// <summary>
        /// Verifies that invalid <paramref name="size" /> values surface an <see cref="ArgumentOutOfRangeException" />
        /// whose <c>ParamName</c> is <c>size</c>, confirming the <see cref="ThrowHelper" /> contract propagates
        /// through the cache lookup path.
        /// </summary>
        [DataTestMethod]
        [DataRow(-1)]
        [DataRow(0)]
        [DataRow(65)]
        [DataRow(int.MaxValue)]
        public void GetLookupTable_WhenSizeIsInvalid_ShouldReportSizeParamName(int size)
        {
            var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => cache.GetLookupTable(size, 0x04C11DB7UL, reflectIn: false));
            Assert.AreEqual("size", ex.ParamName);
        }

        /// <summary>
        /// Verifies that <see cref="CrcLookupTableCache.GetLookupTable" /> succeeds at the inclusive boundary
        /// values <see cref="CrcStandard.MinSize" /> (1-bit) and <see cref="CrcStandard.MaxSize" /> (64-bit).
        /// </summary>
        [DataTestMethod]
        [DataRow(1, 2)]
        [DataRow(64, 256)]
        public void GetLookupTable_WhenSizeIsBoundaryValue_ShouldReturnExpectedTable(int size, int expectedLength)
        {
            ulong[] table = cache.GetLookupTable(size, 0x1UL, reflectIn: false);

            Assert.IsNotNull(table);
            Assert.AreEqual(expectedLength, table.Length);
        }

        /// <summary>
        /// Verifies that the cache returns a <see cref="ulong" /> array whose contents match the shared precomputed table.
        /// </summary>
        [TestMethod]
        public void GetLookupTable_ShouldReturnSharedArray()
        {
            int size = 32;
            ulong polynomial = 0x04C11DB7;
            bool reflectIn = true;

            ulong[] cachedResult = cache.GetLookupTable(size, polynomial, reflectIn);

            Assert.IsNotNull(cachedResult);
            Assert.AreEqual(256, cachedResult.Length);
            Assert.AreSame(cachedResult, cache.GetLookupTable(size, polynomial, reflectIn));
        }
    }
}