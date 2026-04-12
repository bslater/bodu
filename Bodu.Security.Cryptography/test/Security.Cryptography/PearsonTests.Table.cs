using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public partial class PearsonTests
        : Security.Cryptography.HashAlgorithmTests<PearsonTests, Pearson, SingleTestVariant>
    {
        /// <summary>
        /// Verifies that a valid user-defined table with 256 unique bytes can be assigned successfully.
        /// </summary>
        [TestMethod]
        public void Table_WhenAssignedValidPermutation_ShouldUpdateSuccessfully()
        {
            var hash = new Pearson();
            byte[] validTable = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

            hash.Table = validTable;

            CollectionAssert.AreEqual(validTable, hash.Table);
            Assert.AreEqual(Pearson.PearsonTableType.UserDefined, hash.TableType);
        }

        /// <summary>
        /// Verifies that assigning a table with fewer than 256 bytes throws an exception.
        /// </summary>
        [TestMethod]
        public void Table_WhenAssignedTooShort_ShouldThrowExactly()
        {
            var hash = new Pearson();
            byte[] invalidTable = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                hash.Table = invalidTable;
            });
        }

        /// <summary>
        /// Verifies that assigning a table with duplicate bytes throws an exception.
        /// </summary>
        [TestMethod]
        public void Table_WhenAssignedWithDuplicates_ShouldThrowExactly()
        {
            var hash = new Pearson();
            byte[] duplicateTable = Enumerable.Repeat((byte)42, 256).ToArray(); // All elements are the same

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                hash.Table = duplicateTable;
            });
        }

        /// <summary>
        /// Verifies that modifying the table after hashing has started throws a CryptographicException.
        /// </summary>
        [TestMethod]
        public void Table_WhenModifiedAfterHashing_ShouldThrowExactly()
        {
            var hash = new Pearson();
            hash.TransformBlock(new byte[] { 1, 2, 3 }, 0, 3, null, 0);

            byte[] validTable = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
            {
                hash.Table = validTable;
            });
        }

        /// <summary>
        /// Verifies that the getter for the Table property returns a copy, not a reference.
        /// </summary>
        [TestMethod]
        public void Table_WhenAccessed_ShouldReturnCopy()
        {
            var hash = new Pearson();
            byte[] table = hash.Table;

            table[0] ^= 0xFF; // Modify the copy
            Assert.AreNotEqual(table[0], hash.Table[0], "Modifying the returned table should not affect internal state.");
        }
    }
}