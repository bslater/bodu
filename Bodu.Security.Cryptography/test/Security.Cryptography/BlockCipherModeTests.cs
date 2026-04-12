using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Security.Cryptography;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public abstract partial class BlockCipherModeTests<TMode>
        where TMode : IBlockCipherModeTransform
    {
        protected const int ExpectedBlockSize = 8;

        protected abstract TMode CreateTransform(IBlockCipher cipher, byte[] iv);
    }
}