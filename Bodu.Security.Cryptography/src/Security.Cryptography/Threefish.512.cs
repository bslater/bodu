namespace Bodu.Security.Cryptography
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;

    /// <summary>
    /// Provides a managed implementation of the <c>Threefish-512</c> tweakable symmetric block cipher, which operates on 512-bit
    /// (64-byte) blocks using a 512-bit key and a 128-bit tweak. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Threefish is the tweakable block cipher underlying the Skein hash function. This variant supports a variety of cipher block modes
    /// (CBC, CFB, OFB, CTR) via the <see cref="Threefish.BlockMode" /> property, and is suitable for scenarios such as disk encryption
    /// or format-preserving encryption where a tweak is useful.
    /// </para>
    /// <para>For other block sizes, see <see cref="Threefish256" /> and <see cref="Threefish1024" />.</para>
    /// </remarks>
    /// <seealso href="../guides/cryptography/threefish-512.html">Using Threefish-512 (guide with full encrypt / decrypt examples)</seealso>
    /// <seealso href="../guides/cryptography/encryption-basics.html">Encryption basics</seealso>
    /// <seealso href="../guides/cryptography/cipher-modes.html">Cipher block modes</seealso>
    /// <seealso href="../guides/cryptography/padding.html">Padding</seealso>
    public sealed class Threefish512
        : Threefish
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="Threefish512" /> class using a 512-bit block size, 512-bit key, and 128-bit tweak.
        /// </summary>
        public Threefish512()
            : base(512, 128) { }

        /// <summary>
        /// Creates a new <see cref="Threefish512" /> instance with default parameters.
        /// </summary>
        /// <returns>A new <see cref="Threefish512" /> instance.</returns>
        /// <remarks>
        /// The key, initialisation vector, and tweak are generated on demand the first time they are accessed unless assigned explicitly
        /// via <see cref="SymmetricAlgorithm.Key" />, <see cref="SymmetricAlgorithm.IV" />, or <see cref="TweakableSymmetricAlgorithm.Tweak" />.
        /// </remarks>
        public new static Threefish512 Create()
        {
            return new Threefish512();
        }

        /// <inheritdoc />
        protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
            new Threefish512Cipher(key, tweak);
    }
}