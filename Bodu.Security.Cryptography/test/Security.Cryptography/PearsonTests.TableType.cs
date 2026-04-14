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
        /// Provides test data for each predefined <see cref="Pearson.PearsonTableType" /> (excluding UserDefined).
        /// </summary>
        public static IEnumerable<object[]> TableTypeTestData =>
            Enum.GetValues<Pearson.PearsonTableType>()
                .Where(t => t != Pearson.PearsonTableType.UserDefined)
                .Select(t => new object[] { t });

        /// <summary>
        /// Verifies that setting <see cref="Pearson.TableType" /> to a valid predefined type allows computing a hash without error, and
        /// that the hash output is the correct length for the current <see cref="HashAlgorithm.HashSize" />.
        /// </summary>
        /// <param name="type">The table type to test.</param>
        [TestMethod]
        [DynamicData(nameof(TableTypeTestData))]
        public void TableType_Set_WhenValid_ShouldProduceExpectedHash(Pearson.PearsonTableType type)
        {
            using var hash = new Pearson
            {
                TableType = type,
                HashSize = 64
            };

            byte[] input = Encoding.ASCII.GetBytes("Test input");

            byte[] result = hash.ComputeHash(input);

            Assert.IsNotNull(result, $"Result should not be null for table type {type}.");
            Assert.AreEqual(hash.HashSize / 8, result.Length, $"Hash length should match expected size for type {type}.");
            Assert.IsTrue(result.Any(b => b != 0), $"Hash result should not be all zeros for type {type}.");
        }

        [TestMethod]
        public void TableType_Get_WhenDefault_ShouldReturnPearson()
        {
            using var hash = new Pearson();

            var type = hash.TableType;

            Assert.AreEqual(Pearson.PearsonTableType.Pearson, type, "Default TableType should be Pearson.");
        }

        [TestMethod]
        public void TableType_Set_WhenNotStarted_ShouldLoadExpectedPresetTable()
        {
            using var hash = new Pearson();
            var lazy = typeof(Pearson)
                .GetField("AESSBoxTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .GetValue(null) as Lazy<byte[]>;
            var expected = lazy?.Value ?? Array.Empty<byte>();
            hash.TableType = Pearson.PearsonTableType.AESSBox;
            var table = hash.Table;

            Assert.AreEqual(256, table.Length, "Table length should be 256.");
            CollectionAssert.AreEqual(expected, table, "Table contents should match AESSBox preset.");
        }

        [TestMethod]
        public void TableType_Set_ToUserDefined_ShouldRequireManualTableAssignment()
        {
            using var hash = new Pearson();

            hash.TableType = Pearson.PearsonTableType.UserDefined;

            Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
            {
                hash.ComputeHash(new byte[] { 1, 2, 3 });
            }, "Using UserDefined table type without a valid table should throw.");
        }

        [TestMethod]
        public void TableType_Set_WhenHashingStarted_ShouldThrowExactly()
        {
            using var hash = new Pearson();
            hash.TransformBlock(new byte[] { 1, 2, 3 }, 0, 3, null, 0);

            Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
            {
                hash.TableType = Pearson.PearsonTableType.SHA256Constants;
            });
        }

        [TestMethod]
        public void TableType_Set_WhenDisposed_ShouldThrowExactly()
        {
            var hash = new Pearson();
            hash.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                hash.TableType = Pearson.PearsonTableType.CRC32HighByte;
            });
        }

        [TestMethod]
        public void TableType_Get_WhenDisposed_ShouldThrowExactly()
        {
            var hash = new Pearson();
            hash.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                var _ = hash.TableType;
            });
        }

        /// <summary>
        /// Verifies that hashing with <see cref="Pearson.PearsonTableType.UserDefined" /> selected
        /// but no table assigned throws an exception whose message does not contain any
        /// refactor artefacts ("this." prefixes or stray " t " tokens).
        /// </summary>
        [TestMethod]
        public void TableType_WhenSetToUserDefinedWithoutTable_ShouldThrowWithCleanMessage()
        {
            using var hash = new Pearson
            {
                TableType = Pearson.PearsonTableType.UserDefined
            };

            var ex = Assert.ThrowsException<Exception>(() => hash.ComputeHash(new byte[] { 1, 2, 3 }));

            Assert.IsFalse(
                ex.Message.Contains(" t "),
                $"Exception message should not contain the bare token ' t '. Actual: {ex.Message}");
            Assert.IsFalse(
                ex.Message.Contains("this."),
                $"Exception message should not contain a 'this.' artefact. Actual: {ex.Message}");
        }

    }
}