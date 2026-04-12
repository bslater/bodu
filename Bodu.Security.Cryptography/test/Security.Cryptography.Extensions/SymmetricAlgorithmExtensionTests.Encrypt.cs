using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Bodu.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class SymmetricAlgorithmExtensionTests
    {
        [TestMethod]
        public void Encrypt_WhenArrayIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                algorithm.Encrypt(null!);
            });
        }

        [TestMethod]
        public void Encrypt_WhenBufferSizeIsNegative_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var input = new MemoryStream();
            using var output = new MemoryStream();

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                algorithm.Encrypt(input, output, -128));
        }

        [TestMethod]
        public void Encrypt_WhenCalledWithValidArray_ShouldReturnEncrypted()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("encrypt");
            byte[] cipherText = algorithm.Encrypt(plainText);
            byte[] decrypted = algorithm.Decrypt(cipherText);

            CollectionAssert.AreEqual(plainText, decrypted);
        }

        [TestMethod]
        public void Encrypt_WhenCountIsNegative_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                {
                    algorithm.Encrypt(data, 0, -1);
                });
        }

        [TestMethod]
        public void Encrypt_WhenOffsetIsNegative_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                {
                    algorithm.Encrypt(data, -1, 2);
                });
        }

        [TestMethod]
        public void Encrypt_WhenOffsetPlusCountExceedsLength_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                {
                    algorithm.Encrypt(data, 2, 5);
                });
        }

        [TestMethod]
        public void Encrypt_WhenTargetStreamIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var input = new MemoryStream();

            Assert.ThrowsException<ArgumentNullException>(() =>
                algorithm.Encrypt(input, null!, 1024));
        }
    }
}