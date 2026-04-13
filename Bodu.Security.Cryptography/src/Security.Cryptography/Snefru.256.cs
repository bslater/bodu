namespace Bodu.Security.Cryptography
{
    using System;

    /// <summary>
    /// Computes a 256-bit (32-byte) hash using the <c>Snefru</c> hash algorithm by Ralph Merkle. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Snefru256" /> maintains an 8-word internal state and absorbs input in 32-byte blocks into a 512-bit working buffer,
    /// applying 8 rounds of S-box substitution and word rotation per block. On finalisation the state is XOR-folded from the permuted
    /// buffer and serialised in big-endian byte order. See <see cref="Snefru{T}" /> for shared background.
    /// </para>
    /// <note type="important">Snefru is considered broken and <b>not</b> suitable for password hashing, digital signatures, or secure
    /// data integrity checks.</note>
    /// </remarks>
    public sealed class Snefru256
        : Snefru<Snefru256>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Snefru256" /> class using a fixed 256-bit output size.
        /// </summary>
        public Snefru256()
            : base(256)
        {
        }
    }
}