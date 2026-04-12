using Bodu.Security.Cryptography;
using System;
using System.Security.Cryptography;

namespace Bodu.Testing.Security
{
    /// <summary>
    /// A diagnostic block cipher implementation that tracks usage of encryption and decryption operations. This class is intended for
    /// testing CBC, CFB, and other cipher modes without requiring real encryption.
    /// </summary>
    public sealed class MonitoringBlockCipher
        : IBlockCipher
    {
        private readonly byte xorMask;

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringBlockCipher" /> class.
        /// </summary>
        /// <param name="blockSize">The size of each block in bytes. Defaults to 4.</param>
        /// <param name="xorMask">An optional XOR mask to apply during transformation.</param>
        public MonitoringBlockCipher(int blockSize = 4, byte xorMask = 0xAA)
        {
            if (blockSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize));

            this.BlockSize = blockSize;
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

        /// <inheritdoc />
        public void Decrypt(ReadOnlySpan<byte> input, Span<byte> output)
        {
            DecryptCallCount++;
            DecryptBlockCount += input.Length / BlockSize;
            BytesProcessed += input.Length;

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

            EncryptCalled?.Invoke(this, EventArgs.Empty);

            for (int i = 0; i < input.Length; i++)
                output[i] = (byte)(input[i] ^ xorMask);
        }
    }
}