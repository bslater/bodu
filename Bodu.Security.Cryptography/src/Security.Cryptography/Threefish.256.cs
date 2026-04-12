using System;
using System.Linq;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides a managed implementation of the <c>Threefish-256</c> symmetric block cipher algorithm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class derives from <see cref="Threefish" /> and configures the algorithm for the <c>Threefish-256</c> variant, which operates
    /// on 256-bit blocks using a 256-bit key and a 128-bit tweak.
    /// </para>
    /// <para>
    /// It supports a variety of cipher block modes (e.g., CBC, CFB, OFB, CTR) via the <see cref="Threefish.BlockMode" /> property,
    /// replacing the standard <see cref="SymmetricAlgorithm.Mode" /> property to allow for custom or non-standard modes.
    /// </para>
    /// <para>
    /// This implementation is designed to integrate with the .NET cryptographic framework and supports tweakable encryption for enhanced
    /// security scenarios such as disk encryption or format-preserving encryption.
    /// </para>
    /// <note type="important">This class is sealed and specific to the <c>Threefish-256</c> variant. Use <see cref="Threefish512" /> or
    /// <see cref="Threefish1024" /> for other block sizes.</note>
    /// </remarks>
    public sealed class Threefish256
        : Threefish
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Threefish256" /> class using a 256-bit block size, 256-bit key, and 128-bit tweak.
        /// </summary>
        public Threefish256()
            : base(256, 128) { }

        /// <summary>
        /// Creates a new instance of the <see cref="Threefish256" /> class with the default configuration.
        /// </summary>
        /// <returns>A new instance of <see cref="Threefish256" />.</returns>
        /// <remarks>
        /// The newly created algorithm instance will have its key, initialization vector (IV), and tweak generated automatically as needed
        /// upon first use.
        /// </remarks>
        public new static Threefish256 Create()
        {
            return new Threefish256();
        }

        /// <inheritdoc />
        protected override IBlockCipher CreateCipher(byte[] key, byte[] tweak) =>
            new Threefish256Cipher(key, tweak);
    }
}