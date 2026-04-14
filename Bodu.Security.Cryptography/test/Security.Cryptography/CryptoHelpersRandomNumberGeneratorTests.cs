using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public class CryptoHelpersRandomNumberGeneratorTests
    {
        [TestMethod]
        public void TryFillWithRandomNonZeroBytes_WhenSuccessful_ShouldReturnTrueAndFillBuffer()
        {
            // Repeated to knock down flakiness; also guards against the NETSTANDARD2_0
            // branch regressing to never returning true on success.
            for (int attempt = 0; attempt < 100; attempt++)
            {
                byte[] buffer = new byte[32];
                bool result = CryptoHelpers.TryFillWithRandomNonZeroBytes(buffer.AsSpan());

                Assert.IsTrue(result, "TryFillWithRandomNonZeroBytes should return true on success.");

                for (int i = 0; i < buffer.Length; i++)
                {
                    Assert.AreNotEqual((byte)0, buffer[i], "No byte in the filled buffer should be zero.");
                }
            }
        }

        [TestMethod]
        public void TryFillWithRandomNonZeroBytes_BufferBytes_ShouldNotContainZero()
        {
            byte[] buffer = new byte[64];
            bool result = CryptoHelpers.TryFillWithRandomNonZeroBytes(buffer.AsSpan());

            Assert.IsTrue(result);
            for (int i = 0; i < buffer.Length; i++)
            {
                Assert.AreNotEqual((byte)0, buffer[i]);
            }
        }

        [TestMethod]
        public void FillWithRandomBytesExcluding_ShouldNeverContainExcludedValue()
        {
            const byte forbidden = 0xAA;

            for (int attempt = 0; attempt < 100; attempt++)
            {
                byte[] buffer = new byte[1024];
                CryptoHelpers.FillWithRandomBytesExcluding(forbidden, buffer.AsSpan());

                for (int i = 0; i < buffer.Length; i++)
                {
                    Assert.AreNotEqual(forbidden, buffer[i], "Buffer must not contain the forbidden value.");
                }
            }
        }

        [TestMethod]
        public void FillWithRandomBytesExcluding_ShouldNotLeaveTempBufferOnHeap()
        {
            // We cannot directly observe the internal temp buffer being zeroed
            // (defect B), so we assert the behavioural guarantee that successive
            // calls produce fresh randomness rather than any cached content. If
            // two consecutive fills are byte-identical for a reasonable length,
            // something is wrong (probability of accidental collision is ~2^-256).
            byte[] first = new byte[32];
            byte[] second = new byte[32];

            CryptoHelpers.FillWithRandomBytesExcluding(0x00, first.AsSpan());
            CryptoHelpers.FillWithRandomBytesExcluding(0x00, second.AsSpan());

            bool identical = true;
            for (int i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    identical = false;
                    break;
                }
            }

            Assert.IsTrue(!identical, "Successive calls should produce fresh random output, not cached bytes.");
        }

        [TestMethod]
        public void FillWithRandomBytesExcluding_WithHighForbiddenFrequency_ShouldTerminate()
        {
            // Guards against regression to full-buffer refill on any match
            // (defect C). With a very small buffer and forbidden = 0x00 the
            // targeted per-byte replacement must terminate quickly.
            var task = Task.Run(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    byte[] buffer = new byte[2];
                    CryptoHelpers.FillWithRandomBytesExcluding(0x00, buffer.AsSpan());

                    Assert.AreNotEqual((byte)0, buffer[0]);
                    Assert.AreNotEqual((byte)0, buffer[1]);
                }
            });

            bool completed = task.Wait(TimeSpan.FromSeconds(1));
            Assert.IsTrue(completed, "FillWithRandomBytesExcluding should terminate in bounded time.");
        }
    }
}
