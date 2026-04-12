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
        /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws an <see cref="ObjectDisposedException" /> after the
        /// algorithm is disposed.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenDisposed_ShouldThrowExactly()
        {
            using var algorithm = this.CreateAlgorithm();
            algorithm.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                algorithm.TransformBlock(Array.Empty<byte>(), 0, 0, null, 0);
            });
        }
    }
}