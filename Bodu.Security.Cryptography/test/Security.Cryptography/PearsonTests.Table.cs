using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public partial class PearsonTests
    {
        /// <summary>
        /// Verifies that a valid user-defined table with 256 unique bytes can be assigned successfully.
        /// </summary>
        [TestMethod]
        public void Table_WhenAssignedValidPermutation_ShouldUpdateSuccessfully()
        {
            var algorithm = new Pearson();
            byte[] validTable = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

            algorithm.Table = validTable;

            CollectionAssert.AreEqual(validTable, algorithm.Table);
            Assert.AreEqual(Pearson.PearsonTableType.UserDefined, algorithm.TableType);
        }

        /// <summary>
        /// Verifies that assigning a table with fewer than 256 bytes throws an exception.
        /// </summary>
        [TestMethod]
        public void Table_WhenAssignedTooShort_ShouldThrowExactly()
        {
            var algorithm = new Pearson();
            byte[] invalidTable = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                algorithm.Table = invalidTable;
            });
        }

        /// <summary>
        /// Verifies that assigning a table with duplicate bytes throws an exception.
        /// </summary>
        [TestMethod]
        public void Table_WhenAssignedWithDuplicates_ShouldThrowExactly()
        {
            var algorithm = new Pearson();
            byte[] duplicateTable = Enumerable.Repeat((byte)42, 256).ToArray(); // All elements are the same

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                algorithm.Table = duplicateTable;
            });
        }

        /// <summary>
        /// Verifies that modifying the table after hashing has started throws a CryptographicException.
        /// </summary>
        [TestMethod]
        public void Table_WhenModifiedAfterHashing_ShouldThrowExactly()
        {
            var algorithm = new Pearson();
            algorithm.TransformBlock(new byte[] { 1, 2, 3 }, 0, 3, null, 0);

            byte[] validTable = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
            {
                algorithm.Table = validTable;
            });
        }

        /// <summary>
        /// Verifies that the getter for the Table property returns a copy, not a reference.
        /// </summary>
        [TestMethod]
        public void Table_WhenAccessed_ShouldReturnCopy()
        {
            var algorithm = new Pearson();
            byte[] table = algorithm.Table;

            table[0] ^= 0xFF; // Modify the copy
            Assert.AreNotEqual(table[0], algorithm.Table[0], "Modifying the returned table should not affect internal state.");
        }

        /// <summary>
        /// Verifies that the built-in <see cref="Pearson.PearsonTableType.Pearson" /> permutation
        /// table contains every byte value 0..255 exactly once, as required by the Pearson hashing
        /// algorithm.
        /// </summary>
        [TestMethod]
        public void Table_WhenBuiltInPearsonSelected_ShouldBeA256ByteUniquePermutation()
        {
            using var algorithm = new Pearson { TableType = Pearson.PearsonTableType.Pearson };
            byte[] table = algorithm.Table;

            Assert.AreEqual(256, table.Length, "The Pearson table must be exactly 256 bytes long.");
            Assert.AreEqual(256, table.Distinct().Count(), "The Pearson table must contain 256 unique byte values.");
        }

        /// <summary>
        /// Verifies that reading <see cref="Pearson.Table" /> after the instance has been disposed
        /// throws <see cref="ObjectDisposedException" /> and reports the <see cref="Pearson" /> type
        /// name rather than a copy-paste artefact from another type.
        /// </summary>
        [TestMethod]
        public void Table_WhenAccessedAfterDispose_ShouldThrowObjectDisposedExceptionWithPearsonTypeName()
        {
            var algorithm = new Pearson();
            algorithm.Dispose();

            var ex = Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = algorithm.Table;
            });

            Assert.AreEqual(typeof(Pearson).FullName, ex.ObjectName,
                $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(Pearson).FullName}'.");
        }

        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void TableType_WhenInternallyDefined_ShouldBeValidPermutation(Pearson.PearsonTableType variant)
        {
            using var algorithm = CreateAlgorithm(variant);
            var table = algorithm.Table;

            Assert.AreEqual(256, table.Length,
                $"{variant}: table must have exactly 256 entries");

            var distinct = table.Distinct().Count();
            Assert.AreEqual(256, distinct,
                $"{variant}: table must be a valid permutation (found {distinct} unique values, expected 256)");
        }
    }
}