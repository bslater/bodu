// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.ProcessAssociatedData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class AeadBlockCipherModeTests<TTransform>
    {
        /// <summary>
        /// Verifies that <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" /> throws
        /// <see cref="InvalidOperationException" /> when called more than once on the same instance.
        /// Associated data must be supplied exactly once before any encryption or decryption.
        /// </summary>
        [TestMethod]
        public void ProcessAssociatedData_WhenCalledTwice_ShouldThrowInvalidOperationException()
        {
            var transform = MakeTransform();
            transform.ProcessAssociatedData(new byte[] { 1, 2, 3 });

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                transform.ProcessAssociatedData(new byte[] { 4, 5, 6 }));
        }
    }
}