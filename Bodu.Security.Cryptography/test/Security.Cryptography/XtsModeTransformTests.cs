// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.cs" company="PlaceholderCompany">
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class XtsModeTransformTests
        : BlockCipherModeTests<XtsModeTransform>
    {
        protected override XtsModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new XtsModeTransform(cipher, iv);
    }
}
