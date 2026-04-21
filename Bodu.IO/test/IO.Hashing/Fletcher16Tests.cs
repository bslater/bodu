// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher16Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing
{
    using static FletcherTestHelpers;

    /// <summary>
    /// Contains unit tests for the <see cref="Fletcher16" /> hash algorithm.
    /// </summary>
    [TestClass]
    public class Fletcher16Tests
    {
        private static readonly string[] IncrementalExpected = new[]
        {
            "0000", "0000", "0101", "0403", "0A06", "140A", "230F", "3815",
            "541C", "7824", "A52D", "DC37", "1F42", "6D4E", "C85B", "3269",
        };

        /// <summary>
        /// Verifies that <see cref="Fletcher16.HashLengthInBytes" /> reports the 16-bit output width as two bytes.
        /// </summary>
        [TestMethod]
        public void HashLengthInBytes_WhenConstructed_ShouldBeTwo()
        {
            Fletcher16 hash = new();
            Assert.AreEqual(2, hash.HashLengthInBytes);
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher16.AlgorithmName" /> returns <c>Fletcher-16</c>.
        /// </summary>
        [TestMethod]
        public void AlgorithmName_WhenConstructed_ShouldBeFletcher16()
        {
            Fletcher16 hash = new();
            Assert.AreEqual("Fletcher-16", hash.AlgorithmName);
        }

        /// <summary>
        /// Verifies that an empty input produces the all-zero Fletcher-16 checksum.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsEmpty_ShouldReturnAllZero()
        {
            Fletcher16 hash = new();
            Assert.AreEqual("0000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that incremental 0..N byte inputs produce the documented Fletcher-16 known-answer sequence.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenGivenIncrementingInput_ShouldMatchKnownAnswers()
        {
            for (int length = 0; length < IncrementalExpected.Length; length++)
            {
                Fletcher16 hash = new();
                hash.Append(IncrementingBytes(length));
                string actual = ToHex(hash.GetCurrentHash());
                Assert.AreEqual(
                    IncrementalExpected[length],
                    actual,
                    $"Mismatch at length {length}.");
            }
        }

        /// <summary>
        /// Verifies that Fletcher-16 of the ASCII text <c>"ABC"</c> matches the documented known-answer value.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsABC_ShouldMatchKnownAnswer()
        {
            Fletcher16 hash = new();
            hash.Append(Utf8("ABC"));
            Assert.AreEqual("8BC6", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that Fletcher-16 of the ASCII text <c>"The quick brown fox jumps over the lazy dog"</c> matches the
        /// documented known-answer value.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsQuickBrownFox_ShouldMatchKnownAnswer()
        {
            Fletcher16 hash = new();
            hash.Append(Utf8(QuickBrownFox));
            Assert.AreEqual("FEE8", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that sixteen zero bytes produce an all-zero Fletcher-16 checksum.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenInputIsSixteenZeros_ShouldReturnAllZero()
        {
            Fletcher16 hash = new();
            hash.Append(new byte[16]);
            Assert.AreEqual("0000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that splitting an input across multiple <see cref="Fletcher16.Append(byte[])" /> calls produces the
        /// same digest as passing it in a single call.
        /// </summary>
        [TestMethod]
        public void Append_WhenCalledInChunks_ShouldMatchSingleAppend()
        {
            byte[] data = Utf8(QuickBrownFox);

            Fletcher16 whole = new();
            whole.Append(data);

            Fletcher16 chunked = new();
            chunked.Append(data.AsSpan(0, 10));
            chunked.Append(data.AsSpan(10, 15));
            chunked.Append(data.AsSpan(25, data.Length - 25));

            CollectionAssert.AreEqual(whole.GetCurrentHash(), chunked.GetCurrentHash());
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher16.GetCurrentHash()" /> does not mutate internal state and can be invoked
        /// repeatedly with identical results.
        /// </summary>
        [TestMethod]
        public void GetCurrentHash_WhenCalledRepeatedly_ShouldReturnSameDigest()
        {
            Fletcher16 hash = new();
            hash.Append(Utf8("ABC"));

            byte[] first = hash.GetCurrentHash();
            byte[] second = hash.GetCurrentHash();

            CollectionAssert.AreEqual(first, second);
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher16.Reset" /> restores the algorithm to the same state as a newly constructed
        /// instance.
        /// </summary>
        [TestMethod]
        public void Reset_AfterAppend_ShouldReturnToInitialState()
        {
            Fletcher16 hash = new();
            hash.Append(Utf8(QuickBrownFox));
            hash.Reset();

            Assert.AreEqual("0000", ToHex(hash.GetCurrentHash()));
        }

        /// <summary>
        /// Verifies that <see cref="Fletcher16.GetHashAndReset()" /> returns the finalised digest and leaves the
        /// algorithm reset for subsequent use.
        /// </summary>
        [TestMethod]
        public void GetHashAndReset_AfterAppend_ShouldReturnHashAndResetState()
        {
            Fletcher16 hash = new();
            hash.Append(Utf8("ABC"));

            byte[] first = hash.GetHashAndReset();
            Assert.AreEqual("8BC6", ToHex(first));
            Assert.AreEqual("0000", ToHex(hash.GetCurrentHash()));
        }
    }
}
