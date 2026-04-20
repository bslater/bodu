// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing
{
    public partial class FletcherTests
    {
        private static readonly string[] Fletcher32IncrementalExpected = new[]
        {
            "00000000", "00000000", "01000100", "02020102", "05020402", "09080406",
            "0E080906", "1714090C", "1E14100C", "2E281014", "37281914", "5046191E",
            "5B46241E", "7F70242A", "8C70312A", "BDA83138",
        };

        /// <summary>
        /// Verifies that <see cref="Fletcher32.HashLengthInBytes" /> reports the 32-bit output width as four bytes.
        /// </summary>
        [TestMethod]
        public void HashLengthInBytes_WhenFletcher32_ShouldBeFour()
        {
            using Fletcher32 hash = new();
            Assert.AreEqual(4, hash.HashLengthInBytes);
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher32.AlgorithmName" /> returns <c>Fletcher-32</c>.
        /// </summary>
        [TestMethod]
        public void AlgorithmName_WhenFletcher32_ShouldBeFletcher32()
        {
            using Fletcher32 hash = new();
            Assert.AreEqual("Fletcher-32", hash.AlgorithmName);
        }

        /// <summary>
        /// Verifies that an empty input produces the all-zero Fletcher-32 checksum.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsEmpty_ShouldReturnAllZero()
        {
            using Fletcher32 hash = new();
            Assert.AreEqual("00000000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that incremental 0..N byte inputs produce the documented Fletcher-32 known-answer sequence.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenGivenIncrementingInput_ShouldMatchKnownAnswers()
        {
            for (int length = 0; length < Fletcher32IncrementalExpected.Length; length++)
            {
                using Fletcher32 hash = new();
                hash.Append(IncrementingBytes(length));
                string actual = ToHex(hash.GetCurrentHash());
                Assert.AreEqual(
                    Fletcher32IncrementalExpected[length],
                    actual,
                    $"Mismatch at length {length}.");
            }
        }

        /// <summary>
        /// Verifies that Fletcher-32 of the ASCII text <c>"ABC"</c> matches the documented known-answer value.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsABC_ShouldMatchKnownAnswer()
        {
            using Fletcher32 hash = new();
            hash.Append(Utf8("ABC"));
            Assert.AreEqual("84C54284", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that Fletcher-32 of the ASCII text <c>"The quick brown fox jumps over the lazy dog"</c> matches the
        /// documented known-answer value.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsQuickBrownFox_ShouldMatchKnownAnswer()
        {
            using Fletcher32 hash = new();
            hash.Append(Utf8(QuickBrownFox));
            Assert.AreEqual("53CD5B8D", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that sixteen zero bytes produce an all-zero Fletcher-32 checksum.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsSixteenZeros_ShouldReturnAllZero()
        {
            using Fletcher32 hash = new();
            hash.Append(new byte[16]);
            Assert.AreEqual("00000000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that splitting an input across multiple <see cref="Fletcher32.Append(byte[])" /> calls produces the
        /// same digest as passing it in a single call.
        /// </summary>
        [TestMethod]
        public void Append_WhenCalledInChunks_ShouldMatchSingleAppend()
        {
            byte[] data = Utf8(QuickBrownFox);

            using Fletcher32 whole = new();
            whole.Append(data);

            using Fletcher32 chunked = new();
            chunked.Append(data.AsSpan(0, 10));
            chunked.Append(data.AsSpan(10, 15));
            chunked.Append(data.AsSpan(25, data.Length - 25));

            CollectionAssert.AreEqual(whole.GetCurrentHash(), chunked.GetCurrentHash());
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher32.GetCurrentHash()" /> does not mutate internal state and can be invoked
        /// repeatedly with identical results.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenCalledRepeatedly_ShouldReturnSameDigest()
        {
            using Fletcher32 hash = new();
            hash.Append(Utf8("ABC"));

            byte[] first = hash.GetCurrentHash();
            byte[] second = hash.GetCurrentHash();

            CollectionAssert.AreEqual(first, second);
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher32.Reset" /> restores the algorithm to the same state as a newly constructed
        /// instance.
        /// </summary>
        [TestMethod]
        public void Reset_AfterAppend_ShouldReturnToInitialState()
        {
            using Fletcher32 hash = new();
            hash.Append(Utf8(QuickBrownFox));
            hash.Reset();

            Assert.AreEqual("00000000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher32.GetHashAndReset()" /> returns the finalised digest and leaves the
        /// algorithm reset for subsequent use.
        /// </summary>
        [TestMethod]
        public void GetHashAndReset_AfterAppend_ShouldReturnHashAndResetState()
        {
            using Fletcher32 hash = new();
            hash.Append(Utf8("ABC"));

            byte[] first = hash.GetHashAndReset();
            Assert.AreEqual("84C54284", ToHex(first));
            Assert.AreEqual("00000000", ToHex(hash.GetCurrentHash()));
        }
    }
}
