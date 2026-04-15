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

        /// <summary>
        /// Creates a concrete mode transform wrapping the supplied cipher and initialisation vector. Implementations whose mode
        /// does not use an initialisation vector (for example, ECB) may ignore <paramref name="iv" />.
        /// </summary>
        protected abstract TMode CreateTransform(IBlockCipher cipher, byte[] iv);

        /// <summary>
        /// Gets a value indicating whether this mode transform accepts a caller-supplied initialisation vector (or equivalent
        /// seed such as CTR's initial counter) and validates its length against <see cref="IBlockCipher.BlockSize" /> at
        /// construction time. ECB has no IV and therefore overrides this to <see langword="false" />.
        /// </summary>
        protected virtual bool UsesInitializationVector => true;

        /// <summary>
        /// Gets the constructor parameter name used for the initialisation vector (or equivalent seed). Most modes name this
        /// parameter <c>iv</c>; CTR names it <c>initialCounter</c>. The value is used by constructor-validation tests to assert
        /// that thrown exceptions expose the correct <see cref="ArgumentException.ParamName" />.
        /// </summary>
        protected virtual string IvParameterName => "iv";

        /// <summary>
        /// Gets a value indicating whether this mode alters the relationship between plaintext and ciphertext across block
        /// positions (for example via chaining, feedback, or a counter). ECB returns <see langword="false" /> because each block
        /// is transformed independently and identical plaintext blocks always yield identical ciphertext blocks.
        /// </summary>
        protected virtual bool UsesChaining => true;

        /// <summary>
        /// Gets a value indicating whether this mode transform guards against counter overflow or some other form of keystream
        /// reuse. Only CTR currently implements this; other modes return <see langword="false" />.
        /// </summary>
        protected virtual bool GuardsAgainstKeystreamReuse => false;
    }
}
