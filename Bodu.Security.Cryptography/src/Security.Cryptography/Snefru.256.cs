using System;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Computes the hash for the input data using the <c>Snefru-256</c> hash algorithm. This variant performs block-based permutation using
    /// S-boxes and rotations across a 512-bit buffer and an 8-word internal state. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Snefru256" /> computes a 256-bit (32-byte) hash using 8 rounds of transformation applied to 64-byte input blocks. It
    /// maintains an 8-element internal state of 32-bit unsigned integers, with output produced after mixing and finalization.
    /// </para>
    /// <para>
    /// The core transformation involves alternating S-box substitutions and circular bit rotations. Each S-box round updates surrounding
    /// buffer words based on fixed lookup tables. Rotations vary across rounds to promote cross-word diffusion. The permuted buffer values
    /// are XORed back into the internal state.
    /// </para>
    /// <para>
    /// Input is padded to 128 bytes (2 × block size), appending the total message bit length at the end. The final state is written in
    /// big-endian format to produce the final digest.
    /// </para>
    /// <note type="important">This algorithm is <b>not</b> suitable for cryptographic applications such as password hashing, digital
    /// signatures, or secure data integrity checks.</note>
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