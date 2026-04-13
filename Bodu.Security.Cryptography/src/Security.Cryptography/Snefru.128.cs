namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Computes the hash for the input data using the <c>Snefru-128</c> hash algorithm. This variant applies a symmetric, non-keyed block
    /// transformation using a fixed sequence of S-box and bit rotation rounds. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Snefru128" /> computes a 128-bit (16-byte) hash using 8 rounds of transformation over a 512-bit working buffer. The
    /// algorithm uses a 4-word (32-bit unsigned integer) internal state and processes input data in fixed 64-byte blocks.
    /// </para>
    /// <para>
    /// Each round consists of an S-box substitution stage followed by a word-wise circular rotation phase. The S-box logic uses precomputed
    /// lookup tables to inject non-linearity, while rotations ensure data diffusion across the buffer. After all rounds, the result is
    /// XORed back into the internal state.
    /// </para>
    /// <para>
    /// Input blocks are padded using double-length padding (2 × block size), with the total bit length encoded in the final 8 bytes. The
    /// final state is serialized in big-endian byte order to produce the resulting hash.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> suitable for cryptographic applications such as password hashing, digital
    /// signatures, or secure data integrity checks.</note>
    /// </remarks>
    public sealed class Snefru128 : Snefru<Snefru128>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Snefru128" /> class using a fixed 128-bit output size.
        /// </summary>
        public Snefru128()
            : base(128)
        {
        }
    }
}