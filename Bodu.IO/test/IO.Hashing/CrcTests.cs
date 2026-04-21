// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing
{
    using System;
    using System.Text;

    /// <summary>
    /// Contains unit tests for the <see cref="Crc" /> non-cryptographic hash algorithm. Extended by partial files
    /// covering the catalogue parity vectors and resume-from-hash semantics.
    /// </summary>
    [TestClass]
    public partial class CrcTests
    {
        private static readonly byte[] CheckInput = Encoding.ASCII.GetBytes("123456789");

        /// <summary>
        /// Verifies that <see cref="Crc.HashLengthInBytes" /> matches the width derived from the configured
        /// <see cref="CrcStandard.Size" /> (rounded up to the nearest byte).
        /// </summary>
        [TestMethod]
        public void HashLengthInBytes_WhenConstructedWithStandard_ShouldBeWidthRoundedUp()
        {
            Assert.AreEqual(4, new Crc(CrcStandard.CRC32_ISOHDLC).HashLengthInBytes);
            Assert.AreEqual(2, new Crc(CrcStandard.CRC16_ARC).HashLengthInBytes);
            Assert.AreEqual(1, new Crc(CrcStandard.CRC8_SMBUS).HashLengthInBytes);
            Assert.AreEqual(8, new Crc(CrcStandard.CRC64_ECMA182).HashLengthInBytes);
        }

        /// <summary>
        /// Verifies that calling the default constructor selects the CRC-32/ISO-HDLC standard.
        /// </summary>
        [TestMethod]
        public void Ctor_Default_ShouldUseCRC32_ISOHDLC()
        {
            Crc crc = new();
            Assert.AreEqual(CrcStandard.CRC32_ISOHDLC.Name, crc.CrcStandard.Name);
        }

        /// <summary>
        /// Verifies that passing a <see langword="null" /> <see cref="CrcStandard" /> to the constructor throws
        /// <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void Ctor_WhenStandardIsNull_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => _ = new Crc(null!));
        }

        /// <summary>
        /// Verifies that <see cref="Crc.ComputeHash(ReadOnlySpan{byte})" /> on the reference input <c>"123456789"</c>
        /// under CRC-32/ISO-HDLC produces the documented check value <c>0xCBF43926</c>.
        /// </summary>
        [TestMethod]
        public void ComputeHash_WhenInputIs123456789_UnderCRC32_ISOHDLC_ShouldMatchPublishedCheck()
        {
            Crc crc = new(CrcStandard.CRC32_ISOHDLC);
            byte[] hash = crc.ComputeHash(CheckInput);

            // CRC-32/ISO-HDLC stores little-endian; 0xCBF43926 → bytes [26, 39, F4, CB].
            CollectionAssert.AreEqual(new byte[] { 0x26, 0x39, 0xF4, 0xCB }, hash);
        }

        /// <summary>
        /// Verifies that splitting input across multiple <see cref="Crc.Append(ReadOnlySpan{byte})" /> calls produces
        /// the same digest as hashing it all at once.
        /// </summary>
        [TestMethod]
        public void Append_WhenCalledInChunks_ShouldMatchSingleAppend()
        {
            byte[] data = CheckInput;

            Crc whole = new(CrcStandard.CRC32_ISOHDLC);
            whole.Append(data);

            Crc chunked = new(CrcStandard.CRC32_ISOHDLC);
            chunked.Append(data.AsSpan(0, 3));
            chunked.Append(data.AsSpan(3, 4));
            chunked.Append(data.AsSpan(7, data.Length - 7));

            CollectionAssert.AreEqual(whole.GetCurrentHash(), chunked.GetCurrentHash());
        }

        /// <summary>
        /// Verifies that <see cref="Crc.GetCurrentHash()" /> is non-destructive and returns the same digest when
        /// invoked repeatedly against the same accumulator state.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenCalledRepeatedly_ShouldReturnSameDigest()
        {
            Crc crc = new(CrcStandard.CRC32_ISOHDLC);
            crc.Append(CheckInput);

            byte[] first = crc.GetCurrentHash();
            byte[] second = crc.GetCurrentHash();

            CollectionAssert.AreEqual(first, second);
        }

        /// <summary>
        /// Verifies that <see cref="Crc.Reset" /> restores the initial accumulator so that hashing a new input yields
        /// the same digest as a freshly constructed instance would.
        /// </summary>
        [TestMethod]
        public void Reset_AfterAppend_ShouldRestoreInitialState()
        {
            Crc crc = new(CrcStandard.CRC32_ISOHDLC);
            crc.Append(CheckInput);
            crc.Reset();

            byte[] empty = crc.GetCurrentHash();
            byte[] baseline = new Crc(CrcStandard.CRC32_ISOHDLC).GetCurrentHash();

            CollectionAssert.AreEqual(baseline, empty);
        }

        /// <summary>
        /// Verifies that <see cref="Crc.GetHashAndReset()" /> returns the finalised digest and leaves the instance
        /// reset for subsequent hashing.
        /// </summary>
        [TestMethod]
        public void GetHashAndReset_AfterAppend_ShouldReturnHashAndResetState()
        {
            Crc crc = new(CrcStandard.CRC32_ISOHDLC);
            crc.Append(CheckInput);

            byte[] first = crc.GetHashAndReset();
            CollectionAssert.AreEqual(new byte[] { 0x26, 0x39, 0xF4, 0xCB }, first);

            byte[] second = crc.GetCurrentHash();
            byte[] baseline = new Crc(CrcStandard.CRC32_ISOHDLC).GetCurrentHash();
            CollectionAssert.AreEqual(baseline, second);
        }

        /// <summary>
        /// Verifies that <see cref="Crc.ComputeHashFrom(ReadOnlySpan{byte}, ReadOnlySpan{byte})" /> continues a prior
        /// hash by undoing finalisation and hashing additional data, yielding the same digest as a single-shot hash
        /// over the combined input.
        /// </summary>
        [TestMethod]
        public void ComputeHashFrom_WhenResumedWithAdditionalInput_ShouldMatchSingleShotCombinedHash()
        {
            byte[] first = new byte[] { 0x31, 0x32, 0x33, 0x34 };  // "1234"
            byte[] rest = new byte[] { 0x35, 0x36, 0x37, 0x38, 0x39 };  // "56789"

            byte[] firstHash = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(first);

            Crc resumer = new(CrcStandard.CRC32_ISOHDLC);
            byte[] resumed = resumer.ComputeHashFrom(firstHash, rest);

            byte[] combined = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(CheckInput);
            CollectionAssert.AreEqual(combined, resumed);
        }

        /// <summary>
        /// Verifies that two <see cref="Crc" /> instances configured with identical <see cref="CrcStandard" /> values
        /// report equal via <see cref="Crc.Equals(object?)" /> and produce matching hash codes.
        /// </summary>
        [TestMethod]
        public void Equals_WhenStandardsMatch_ShouldReturnTrueAndEqualHashCode()
        {
            Crc a = new(CrcStandard.CRC32_ISOHDLC);
            Crc b = new(CrcStandard.CRC32_ISOHDLC);

            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }

        /// <summary>
        /// Verifies that two <see cref="Crc" /> instances configured with different <see cref="CrcStandard" /> values
        /// report unequal via <see cref="Crc.Equals(object?)" />.
        /// </summary>
        [TestMethod]
        public void Equals_WhenStandardsDiffer_ShouldReturnFalse()
        {
            Crc a = new(CrcStandard.CRC32_ISOHDLC);
            Crc b = new(CrcStandard.CRC32_ISCSI);

            Assert.IsFalse(a.Equals(b));
        }
    }
}
