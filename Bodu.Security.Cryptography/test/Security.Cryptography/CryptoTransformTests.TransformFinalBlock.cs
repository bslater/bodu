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
            using var transform = this.CreateTransform();
            transform.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                transform.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransform.TransformFinalBlock" /> throws
        /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
        /// Regression guard for transforms that previously threw <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
        /// </summary>
        [TestMethod]
        public void TransformFinalBlock_WhenInputBufferIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var transform = this.CreateTransform();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = transform.TransformFinalBlock(null!, 0, 0);
            });
        }
    }
}
