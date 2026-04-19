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
            using var transform = this.CreateTransform();
            transform.Dispose();

            byte[] buffer = new byte[transform.InputBlockSize];

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                transform.TransformBlock(buffer, 0, buffer.Length, buffer, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws
        /// <see cref="ArgumentNullException" /> when <c>inputBuffer</c> is <see langword="null" />.
        /// Regression guard for transforms that previously threw <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenInputBufferIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var transform = this.CreateTransform();
            byte[] output = new byte[transform.OutputBlockSize];

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = transform.TransformBlock(null!, 0, 0, output, 0);
            });
        }

        /// <summary>
        /// Verifies that <see cref="ICryptoTransform.TransformBlock" /> throws
        /// <see cref="ArgumentNullException" /> when <c>outputBuffer</c> is <see langword="null" />.
        /// Regression guard for transforms that previously threw <see cref="NullReferenceException" /> via <c>.AsSpan</c>.
        /// </summary>
        [TestMethod]
        public void TransformBlock_WhenOutputBufferIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var transform = this.CreateTransform();
            byte[] input = new byte[transform.InputBlockSize];

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = transform.TransformBlock(input, 0, input.Length, null!, 0);
            });
        }
    }
}
