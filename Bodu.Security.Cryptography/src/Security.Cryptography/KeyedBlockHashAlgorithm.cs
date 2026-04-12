using Bodu.Extensions;
using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Represents the abstract base class for hash algorithms that require a secret key and process data in fixed-size blocks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class extends <see cref="BlockHashAlgorithm{T}" /> to provide key-handling logic required by keyed hash algorithms such as HMAC
    /// or SipHash. It ensures the key is securely managed and disposed using defensive copying and memory clearing.
    /// </para>
    /// <para>
    /// Derived classes must define how the key is integrated into the hashing process. The <see cref="Key" /> property provides controlled
    /// access to the key material and prevents external mutation by using defensive copying.
    /// </para>
    /// </remarks>
    public abstract class KeyedBlockHashAlgorithm<T>
        : BlockHashAlgorithm<T>
        where T : KeyedBlockHashAlgorithm<T>, new()
    {
        /// <summary>
        /// Internal storage for the key used in the algorithm. Always set using a defensive copy.
        /// </summary>
        protected byte[] KeyValue = null!;

        private bool disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyedBlockHashAlgorithm{T}" /> class with the specified input block size.
        /// </summary>
        /// <param name="blockSize">
        /// The fixed size of input blocks, in bytes, that the algorithm processes. This must match the internal block size used by the
        /// underlying hash structure.
        /// </param>
        /// <remarks>
        /// Derived classes are expected to assign a valid key and implement block-wise hashing logic using the configured block size.
        /// </remarks>
        protected KeyedBlockHashAlgorithm(int blockSize)
            : base(blockSize)
        { }

        /// <summary>
        /// Gets or sets the secret key used by the keyed hash algorithm to compute the message authentication code (MAC).
        /// </summary>
        /// <value>A byte array containing the key material. The returned value is a defensive copy to ensure immutability.</value>
        /// <remarks>
        /// <para>
        /// This key must be set prior to calling any hashing methods such as <see cref="HashAlgorithm.ComputeHash(byte[])" />. Derived
        /// implementations may enforce specific key lengths or validation constraints within the setter.
        /// </para>
        /// <para>
        /// The internal key is always stored as a private copy of the caller-supplied array to prevent external mutations. Likewise,
        /// accessing this property returns a copy of the internal key to preserve encapsulation.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if the assigned key value is <see langword="null" />.</exception>
        public virtual byte[] Key
        {
            get => KeyValue.ToArray(); // Return a copy to maintain immutability

            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                // Defensive copy ensures external references cannot mutate internal key
                KeyValue = value.ToArray();
            }
        }

        /// <summary>
        /// Releases the unmanaged resources used by the algorithm and clears the key from memory.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.
        /// </param>
        /// <remarks>Ensures all internal secrets are overwritten with zeros before releasing resources.</remarks>
        protected override void Dispose(bool disposing)
        {
            if (this.disposed) return;

            if (disposing)
            {
                // Zero out the key material to avoid leaking secrets in memory
                if (KeyValue is not null)
                {
                    CryptoHelpers.ClearAndNullify(ref KeyValue!);
                }
            }

            this.disposed = true;
            base.Dispose(disposing);
        }
    }
}