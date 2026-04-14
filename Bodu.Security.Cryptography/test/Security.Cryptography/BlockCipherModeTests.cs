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

        /// <summary>
        /// Gets a value indicating whether this mode transform validates IV length against
        /// <see cref="IBlockCipher.BlockSize" /> at construction time. ECB mode typically returns
        /// <see langword="false" />.
        /// </summary>
        protected virtual bool ValidatesIvLengthAtConstruction => true;

        /// <summary>
        /// Gets a value indicating whether this mode transform guards against counter overflow
        /// or some other form of keystream reuse. Only CTR currently implements this; other modes
        /// return <see langword="false" />.
        /// </summary>
        protected virtual bool GuardsAgainstKeystreamReuse => false;

    }
}