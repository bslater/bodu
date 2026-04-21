namespace Bodu.Security.Cryptography
{
    public sealed partial class CfbModeTransformTests
    {
        /// <summary>
        /// Verifies that CFB encryption computes <c>Cᵢ = Pᵢ ⊕ E(IVᵢ)</c> with <c>IV₀</c> equal to the caller-supplied IV and
        /// <c>IVᵢ₊₁ = Cᵢ</c> for subsequent blocks. Uses an identity cipher (xorMask 0x00) so that <c>E(x) = x</c> and the
        /// expected output reduces to successive XOR chains.
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncrypting_ShouldApplyCfbChaining()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
            var output = new byte[plaintext.Length];

            transform.Transform(plaintext, output, encrypt: true);

            // With an identity cipher: C_0 = P_0 ⊕ E(IV) = P_0 ⊕ IV; then C_1 = P_1 ⊕ E(C_0) = P_1 ⊕ C_0.
            var expectedBlock1 = plaintext[..ExpectedBlockSize].Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
            var expectedBlock2 = plaintext[ExpectedBlockSize..].Zip(expectedBlock1, (a, b) => (byte)(a ^ b)).ToArray();

            CollectionAssert.AreEqual(expectedBlock1, output[..ExpectedBlockSize].ToArray(), "First CFB block did not match expected chaining output.");
            CollectionAssert.AreEqual(expectedBlock2, output[ExpectedBlockSize..].ToArray(), "Second CFB block did not match expected chaining output.");
        }

        /// <summary>
        /// Verifies that CFB decryption inverts <see cref="Transform_WhenEncrypting_ShouldApplyCfbChaining" />, recovering
        /// the original plaintext for both blocks under the same key and IV.
        /// </summary>
        [TestMethod]
        public void Transform_WhenDecrypting_ShouldApplyCfbUnchaining()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();

            var encrypt = CreateTransform(cipher, (byte[])iv.Clone());
            var decrypt = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
            var ciphertext = new byte[plaintext.Length];
            var recovered = new byte[plaintext.Length];

            encrypt.Transform(plaintext, ciphertext, encrypt: true);
            decrypt.Transform(ciphertext, recovered, encrypt: false);

            CollectionAssert.AreEqual(plaintext, recovered, "CFB decryption did not recover the original plaintext.");
        }

        /// <summary>
        /// Verifies that encrypting in CFB mode does not mutate the caller-supplied initialisation vector.
        /// </summary>
        [TestMethod]
        public void Transform_WhenEncrypting_ShouldNotMutateIv()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x7E, ExpectedBlockSize).ToArray();
            var ivCopy = (byte[])iv.Clone();
            var transform = CreateTransform(cipher, iv);

            var plaintext = new byte[ExpectedBlockSize];
            var output = new byte[ExpectedBlockSize];

            transform.Transform(plaintext, output, encrypt: true);

            CollectionAssert.AreEqual(ivCopy, iv, "CFB must not mutate the caller-supplied IV array.");
        }

        /// <summary>
        /// Verifies that CFB uses the cipher's encrypt primitive on both encryption and decryption paths. This is a defining
        /// property of CFB (it turns the block cipher into a self-synchronising stream cipher).
        /// </summary>
        [TestMethod]
        public void Transform_WhenDecrypting_ShouldUseCipherEncryptPrimitive()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xAA);
            var iv = Enumerable.Repeat((byte)0x01, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, iv);

            var input = new byte[ExpectedBlockSize * 2];
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: false);

            Assert.AreEqual(2, cipher.EncryptBlockCount, "CFB decryption must use the cipher's encrypt primitive for every block.");
            Assert.AreEqual(0, cipher.DecryptBlockCount, "CFB decryption must never call the cipher's decrypt primitive.");
        }

        /// <summary>
        /// Verifies that transforming a single full block in CFB mode produces the expected <c>P ⊕ E(IV)</c> output.
        /// </summary>
        [TestMethod]
        public void Transform_WithSingleBlock_ShouldEncryptCorrectly()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0x55, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
            var output = new byte[ExpectedBlockSize];

            transform.Transform(plaintext, output, encrypt: true);

            var expected = plaintext.Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
            CollectionAssert.AreEqual(expected, output);
        }
    }
}
