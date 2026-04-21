// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher64Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing
{
    using static FletcherTestHelpers;

    /// <summary>
    /// Contains unit tests for the <see cref="Fletcher64" /> hash algorithm.
    /// </summary>
    [TestClass]
    public class Fletcher64Tests
    {
        private static readonly string[] IncrementalExpected = new[]
        {
            "0000000000000000", "0000000000000000", "0000010000000100", "0002010000020100",
            "0302010003020100", "0604020403020104", "0604070403020604", "060A070403080604",
            "0D0A07040A080604", "17120D100A08060C", "171216100A080F0C", "171C16100A120F0C",
            "221C161015120F0C", "372E252815120F18", "372E322815121C18", "373C322815201C18",
            "463C322824201C18", "6A5C4E5024201C28", "6A5C5F5024202D28", "6A6E5F5024322D28",
            "7D6E5F5037322D28", "B4A08C8C37322D3C", "B4A0A18C3732423C", "B4B6A18C3748423C",
            "CBB6A18C4E48423C", "19FFE3E04E484254", "19FFFCE04E485B54", "1919FDE04E625B54",
            "3419FDE069625B54", "9E7B585169625B70", "9E7B755169627870", "9E99755169807870",
        };

        /// <summary>
        /// Verifies that <see cref="Fletcher64.HashLengthInBytes" /> reports the 64-bit output width as eight bytes.
        /// </summary>
        [TestMethod]
        public void HashLengthInBytes_WhenConstructed_ShouldBeEight()
        {
            Fletcher64 hash = new();
            Assert.AreEqual(8, hash.HashLengthInBytes);
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher64.AlgorithmName" /> returns <c>Fletcher-64</c>.
        /// </summary>
        [TestMethod]
        public void AlgorithmName_WhenConstructed_ShouldBeFletcher64()
        {
            Fletcher64 hash = new();
            Assert.AreEqual("Fletcher-64", hash.AlgorithmName);
        }

        /// <summary>
        /// Verifies that an empty input produces the all-zero Fletcher-64 checksum.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsEmpty_ShouldReturnAllZero()
        {
            Fletcher64 hash = new();
            Assert.AreEqual("0000000000000000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that incremental 0..N byte inputs produce the documented Fletcher-64 known-answer sequence.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenGivenIncrementingInput_ShouldMatchKnownAnswers()
        {
            for (int length = 0; length < IncrementalExpected.Length; length++)
            {
                Fletcher64 hash = new();
                hash.Append(IncrementingBytes(length));
                string actual = ToHex(hash.GetCurrentHash());
                Assert.AreEqual(
                    IncrementalExpected[length],
                    actual,
                    $"Mismatch at length {length}.");
            }
        }

        /// <summary>
        /// Verifies that Fletcher-64 of the ASCII text <c>"ABC"</c> matches the documented known-answer value.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsABC_ShouldMatchKnownAnswer()
        {
            Fletcher64 hash = new();
            hash.Append(Utf8("ABC"));
            Assert.AreEqual("0043424100434241", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that Fletcher-64 of the ASCII text <c>"The quick brown fox jumps over the lazy dog"</c> matches the
        /// documented known-answer value.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsQuickBrownFox_ShouldMatchKnownAnswer()
        {
            Fletcher64 hash = new();
            hash.Append(Utf8(QuickBrownFox));
            Assert.AreEqual("7CA0BCD01F153C78", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that sixteen zero bytes produce an all-zero Fletcher-64 checksum.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsSixteenZeros_ShouldReturnAllZero()
        {
            Fletcher64 hash = new();
            hash.Append(new byte[16]);
            Assert.AreEqual("0000000000000000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that splitting an input across multiple <see cref="Fletcher64.Append(byte[])" /> calls produces the
        /// same digest as passing it in a single call.
        /// </summary>
        [TestMethod]
        public void Append_WhenCalledInChunks_ShouldMatchSingleAppend()
        {
            byte[] data = Utf8(QuickBrownFox);

            Fletcher64 whole = new();
            whole.Append(data);

            Fletcher64 chunked = new();
            chunked.Append(data.AsSpan(0, 12));
            chunked.Append(data.AsSpan(12, 16));
            chunked.Append(data.AsSpan(28, data.Length - 28));

            CollectionAssert.AreEqual(whole.GetCurrentHash(), chunked.GetCurrentHash());
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher64.GetCurrentHash()" /> does not mutate internal state and can be invoked
        /// repeatedly with identical results.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenCalledRepeatedly_ShouldReturnSameDigest()
        {
            Fletcher64 hash = new();
            hash.Append(Utf8("ABC"));

            byte[] first = hash.GetCurrentHash();
            byte[] second = hash.GetCurrentHash();

            CollectionAssert.AreEqual(first, second);
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher64.Reset" /> restores the algorithm to the same state as a newly constructed
        /// instance.
        /// </summary>
        [TestMethod]
        public void Reset_AfterAppend_ShouldReturnToInitialState()
        {
            Fletcher64 hash = new();
            hash.Append(Utf8(QuickBrownFox));
            hash.Reset();

            Assert.AreEqual("0000000000000000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher64.GetHashAndReset()" /> returns the finalised digest and leaves the
        /// algorithm reset for subsequent use.
        /// </summary>
        [TestMethod]
        public void GetHashAndReset_AfterAppend_ShouldReturnHashAndResetState()
        {
            Fletcher64 hash = new();
            hash.Append(Utf8("ABC"));

            byte[] first = hash.GetHashAndReset();
            Assert.AreEqual("0043424100434241", ToHex(first));
            Assert.AreEqual("0000000000000000", ToHex(hash.GetCurrentHash()));
        }
    }
}
