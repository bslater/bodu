// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PaddingFactoryTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public class PaddingFactoryTests
    {
        /// <summary>
        /// Verifies that <see cref="PaddingFactory.Create" /> rejects an undefined
        /// <see cref="PaddingMode" /> value with a clean exception message.
        /// </summary>
        [TestMethod]
        public void Create_WhenPaddingModeIsInvalid_ShouldThrowWithCleanMessage()
        {
            var ex = Assert.ThrowsExactly<CryptographicException>(() => PaddingFactory.Create((PaddingMode)999));
            Assert.IsFalse(ex.Message.Contains("this."), "Exception message must not contain 'this.' artifact.");
        }
    }
}
