using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// A stream wrapper that monitors and records all read operations performed on the innerStream stream. Useful for verifying stream
    /// access patterns during asynchronous hash computations.
    /// </summary>
    public sealed class MonitoringStream
        : System.IO.Stream
    {
        private readonly Stream innerStream;
        private readonly List<(long Offset, int Count)> reads = new();
        private long position;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitoringStream" /> class.
        /// </summary>
        /// <param name="stream">The inner stream to monitor.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream" /> is null.</exception>
        public MonitoringStream(Stream stream)
        {
            ThrowHelper.ThrowIfNull(stream);
            this.innerStream = stream;
        }

        /// <inheritdoc />
        public override bool CanRead => this.innerStream.CanRead;

        /// <inheritdoc />
        public override bool CanSeek => this.innerStream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => this.innerStream.CanWrite;

        /// <inheritdoc />
        public override long Length => this.innerStream.Length;

        /// <inheritdoc />
        public override long Position
        {
            get => this.position;
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets a read-only list of all read operations, each represented by the starting offset and number of bytes read.
        /// </summary>
        public IReadOnlyList<(long Offset, int Count)> Reads => reads;

        /// <inheritdoc />
        public override void Flush() => this.innerStream.Flush();

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = this.innerStream.Read(buffer, offset, count);
            if (bytesRead > 0)
            {
                this.reads.Add((this.position, bytesRead));
                this.position += bytesRead;
            }
            return bytesRead;
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int bytesRead = await this.innerStream.ReadAsync(buffer, offset, count, cancellationToken);
            if (bytesRead > 0)
            {
                this.reads.Add((this.position, bytesRead));
                this.position += bytesRead;
            }
            return bytesRead;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        /// Returns the contents of the underlying stream as a byte array.
        /// </summary>
        /// <remarks>
        /// If the underlying stream supports seeking, this method reads the entire content without affecting the current position. If not,
        /// a <see cref="NotSupportedException" /> is thrown.
        /// </remarks>
        /// <returns>A byte array containing the full content of the inner stream.</returns>
        /// <exception cref="NotSupportedException">Thrown if the inner stream does not support seeking.</exception>
        public byte[] ToArray()
        {
            if (!innerStream.CanSeek)
                throw new NotSupportedException("The inner stream must support seeking to use ToArray().");

            long originalPosition = innerStream.Position;

            try
            {
                innerStream.Position = 0;
                using var memory = new MemoryStream();
                innerStream.CopyTo(memory);
                return memory.ToArray();
            }
            finally
            {
                innerStream.Position = originalPosition;
            }
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            this.innerStream.Write(buffer, offset, count);
            this.position += count;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            this.position += count;
            return this.innerStream.WriteAsync(buffer, offset, count, cancellationToken);
        }
    }
}