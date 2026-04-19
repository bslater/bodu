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
        /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws an <see cref="ObjectDisposedException" /> after the
        /// transform has been disposed.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_WhenDisposed_ShouldThrowExactly()
        {
            using var transform = this.CreateAlgorithm();
            transform.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            });
        }
    }
}
