namespace Bodu.Security.Cryptography
{
    public sealed partial class CbcModeTransformTests
    {
        [TestMethod]
        public void Transform_WhenDecrypting_ShouldApplyCBCUnchaining()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00); // Identity
            var iv = Enumerable.Repeat((byte)0x10, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());

            // Encrypted block1 = plaintext1 ^ IV, encrypted block2 = plaintext2 ^ encrypted block1
            var plaintext1 = Enumerable.Repeat((byte)0x33, ExpectedBlockSize).ToArray();
            var plaintext2 = Enumerable.Repeat((byte)0x44, ExpectedBlockSize).ToArray();
            var block1 = plaintext1.Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
            var block2 = plaintext2.Zip(block1, (a, b) => (byte)(a ^ b)).ToArray();

            var ciphertext = block1.Concat(block2).ToArray();
            var output = new byte[ciphertext.Length];

            transform.Transform(ciphertext, output, encrypt: false);

            CollectionAssert.AreEqual(plaintext1, output[..ExpectedBlockSize].ToArray(), "Decryption of first block failed.");
            CollectionAssert.AreEqual(plaintext2, output[ExpectedBlockSize..].ToArray(), "Decryption of second block failed.");
        }

        [TestMethod]
        public void Transform_WhenEncrypting_ShouldApplyCBCChaining()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00); // Identity cipher
            var iv = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)(i << 4)).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)i).ToArray();
            var output = new byte[plaintext.Length];

            transform.Transform(plaintext, output, encrypt: true);

            var expectedBlock1 = plaintext[..ExpectedBlockSize].Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
            var expectedBlock2 = plaintext[ExpectedBlockSize..].Zip(expectedBlock1, (a, b) => (byte)(a ^ b)).ToArray();

            CollectionAssert.AreEqual(expectedBlock1, output[..ExpectedBlockSize].ToArray());
            CollectionAssert.AreEqual(expectedBlock2, output[ExpectedBlockSize..].ToArray());
        }

        [TestMethod]
        public void Transform_WhenEncrypting_ShouldNotMutateIV()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0xAB, ExpectedBlockSize).ToArray();
            var ivCopy = (byte[])iv.Clone();
            var transform = CreateTransform(cipher, iv);

            var plaintext = Enumerable.Repeat((byte)0xCD, ExpectedBlockSize).ToArray();
            var output = new byte[ExpectedBlockSize];

            transform.Transform(plaintext, output, encrypt: true);

            CollectionAssert.AreEqual(ivCopy, iv, "IV must not be mutated after encryption.");
        }

        [TestMethod]
        public void Transform_WithEmptyInput_ShouldNotThrow()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var transform = CreateTransform(cipher, new byte[ExpectedBlockSize]);
            var input = Array.Empty<byte>();
            var output = Array.Empty<byte>();

            transform.Transform(input, output, encrypt: true);

            Assert.AreEqual(0, output.Length);
        }

        [TestMethod]
        public void Transform_WithSingleBlock_ShouldEncryptCorrectly()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0x11, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = Enumerable.Repeat((byte)0x22, ExpectedBlockSize).ToArray();
            var output = new byte[ExpectedBlockSize];

            transform.Transform(plaintext, output, encrypt: true);

            var expected = plaintext.Zip(iv, (a, b) => (byte)(a ^ b)).ToArray();
            CollectionAssert.AreEqual(expected, output);
        }
    }
}