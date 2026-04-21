namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// A diagnostic block cipher implementation that tracks usage of encryption and decryption
    /// operations. This class is intended for testing CBC, CFB, OFB, CTR, OCB, and other cipher
    /// modes without requiring real encryption.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The transform is an XOR with a fixed mask — reversible, so <see cref="Decrypt" /> is a
    /// true inverse of <see cref="Encrypt" /> and round-trip tests work correctly. Be aware,
    /// however, that XOR is <b>linear</b>: when a mode surrounds its cipher call with pre- and
    /// post-XOR operations against the same value (OCB's offset, for example), that value cancels
    /// and the ciphertext is reduced to <c>plaintext XOR mask</c>. Tests that need to verify a
    /// mode's chaining/offset is actually taking place should assert on
    /// <see cref="EncryptInputs" /> / <see cref="DecryptInputs" /> rather than on the ciphertext
    /// bytes.
    /// </para>
    /// </remarks>
    public sealed class MonitoringBlockCipher
        : IBlockCipher
    {
        private readonly byte xorMask;

        // Captured copies of every buffer passed to Encrypt / Decrypt, in call order.
        // Kept as byte[] copies so callers can inspect them after the transform returns and the
        // underlying spans have been reused.
        private readonly List<byte[]> encryptInputs = new();
        private readonly List<byte[]> decryptInputs = new();

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringBlockCipher" /> class.
        /// </summary>
        /// <param name="blockSize">The size of each block in bytes. Defaults to 4.</param>
        /// <param name="xorMask">An optional XOR mask to apply during transformation.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="blockSize" /> is not positive.</exception>
        public MonitoringBlockCipher(int blockSize = 4, byte xorMask = 0xAA)
        {
            if (blockSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize));

            BlockSize = blockSize;
            this.xorMask = xorMask;
        }

        /// <summary>
        /// Raised each time <see cref="Decrypt" /> is called.
        /// </summary>
        public event EventHandler? DecryptCalled;

        /// <summary>
        /// Raised each time <see cref="Encrypt" /> is called.
        /// </summary>
        public event EventHandler? EncryptCalled;

        /// <summary>
        /// Gets the fixed block size in bytes. Defaults to 4 for test simplicity.
        /// </summary>
        public int BlockSize { get; }

        /// <summary>
        /// Gets or sets the number of bytes processed.
        /// </summary>
        public int BytesProcessed { get; private set; }

        /// <summary>
        /// Gets the number of blocks processed via <see cref="Decrypt" />.
        /// </summary>
        public int DecryptBlockCount { get; private set; }

        /// <summary>
        /// Gets the number of times <see cref="Decrypt" /> was called.
        /// </summary>
        public int DecryptCallCount { get; private set; }

        /// <summary>
        /// Gets the number of blocks processed via <see cref="Encrypt" />.
        /// </summary>
        public int EncryptBlockCount { get; private set; }

        /// <summary>
        /// Gets the number of times <see cref="Encrypt" /> was called.
        /// </summary>
        public int EncryptCallCount { get; private set; }

        /// <summary>
        /// Gets an ordered, read-only view of every buffer that has been passed to
        /// <see cref="Encrypt" />, captured as an independent copy at the moment of each call.
        /// </summary>
        /// <remarks>
        /// This is the definitive signal that a mode is actively chaining, injecting an IV, or
        /// advancing an offset — the <i>inputs</i> the underlying cipher received must differ
        /// across blocks even when the plaintext blocks are identical. Asserting on this collection
        /// is robust against the XOR transform's linearity, which can otherwise make distinct
        /// cipher inputs produce identical ciphertext.
        /// </remarks>
        public IReadOnlyList<byte[]> EncryptInputs => encryptInputs;

        /// <summary>
        /// Gets an ordered, read-only view of every buffer that has been passed to
        /// <see cref="Decrypt" />, captured as an independent copy at the moment of each call.
        /// </summary>
        public IReadOnlyList<byte[]> DecryptInputs => decryptInputs;

        /// <inheritdoc />
        public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            DecryptCallCount++;
            DecryptBlockCount += input.Length / BlockSize;
            BytesProcessed += input.Length;
            decryptInputs.Add(input.ToArray());

            DecryptCalled?.Invoke(this, EventArgs.Empty);

            for (int i = 0; i < input.Length; i++)
                output[i] = (byte)(input[i] ^ xorMask); // reversible
        }

        /// <summary>
        /// Releases all resources used by the <see cref="MonitoringBlockCipher" />.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
                return;

            // Reset instrumentation counters (optional for test repeatability)
            BytesProcessed = 0;
            EncryptCallCount = 0;
            DecryptCallCount = 0;
            EncryptBlockCount = 0;
            DecryptBlockCount = 0;
            encryptInputs.Clear();
            decryptInputs.Clear();

            // Clear event handlers
            EncryptCalled = null;
            DecryptCalled = null;

            disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        public void Encrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            EncryptCallCount++;
            EncryptBlockCount += input.Length / BlockSize;
            BytesProcessed += input.Length;
            encryptInputs.Add(input.ToArray());

            EncryptCalled?.Invoke(this, EventArgs.Empty);

            for (int i = 0; i < input.Length; i++)
                output[i] = (byte)(input[i] ^ xorMask);
        }
    }
}