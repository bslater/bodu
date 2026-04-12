using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Infrastructure
{
    // <summary>
    /// A test stream that simulates slow I/O by sleeping on each read, to trigger cancellation scenarios. </summary>
    public sealed class ThrottledIncrementingByteStream
        : IncrementingByteStream
    {
        private readonly int throttleDelayMs;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThrottledIncrementingByteStream" /> class with the specified total number of bytes.
        /// </summary>
        /// <param name="totalCount">The total number of bytes the stream will return.</param>
        public ThrottledIncrementingByteStream(int totalCount, int readDelay = 1000)
            : base(totalCount)
        {
            this.throttleDelayMs = readDelay;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            Thread.Sleep(this.throttleDelayMs); // simulate delay
            return base.Read(buffer, offset, count);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(this.throttleDelayMs, cancellationToken); // simulate delay

            // explicitly handle cancellation
            cancellationToken.ThrowIfCancellationRequested();

            return await base.ReadAsync(buffer, cancellationToken);
        }
    }
}