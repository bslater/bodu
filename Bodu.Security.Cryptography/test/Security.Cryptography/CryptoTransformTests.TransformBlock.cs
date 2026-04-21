// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class CryptoTransformTests<TCryptoTransform>
    {
        /// <summary>
        /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an <see cref="ObjectDisposedException" /> after the
        /// transform is disposed.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenDisposed_ShouldThrowExactly()
        {
            using var transform = CreateAlgorithm();
            byte[] buffer = new byte[transform.InputBlockSize];

            transform.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                transform.TransformBlock(buffer, 0, buffer.Length, buffer, 0);
            });
        }
    }
}
