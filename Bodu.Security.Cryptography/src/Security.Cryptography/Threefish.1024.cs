namespace Bodu.Security.Cryptography
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;

    /// <summary>
    /// Provides a managed implementation of the <c>Threefish-1024</c> tweakable symmetric block cipher, which operates on 1024-bit
    /// (128-byte) blocks using a 1024-bit key and a 128-bit tweak. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Threefish is the tweakable block cipher underlying the Skein hash function. This variant supports a variety of cipher block modes
    /// (CBC, CFB, OFB, CTR) via the <see cref="Threefish.BlockMode" /> property, and is suitable for scenarios such as disk encryption
    /// or format-preserving encryption where a tweak is useful.
    /// </para>
    /// <para>For other block sizes, see <see cref="Threefish256" /> and <see cref="Threefish512" />.</para>
    /// </remarks>
    public sealed class Threefish1024
        : Threefish
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Threefish1024" /> class using a 1024-bit block size, 1024-bit key, and 128-bit tweak.
        /// </summary>
        public Threefish1024()
            : base(1024, 128) { }

        /// <summary>
        /// Creates a new instance of the <see cref="Threefish1024" /> class with the default configuration.
        /// </summary>
        /// <returns>A new instance of <see cref="Threefish1024" />.</returns>
        /// <remarks>
        /// The newly created algorithm instance will have its key, initialization vector (IV), and tweak generated automatically as needed
        /// upon first use.
        /// </remarks>
        public new static Threefish1024 Create()
        {
            return new Threefish1024();
        }

        /// <inheritdoc />
        protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
            new Threefish1024Cipher(key, tweak);
    }
}