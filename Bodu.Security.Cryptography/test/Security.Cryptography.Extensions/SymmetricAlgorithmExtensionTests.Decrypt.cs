// -----------------------------------------------------------------------
// <copyright file="BKDR.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Bodu.Security.Cryptography.Extensions
{
    public partial class SymmetricAlgorithmExtensionTests
    {
        [TestMethod]
        public void Decrypt_WhenArrayIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsException<ArgumentNullException>(() => algorithm.Decrypt(null!));
        }

        [TestMethod]
        public void Decrypt_WhenBufferSizeIsZero_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var input = new MemoryStream();
            using var output = new MemoryStream();

            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                algorithm.Decrypt(input, output, 0));
        }

        [TestMethod]
        public void Decrypt_WhenCalledWithValidArray_ShouldReturnDecrypted()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("abc");
            byte[] cipherText = algorithm.Encrypt(plainText);

            byte[] result = algorithm.Decrypt(cipherText);

            CollectionAssert.AreEqual(plainText, result);
        }

        [TestMethod]
        public void Decrypt_WhenCountIsNegative_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                {
                    algorithm.Decrypt(data, 0, -1);
                });
        }

        [TestMethod]
        public void Decrypt_WhenOffsetAndCountValid_ShouldReturnDecrypted()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("abcde");
            byte[] cipherText = algorithm.Encrypt(plainText);

            byte[] result = algorithm.Decrypt(cipherText, 0, cipherText.Length);
            CollectionAssert.AreEqual(plainText, result);
        }

        [TestMethod]
        public void Decrypt_WhenOffsetIsInvalid_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = new byte[10];
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => algorithm.Decrypt(data, 20));
        }

        [TestMethod]
        public void Decrypt_WhenOffsetIsNegative_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                {
                    algorithm.Decrypt(data, -1, 2);
                });
        }

        [TestMethod]
        public void Decrypt_WhenOffsetPlusCountExceedsLength_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            byte[] data = Encoding.UTF8.GetBytes("data");
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                {
                    algorithm.Decrypt(data, 2, 5);
                });
        }

        [TestMethod]
        public void Decrypt_WhenOffsetValid_ShouldReturnDecrypted()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("abc");
            byte[] cipherText = algorithm.Encrypt(plainText);

            byte[] result = algorithm.Decrypt(cipherText, 0);

            CollectionAssert.AreEqual(plainText, result);
        }

        [TestMethod]
        public void Decrypt_WhenSourceStreamIsNull_ShouldThrow()
        {
            using var algorithm = CreateAlgorithm();
            using var output = new MemoryStream();

            Assert.ThrowsException<ArgumentNullException>(() =>
                algorithm.Decrypt(null!, output, 1024));
        }

        [TestMethod]
        public void Decrypt_WhenStreamIsUsed_ShouldReturnDecrypted()
        {
            using var algorithm = CreateAlgorithm();
            byte[] plainText = Encoding.UTF8.GetBytes("stream-data");
            byte[] cipherText = algorithm.Encrypt(plainText);

            using var input = new MemoryStream(cipherText);
            using var output = new MemoryStream();

            algorithm.Decrypt(input, output);
            byte[] result = output.ToArray();

            CollectionAssert.AreEqual(plainText, result);
        }
    }
}