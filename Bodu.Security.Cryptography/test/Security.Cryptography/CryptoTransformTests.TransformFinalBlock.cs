// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class CryptoTransformTests<TAlgorithm>
    {
        /// <summary>
        /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws an <see cref="ObjectDisposedException" /> after the
        /// algorithm has been disposed.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_WhenDisposed_ShouldThrowExactly()
        {
            using var algorithm = this.CreateAlgorithm();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                algorithm.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> returns the input unchanged when passed a valid byte array.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_WhenCalledWithValidInput_ShouldReturnInputUnchanged()
        {
            byte[] input = CryptoTestUtilities.ByteSequence0To255;
            using var algorithm = this.CreateAlgorithm();
            byte[] result = algorithm.TransformFinalBlock(input, 0, input.Length);
            CollectionAssert.AreEqual(input, result);
        }
    }
}