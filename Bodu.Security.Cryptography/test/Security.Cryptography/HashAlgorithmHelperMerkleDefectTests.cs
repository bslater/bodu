namespace Bodu.Security.Cryptography
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HashAlgorithmHelperMerkleDefectTests
    {
        // Known SHA-256 of the UTF-8 bytes of "abc":
        // BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD
        private static readonly byte[] ExpectedAbcSha256 = new byte[]
        {
            0xBA, 0x78, 0x16, 0xBF, 0x8F, 0x01, 0xCF, 0xEA,
            0x41, 0x41, 0x40, 0xDE, 0x5D, 0xAE, 0x22, 0x23,
            0xB0, 0x03, 0x61, 0xA3, 0x96, 0x17, 0x7A, 0x9C,
            0xB4, 0x10, 0xFF, 0x61, 0xF2, 0x00, 0x15, 0xAD,
        };

        private static DelegateHashAlgorithmFactory<SHA256> CreateSha256Factory() =>
            new DelegateHashAlgorithmFactory<SHA256>(SHA256.Create);

        [TestMethod]
        public void HashData_WithSpan_ShouldReturnExpectedSha256()
        {
            // Defect C: HashData(ReadOnlySpan<byte>) should use TryComputeHash without
            // allocating an intermediate byte[] via input.ToArray().
            var factory = CreateSha256Factory();
            byte[] input = Encoding.UTF8.GetBytes("abc");

            byte[] actual = HashAlgorithmHelper.HashData(factory, (ReadOnlySpan<byte>)input);

            CollectionAssert.AreEqual(ExpectedAbcSha256, actual);
        }

        [TestMethod]
        public void HashData_WithStream_ShouldReturnExpectedSha256()
        {
            // Defect A: the pooled plaintext buffer inside AppendDataFromStreamInternal must
            // be returned with clearArray: true. This test is a behavioural smoke-test to ensure
            // the clearing path still yields a correct hash.
            var factory = CreateSha256Factory();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

            byte[] actual = HashAlgorithmHelper.HashData(factory, stream);

            CollectionAssert.AreEqual(ExpectedAbcSha256, actual);
        }

        [TestMethod]
        public void TryHashData_WithAdequateDestination_ShouldFillAndReturnTrue()
        {
            // Defect D: TryHashData should delegate directly to HashAlgorithm.TryComputeHash.
            var factory = CreateSha256Factory();
            byte[] input = Encoding.UTF8.GetBytes("abc");
            Span<byte> destination = stackalloc byte[32];

            bool ok = HashAlgorithmHelper.TryHashData(factory, input, destination, out int bytesWritten);

            Assert.IsTrue(ok);
            Assert.AreEqual(32, bytesWritten);
            CollectionAssert.AreEqual(ExpectedAbcSha256, destination.ToArray());
        }

        [TestMethod]
        public void TryHashData_WithSmallDestination_ShouldReturnFalse()
        {
            // Defect D: destination smaller than the hash size must return false rather than throwing.
            var factory = CreateSha256Factory();
            byte[] input = Encoding.UTF8.GetBytes("abc");
            Span<byte> destination = stackalloc byte[16];

            bool ok = HashAlgorithmHelper.TryHashData(factory, input, destination, out int bytesWritten);

            Assert.IsFalse(ok);
        }

        [TestMethod]
        public void MerkleTreeHash_ComputeHash_WithNullByteArray_ShouldThrowArgumentNullException()
        {
            // Defect E: the single-argument ComputeHash(byte[]) overload documented
            // ArgumentNullException but previously silently accepted null via ReadOnlySpan<byte>.
            using var merkle = new MerkleTreeHash(SHA256.Create);

            Assert.ThrowsExactly<ArgumentNullException>(() => merkle.ComputeHash((byte[])null!));
        }

        [TestMethod]
        public void MerkleTreeHash_ComputeHashRange_WithNullByteArray_ShouldThrowArgumentNullException()
        {
            // Defect E: three-argument overload must also guard against a null array.
            using var merkle = new MerkleTreeHash(SHA256.Create);

            Assert.ThrowsExactly<ArgumentNullException>(() => merkle.ComputeHash((byte[])null!, 0, 0));
        }

        [TestMethod]
        public async Task HashAlgorithmHelper_HashData_Async_ShouldClearPooledPlaintextBuffer()
        {
            // Defect A: AppendDataFromStreamInternal rents a pooled buffer that may contain
            // plaintext (e.g. a password) read from the caller's stream. The fix is to return
            // the buffer with clearArray: true so the next pool consumer cannot observe the
            // plaintext. This invariant cannot be directly asserted from outside the method,
            // so this test is a behavioural smoke-test guard verifying that the clearing path
            // still computes the correct hash end-to-end.
            var factory = CreateSha256Factory();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

            byte[] actual = await HashAlgorithmHelper.HashDataAsync(factory, stream);

            CollectionAssert.AreEqual(ExpectedAbcSha256, actual);
        }
    }
}
