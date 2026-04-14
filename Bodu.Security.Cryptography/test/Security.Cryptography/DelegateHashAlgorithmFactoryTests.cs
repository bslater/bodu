// -----------------------------------------------------------------------
// <copyright file="DelegateHashAlgorithmFactoryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public class DelegateHashAlgorithmFactoryTests
    {
        /// <summary>
        /// Verifies that <see cref="DelegateHashAlgorithmFactory{T}.Create" /> invokes the supplied builder delegate and returns a
        /// fresh, non-null instance on each call.
        /// </summary>
        [TestMethod]
        public void Create_WhenBuilderProvided_ShouldReturnInstanceFromDelegate()
        {
            var factory = new DelegateHashAlgorithmFactory<MD5>(MD5.Create);

            using var first = factory.Create();
            using var second = factory.Create();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreNotSame(first, second);
        }

        /// <summary>
        /// Verifies that constructing a <see cref="DelegateHashAlgorithmFactory{T}" /> with a <see langword="null" /> builder
        /// delegate throws <see cref="ArgumentNullException" /> with the expected parameter name.
        /// </summary>
        [TestMethod]
        public void Ctor_WhenBuilderIsNull_ShouldThrowArgumentNullException()
        {
            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new DelegateHashAlgorithmFactory<MD5>((Func<MD5>)null!);
            });

            Assert.AreEqual("builder", ex.ParamName);
        }
    }
}
