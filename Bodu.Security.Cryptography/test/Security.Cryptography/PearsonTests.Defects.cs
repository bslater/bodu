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
        /// Verifies that the built-in Pearson permutation table contains every byte value
        /// from 0 to 255 exactly once, as required by the Pearson hashing algorithm.
        /// </summary>
        [TestMethod]
        public void DefaultPearsonTable_ShouldContainAllByteValuesExactlyOnce()
        {
            var hash = new Pearson { TableType = Pearson.PearsonTableType.Pearson };
            byte[] table = hash.Table;

            Assert.AreEqual(256, table.Length, "The Pearson table must be exactly 256 bytes long.");
            Assert.AreEqual(256, table.Distinct().Count(), "The Pearson table must contain 256 unique byte values.");
        }

        /// <summary>
        /// Verifies that accessing the <see cref="Pearson.Table"/> property after disposal throws an
        /// <see cref="ObjectDisposedException"/> whose <see cref="ObjectDisposedException.ObjectName"/>
        /// reports the <see cref="Pearson"/> type rather than a copy-paste artefact from another type.
        /// </summary>
        [TestMethod]
        public void Dispose_ThenAccessTable_ShouldThrowObjectDisposedExceptionWithPearsonTypeName()
        {
            var hash = new Pearson();
            hash.Dispose();

            try
            {
                _ = hash.Table;
                Assert.Fail("Expected ObjectDisposedException was not thrown.");
            }
            catch (ObjectDisposedException ex)
            {
                Assert.AreEqual(nameof(Pearson), ex.ObjectName);
            }
        }

        /// <summary>
        /// Verifies that the exception surfaced when <see cref="Pearson.PearsonTableType.UserDefined"/>
        /// is selected without a table assignment carries a clean, human-readable message that does
        /// not contain stray refactor artefacts such as a bare " t " token or a "this." prefix.
        /// </summary>
        [TestMethod]
        public void TableType_WhenSetToUserDefinedWithoutTable_ShouldThrowWithCleanMessage()
        {
            var hash = new Pearson
            {
                TableType = Pearson.PearsonTableType.UserDefined
            };

            try
            {
                hash.ComputeHash(new byte[] { 1, 2, 3 });
                Assert.Fail("Expected an exception to be thrown when hashing with an unconfigured UserDefined table.");
            }
            catch (Exception ex)
            {
                Assert.IsFalse(
                    ex.Message.Contains(" t "),
                    $"Exception message should not contain the bare token ' t '. Actual: {ex.Message}");
                Assert.IsFalse(
                    ex.Message.Contains("this."),
                    $"Exception message should not contain a 'this.' artefact. Actual: {ex.Message}");
            }
        }
    }
}
