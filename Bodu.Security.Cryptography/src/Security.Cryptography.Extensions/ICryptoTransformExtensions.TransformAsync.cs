using System;
using System.IO;
using System.Security.Cryptography;
using Bodu.Security.Cryptography.Extensions;
using Bodu.Extensions;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Bodu.Security.Cryptography.Extensions
{
    public static partial class ICryptoTransformExtensions
    {
        /// <summary>
        /// Asynchronously applies a cryptographic transformation to data read from a source stream and writes the transformed output to a
        /// target stream. Supports cancellation and ensures the transformation is finalized correctly.
        /// </summary>
        /// <param name="transform">The cryptographic transform to apply.</param>
        /// <param name="sourceStream">The stream to read untransformed data from.</param>
        /// <param name="targetStream">The stream to write transformed data to.</param>
        /// <param name="bufferSize">The buffer size (in bytes) used for streaming. Must be greater than zero.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the transformation before or during processing, including prior to finalization.</param>
        /// <returns>A task representing the asynchronous transformation operation.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="transform" />, <paramref name="sourceStream" />, or <paramref name="targetStream" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bufferSize" /> is less than or equal to zero.</exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the provided <paramref name="cancellationToken" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method uses a <see cref="CryptoStreamBen" /> internally to write the transformed data to the target stream. It ensures
        /// <see cref="CryptoStreamBen.FlushFinalBlockAsync" /> is called to finalize the cryptographic operation properly.
        /// </para>
        /// <para>
        /// Cancellation is checked before each read and write operation, and again before finalizing the transformation. If cancellation is requested,
        /// an <see cref="OperationCanceledException" /> is thrown and finalization is skipped to avoid cryptographic errors.
        /// </para>
        /// <para>
        /// Both source and target streams must support asynchronous operations. The method does not dispose the source or target stream.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// using var input = File.OpenRead("input.txt");
        /// using var output = File.Create("encrypted.bin");
        /// await aes.CreateEncryptor().TransformAsync(input, output, 4096);
        ///]]>
        /// </code>
        /// </example>
        public static async Task TransformAsync(
            this ICryptoTransform transform,
            Stream sourceStream,
            Stream targetStream,
            int bufferSize,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(transform);
            ThrowHelper.ThrowIfNull(sourceStream);
            ThrowHelper.ThrowIfNull(targetStream);
            ThrowHelper.ThrowIfLessThanOrEqual(bufferSize, 0);

            byte[] buffer = new byte[bufferSize];

            CryptoStream cryptoStream = new CryptoStream(targetStream, transform, CryptoStreamMode.Write, leaveOpen: true);
            try
            {
                int bytesRead;
                while (true)
                {
                    try
                    {
                        bytesRead = await sourceStream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw new TaskCanceledException();
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (bytesRead == 0)
                        break;

                    await cryptoStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                }

                cancellationToken.ThrowIfCancellationRequested();
                await cryptoStream.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Prevent Dispose from calling FlushFinalBlock implicitly if cancellation occurred
                cryptoStream.Dispose();
            }
        }

        /// <summary>
        /// Asynchronously applies a cryptographic transformation to a memory region and writes the transformed result into a destination memory region.
        /// Supports cancellation and ensures the transformation is finalized correctly.
        /// </summary>
        /// <param name="transform">The cryptographic transform to apply.</param>
        /// <param name="input">The memory region containing the input data.</param>
        /// <param name="destination">The memory region to write the transformed output to.</param>
        /// <param name="cancellationToken">
        /// A cancellation token that can be used to cancel the transformation before or during processing, including prior to finalization.
        /// </param>
        /// <returns>The number of bytes written to the destination memory region.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="transform" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="destination" /> is too small to hold the transformed output.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled via the provided <paramref name="cancellationToken" />.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the transformed data cannot be accessed after finalization.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method uses a <see cref="CryptoStream" /> internally to apply the transformation.
        /// It ensures <see cref="CryptoStream.FlushFinalBlockAsync" /> is called to finalize the cryptographic operation properly.
        /// </para>
        /// <para>
        /// Cancellation is checked before finalizing the transformation. If cancellation is requested,
        /// an <see cref="OperationCanceledException" /> is thrown and finalization is skipped to prevent cryptographic errors.
        /// </para>
        /// <para>
        /// The destination memory region must be large enough to accommodate the transformed output.
        /// A safe minimum size is <c>input.Length + transform.OutputBlockSize</c>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        ///<![CDATA[
        /// using var aes = Aes.Create();
        /// using var encryptor = aes.CreateEncryptor();
        ///
        /// byte[] input = new byte[] { 0x01, 0x02, 0x03 };
        /// byte[] buffer = new byte[64];
        ///
        /// int written = await encryptor.TransformAsync(input, buffer);
        ///]]>
        /// </code>
        /// </example>
        public static async Task<int> TransformAsync(
            this ICryptoTransform transform,
            ReadOnlyMemory<byte> input,
            Memory<byte> destination,
            CancellationToken cancellationToken = default)
        {
            ThrowHelper.ThrowIfNull(transform);
            ThrowHelper.ThrowIfArrayLengthIsInsufficient(destination.Span, 0, input.Length + transform.OutputBlockSize);

            using var ms = new MemoryStream(destination.Length);
            using var cryptoStream = new CryptoStream(ms, transform, CryptoStreamMode.Write, leaveOpen: true);

            await cryptoStream.WriteAsync(input, cancellationToken).ConfigureAwait(false);

            // Ensure cancellation is respected before finalization
            cancellationToken.ThrowIfCancellationRequested();

            await cryptoStream.FlushFinalBlockAsync(cancellationToken).ConfigureAwait(false);

            if (!ms.TryGetBuffer(out ArraySegment<byte> segment))
                throw new InvalidOperationException("Failed to access transformed data.");

            segment.AsSpan().CopyTo(destination.Span);
            return segment.Count;
        }
    }

    /*
    public class CryptoStreamBen
        : Stream
        , IDisposable
    {
        private readonly int inputBlockSize;
        private readonly bool leaveOpen;
        private readonly int outputBlockSize;

        // Member variables
        private readonly Stream stream;

        private readonly ICryptoTransform transform;
        private bool canRead;
        private bool canWrite;
        private bool finalBlockTransformed;
        private byte[] inputBuffer;
        private int inputBufferIndex;
        private SemaphoreSlim? lazyAsyncActiveSemaphore;
        private byte[] outputBuffer;
        private int outputBufferIndex;

        public CryptoStreamBen(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
            : this(stream, transform, mode, false)
        {
        }

        // Constructors
        public CryptoStreamBen(Stream stream, ICryptoTransform transform, CryptoStreamMode mode, bool leaveOpen)
        {
            ArgumentNullException.ThrowIfNull(transform);

            this.stream = stream;
            this.transform = transform;
            this.leaveOpen = leaveOpen;

            switch (mode)
            {
                case CryptoStreamMode.Read:
                    if (!this.stream.CanRead)
                    {
                        throw new ArgumentException();//SR.Argument_StreamNotReadable, nameof(stream));
                    }
                    this.canRead = true;
                    break;

                case CryptoStreamMode.Write:
                    if (!this.stream.CanWrite)
                    {
                        throw new ArgumentException();//SR.Argument_StreamNotWritable, nameof(stream));
                    }
                    this.canWrite = true;
                    break;

                default:
                    throw new ArgumentException();//SR.Argument_InvalidValue, nameof(mode));
            }

            this.inputBlockSize = this.transform.InputBlockSize;
            this.inputBuffer = new byte[this.inputBlockSize];
            this.outputBlockSize = this.transform.OutputBlockSize;
            this.outputBuffer = new byte[this.outputBlockSize];
        }

        public override bool CanRead
        {
            get { return this.canRead; }
        }

        // For now, assume we can never seek into the middle of a cryptostream and get the state right. This is too strict.
        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return this.canWrite; }
        }

        public bool HasFlushedFinalBlock
        {
            get { return this.finalBlockTransformed; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }//SR.NotSupported_UnseekableStream); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }//SR.NotSupported_UnseekableStream); }
            set { throw new NotSupportedException(); }//SR.NotSupported_UnseekableStream); }
        }

        // read from this.stream before this.Transform buffered output of this.Transform
        [MemberNotNull(nameof(this.lazyAsyncActiveSemaphore))]
        private SemaphoreSlim AsyncActiveSemaphore
        {
            get

            {
                // Lazily-initialize this.lazyAsyncActiveSemaphore. As we're never accessing the SemaphoreSlim's WaitHandle, we don't need
                // to worry about Disposing it.
                return LazyInitializer.EnsureInitialized(ref this.lazyAsyncActiveSemaphore, () => new SemaphoreSlim(1, 1));
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                    TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
                    TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public void Clear()
        {
            Close();
        }

        /// <inheritdoc />
        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            CheckCopyToArguments(destination, bufferSize);
            return CopyToAsyncInternal(destination, bufferSize, cancellationToken);
        }

        public override ValueTask DisposeAsync()
        {
            return GetType() != typeof(CryptoStreamBen) ?
                base.DisposeAsync() :
                DisposeAsyncCore();
        }

        public override int EndRead(IAsyncResult asyncResult) =>
                    TaskToAsyncResult.End<int>(asyncResult);

        public override void EndWrite(IAsyncResult asyncResult) =>
                    TaskToAsyncResult.End(asyncResult);

        public override void Flush()
        {
            if (this.canWrite)
            {
                this.stream.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            // If we have been inherited into a subclass, the following implementation could be incorrect since it does not call through to
            // Flush() which a subclass might have overridden. To be safe we will only use this implementation in cases where we know it is
            // safe to do so, and delegate to our base class (which will call into Flush) when we are not sure.
            if (GetType() != typeof(CryptoStreamBen))
                return base.FlushAsync(cancellationToken);

            return cancellationToken.IsCancellationRequested ?
                Task.FromCanceled(cancellationToken) :
                !this.canWrite ? Task.CompletedTask :
                this.stream.FlushAsync(cancellationToken);
        }

        // The flush final block functionality used to be part of close, but that meant you couldn't do something like this: MemoryStream ms
        // = new MemoryStream(); CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write); cs.Write(foo, 0,
        // foo.Length); cs.Close(); and get the encrypted data out of ms, because the cs.Close also closed ms and the data went away. so now
        // do this: cs.Write(foo, 0, foo.Length); cs.FlushFinalBlock() // which can only be called once byte[] ciphertext = ms.ToArray(); cs.Close();
        public void FlushFinalBlock() =>
            FlushFinalBlockAsync(useAsync: false, default).AsTask().GetAwaiter().GetResult();

        /// <summary>
        /// Asynchronously updates the underlying data source or repository with the current state of the buffer, then clears the buffer.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None" />.</param>
        /// <returns>A task that represents the asynchronous flush operation.</returns>
        public ValueTask FlushFinalBlockAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled(cancellationToken);

            return FlushFinalBlockAsync(useAsync: true, cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckReadArguments(buffer, offset, count);
            ValueTask<int> completedValueTask = ReadAsyncCore(buffer.AsMemory(offset, count), default(CancellationToken), useAsync: false);
            Debug.Assert(completedValueTask.IsCompleted);

            return completedValueTask.GetAwaiter().GetResult();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckReadArguments(buffer, offset, count);
            return ReadAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanRead)
                return ValueTask.FromException<int>(new NotSupportedException());//SR.NotSupported_UnreadableStream));

            return ReadAsyncInternal(buffer, cancellationToken);
        }

        public override int ReadByte()
        {
            // If we have enough bytes in the buffer such that reading 1 will still leave bytes in the buffer, then take the faster path of
            // simply returning the first byte. (This unfortunately still involves shifting down the bytes in the buffer, as it does in
            // Read. If/when that's fixed for Read, it should be fixed here, too.)
            if (this.outputBufferIndex > 1)
            {
                Debug.Assert(this.outputBuffer != null);
                byte b = this.outputBuffer[0];
                Buffer.BlockCopy(this.outputBuffer, 1, this.outputBuffer, 0, this.outputBufferIndex - 1);
                this.outputBufferIndex -= 1;
                return b;
            }

            // Otherwise, fall back to the more robust but expensive path of using the base Stream.ReadByte to call Read.
            return base.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();//SR.NotSupported_UnseekableStream);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();//SR.NotSupported_UnseekableStream);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckWriteArguments(buffer, offset, count);
            WriteAsyncCore(buffer.AsMemory(offset, count), default, useAsync: false).AsTask().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            // Logically this is doing the same thing as the base Stream, however CryptoStream clears arrays before returning them to the
            // pool, whereas the base Stream does not. Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);

            try
            {
                buffer.CopyTo(sharedBuffer);

                // We want to keep calling the virtual Write(byte[]...) so that derived CryptoStream types continue to get the array
                // overload called from the span one.
                Write(sharedBuffer, 0, buffer.Length);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(sharedBuffer.AsSpan(0, buffer.Length));
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckWriteArguments(buffer, offset, count);
            return WriteAsyncInternal(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!CanWrite)
                return ValueTask.FromException(new NotSupportedException());//SR.NotSupported_UnwritableStream));

            return WriteAsyncInternal(buffer, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            // If there's room in the input buffer such that even with this byte we wouldn't complete a block, simply add the byte to the
            // input buffer.
            if (this.inputBufferIndex + 1 < this.inputBlockSize)
            {
                this.inputBuffer![this.inputBufferIndex++] = value;
                return;
            }

            // Otherwise, the logic is complicated, so we simply fall back to the base implementation that'll use Write.
            base.WriteByte(value);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (!this.finalBlockTransformed)
                    {
                        FlushFinalBlock();
                    }
                    if (!this.leaveOpen)
                    {
                        this.stream.Dispose();
                    }
                }
            }
            finally
            {
                try
                {
                    // Ensure we don't try to transform the final block again if we get disposed twice since it's null after this
                    this.finalBlockTransformed = true;

                    // we need to clear all the internal buffers
                    if (this.inputBuffer != null)
                        Array.Clear(this.inputBuffer);
                    if (this.outputBuffer != null)
                        Array.Clear(this.outputBuffer);

                    this.inputBuffer = null!;
                    this.outputBuffer = null!;
                    this.canRead = false;
                    this.canWrite = false;
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        private void CheckCopyToArguments(Stream destination, int bufferSize)
        {
            ArgumentNullException.ThrowIfNull(destination);
            ObjectDisposedException.ThrowIf(!destination.CanRead && !destination.CanWrite, destination);

            if (!destination.CanWrite)
                throw new NotSupportedException();//SR.NotSupported_UnwritableStream);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);
            if (!CanRead)
                throw new NotSupportedException();// SR.NotSupported_UnreadableStream);
        }

        private void CheckReadArguments(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanRead)
                throw new NotSupportedException();//SR.NotSupported_UnreadableStream);
        }

        private void CheckWriteArguments(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);
            if (!CanWrite)
                throw new NotSupportedException();//SR.NotSupported_UnwritableStream);
        }

        //    // Use ArrayPool<byte>.Shared instead of CryptoPool because the array is passed out.
        //    byte[]? rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        //    // Pin the array for security.
        //    fixed (byte* _ = &rentedBuffer[0])
        //    {
        //        try
        //        {
        //            int bytesRead;
        //            do
        //            {
        //                bytesRead = Read(rentedBuffer, 0, bufferSize);
        //                destination.Write(rentedBuffer, 0, bytesRead);
        //            } while (bytesRead > 0);
        //        }
        //        finally
        //        {
        //            CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, bufferSize));
        //        }
        //    }
        //    ArrayPool<byte>.Shared.Return(rentedBuffer);
        //    rentedBuffer = null;
        //}
        private async Task CopyToAsyncInternal(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            // Use ArrayPool<byte>.Shared instead of CryptoPool because the array is passed out.
            byte[]? rentedBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            // Pin the array for security.
            GCHandle pinHandle = GCHandle.Alloc(rentedBuffer, GCHandleType.Pinned);
            try
            {
                int bytesRead;
                do
                {
                    bytesRead = await ReadAsync(rentedBuffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(false);
                    await destination.WriteAsync(rentedBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                } while (bytesRead > 0);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, bufferSize));
                pinHandle.Free();
            }
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }

        /// <inheritdoc />
        //public override unsafe void CopyTo(Stream destination, int bufferSize)
        //{
        //    CheckCopyToArguments(destination, bufferSize);
        private async ValueTask DisposeAsyncCore()
        {
            // Same logic as in Dispose, but with async counterparts
            try
            {
                if (!this.finalBlockTransformed)
                {
                    await FlushFinalBlockAsync(useAsync: true, default).ConfigureAwait(false);
                }

                if (!this.leaveOpen)
                {
                    await this.stream.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                // Ensure we don't try to transform the final block again if we get disposed twice since it's null after this
                this.finalBlockTransformed = true;

                // we need to clear all the internal buffers
                if (this.inputBuffer != null)
                {
                    Array.Clear(this.inputBuffer);
                }

                if (this.outputBuffer != null)
                {
                    Array.Clear(this.outputBuffer);
                }

                this.inputBuffer = null!;
                this.outputBuffer = null!;
                this.canRead = false;
                this.canWrite = false;
            }
        }

        private async ValueTask FlushFinalBlockAsync(bool useAsync, CancellationToken cancellationToken)
        {
            if (this.finalBlockTransformed)
                throw new NotSupportedException();//SR.Cryptography_CryptoStream_FlushFinalBlockTwice);
            this.finalBlockTransformed = true;

            // Transform and write out the final bytes.
            if (this.canWrite)
            {
                Debug.Assert(this.outputBufferIndex == 0, "The output this.index can only ever be non-zero when in read this.mode.");

                byte[] finalBytes = this.transform.TransformFinalBlock(this.inputBuffer!, 0, this.inputBufferIndex);
                if (useAsync)
                {
                    await this.stream.WriteAsync(new ReadOnlyMemory<byte>(finalBytes), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    this.stream.Write(finalBytes, 0, finalBytes.Length);
                }
            }

            // If the inner stream is a CryptoStream, then we want to call FlushFinalBlock on it too, otherwise just Flush.
            if (this.stream is CryptoStreamBen innerCryptoStream)
            {
                if (!innerCryptoStream.HasFlushedFinalBlock)
                {
                    await innerCryptoStream.FlushFinalBlockAsync(useAsync, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                if (useAsync)
                {
                    await this.stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    this.stream.Flush();
                }
            }

            // zeroize plain text material before returning
            if (this.inputBuffer != null)
                Array.Clear(this.inputBuffer);
            if (this.outputBuffer != null)
                Array.Clear(this.outputBuffer);
        }

        private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken, bool useAsync)
        {
            while (true)
            {
                // If there are currently any bytes stored in the output buffer, hand back as many as we can.
                if (this.outputBufferIndex != 0)
                {
                    int bytesToCopy = Math.Min(this.outputBufferIndex, buffer.Length);
                    if (bytesToCopy != 0)
                    {
                        // Copy as many bytes as we can, then shift down the remaining bytes.
                        new ReadOnlySpan<byte>(this.outputBuffer, 0, bytesToCopy).CopyTo(buffer.Span);
                        this.outputBufferIndex -= bytesToCopy;
                        this.outputBuffer.AsSpan(bytesToCopy).CopyTo(this.outputBuffer);
                        CryptographicOperations.ZeroMemory(this.outputBuffer.AsSpan(this.outputBufferIndex, bytesToCopy));
                    }
                    return bytesToCopy;
                }

                // If we've already hit the end of the stream, there's nothing more to do.
                Debug.Assert(this.outputBufferIndex == 0);
                if (this.finalBlockTransformed)
                {
                    Debug.Assert(this.inputBufferIndex == 0);
                    return 0;
                }

                int bytesRead = 0;
                bool eof = false;

                // If the transform supports transforming multiple blocks, try to read as large a chunk as would yield data to fill the
                // output buffer and do the appropriate transform directly into the output buffer.
                int blocksToProcess = buffer.Length / this.outputBlockSize;
                if (blocksToProcess > 1 && this.transform.CanTransformMultipleBlocks)
                {
                    // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
                    int numWholeBlocksInBytes = checked(blocksToProcess * this.inputBlockSize);
                    byte[] tempInputBuffer = ArrayPool<byte>.Shared.Rent(numWholeBlocksInBytes);
                    try
                    {
                        // Read into our temporary input buffer, leaving enough room at the beginning for any existing data we have in this.inputBuffer.
                        bytesRead = useAsync ?
                            await this.stream.ReadAsync(new Memory<byte>(tempInputBuffer, this.inputBufferIndex, numWholeBlocksInBytes - this.inputBufferIndex), cancellationToken).ConfigureAwait(false) :
                            this.stream.Read(tempInputBuffer, this.inputBufferIndex, numWholeBlocksInBytes - this.inputBufferIndex);
                        eof = bytesRead == 0;

                        // If we got enough data to form at least one block, transform as much as we can.
                        int totalInput = this.inputBufferIndex + bytesRead;
                        if (totalInput >= this.inputBlockSize)
                        {
                            // Copy any held data into tempInputBuffer now that we know we're proceeding to handle decrypting all the
                            // received data.
                            Buffer.BlockCopy(this.inputBuffer, 0, tempInputBuffer, 0, this.inputBufferIndex);
                            CryptographicOperations.ZeroMemory(new Span<byte>(this.inputBuffer, 0, this.inputBufferIndex));
                            bytesRead += this.inputBufferIndex;

                            // Determine how many entire blocks worth of data we read.
                            int numWholeReadBlocks = bytesRead / this.inputBlockSize;
                            int numWholeReadBlocksInBytes = numWholeReadBlocks * this.inputBlockSize;

                            // If there's anything left over, copy that back into this.inputBuffer for a later read.
                            this.inputBufferIndex = bytesRead - numWholeReadBlocksInBytes;
                            if (this.inputBufferIndex != 0)
                            {
                                Buffer.BlockCopy(tempInputBuffer, numWholeReadBlocksInBytes, this.inputBuffer, 0, this.inputBufferIndex);
                            }

                            // Transform the read data into the caller's buffer.
                            int numOutputBytes;
                            if (MemoryMarshal.TryGetArray(buffer, out ArraySegment<byte> bufferArray))
                            {
                                // Because TransformBlock is based on arrays, we can only write directly into the output buffer if it's
                                // backed by an array; otherwise, we need to rent from the pool.
                                numOutputBytes = this.transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, bufferArray.Array!, bufferArray.Offset);
                            }
                            else
                            {
                                // Otherwise, we need to rent a temporary from the pool.
                                byte[] tempOutputBuffer = ArrayPool<byte>.Shared.Rent(numWholeReadBlocks * this.outputBlockSize);
                                numOutputBytes = numWholeReadBlocks * this.outputBlockSize;
                                try
                                {
                                    numOutputBytes = this.transform.TransformBlock(tempInputBuffer, 0, numWholeReadBlocksInBytes, tempOutputBuffer, 0);
                                    tempOutputBuffer.AsSpan(0, numOutputBytes).CopyTo(buffer.Span);
                                }
                                finally
                                {
                                    CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes));
                                    ArrayPool<byte>.Shared.Return(tempOutputBuffer);
                                }
                            }

                            // Return anything we've got at this point.
                            if (numOutputBytes != 0)
                            {
                                return numOutputBytes;
                            }
                        }
                        else
                        {
                            // We have less than a block's worth of data. Copy the new data back into the this.inputBuffer and fall back to
                            // using the single block code path.
                            Buffer.BlockCopy(tempInputBuffer, this.inputBufferIndex, this.inputBuffer, this.inputBufferIndex, bytesRead);
                            this.inputBufferIndex = totalInput;
                        }
                    }
                    finally
                    {
                        CryptographicOperations.ZeroMemory(new Span<byte>(tempInputBuffer, 0, numWholeBlocksInBytes));
                        ArrayPool<byte>.Shared.Return(tempInputBuffer);
                    }
                }

                // Read enough to fill one input block, as anything less won't be able to be transformed to produce output.
                if (!eof)
                {
                    while (this.inputBufferIndex < this.inputBlockSize)
                    {
                        bytesRead = useAsync ?
                            await this.stream.ReadAsync(new Memory<byte>(this.inputBuffer, this.inputBufferIndex, this.inputBlockSize - this.inputBufferIndex), cancellationToken).ConfigureAwait(false) :
                            this.stream.Read(this.inputBuffer, this.inputBufferIndex, this.inputBlockSize - this.inputBufferIndex);
                        if (bytesRead <= 0)
                        {
                            break;
                        }

                        this.inputBufferIndex += bytesRead;
                    }
                }

                // Transform the received data.
                if (bytesRead <= 0)
                {
                    this.outputBuffer = this.transform.TransformFinalBlock(this.inputBuffer, 0, this.inputBufferIndex);
                    this.outputBufferIndex = this.outputBuffer.Length;
                    this.finalBlockTransformed = true;
                }
                else
                {
                    this.outputBufferIndex = this.transform.TransformBlock(this.inputBuffer, 0, this.inputBufferIndex, this.outputBuffer, 0);
                }

                // All input data has been processed.
                this.inputBufferIndex = 0;
            }
        }

        private async ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // To avoid a race with a stream's position pointer & generating race conditions with internal buffer indexes in our own streams
            // that don't natively support async IO operations when there are multiple async requests outstanding, we will block the
            // application's main thread if it does a second IO request until the first one completes.

            await AsyncActiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await ReadAsyncCore(buffer, cancellationToken, useAsync: true).ConfigureAwait(false);
            }
            finally
            {
                this.lazyAsyncActiveSemaphore.Release();
            }
        }

        private async ValueTask WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, bool useAsync)
        {
            // write <= count bytes to the output stream, transforming as we go. Basic idea: using bytes in the this.InputBuffer first, make
            // whole blocks, transform them, and write them out. Cache any remaining bytes in the this.InputBuffer.
            int bytesToWrite = buffer.Length;
            int currentInputIndex = 0;

            // if we have some bytes in the this.InputBuffer, we have to deal with those first, so let's try to make an entire block out of it
            if (this.inputBufferIndex > 0)
            {
                Debug.Assert(this.inputBuffer != null);
                if (buffer.Length >= this.inputBlockSize - this.inputBufferIndex)
                {
                    // we have enough to transform at least a block, so fill the input block
                    buffer.Slice(0, this.inputBlockSize - this.inputBufferIndex).CopyTo(this.inputBuffer.AsMemory(this.inputBufferIndex));
                    currentInputIndex += (this.inputBlockSize - this.inputBufferIndex);
                    bytesToWrite -= (this.inputBlockSize - this.inputBufferIndex);
                    this.inputBufferIndex = this.inputBlockSize;

                    // Transform the block and write it out
                }
                else
                {
                    // not enough to transform a block, so just copy the bytes into the this.InputBuffer and return
                    buffer.CopyTo(this.inputBuffer.AsMemory(this.inputBufferIndex));
                    this.inputBufferIndex += buffer.Length;
                    return;
                }
            }

            Debug.Assert(this.outputBufferIndex == 0, "The output this.index can only ever be non-zero when in read this.mode.");

            // At this point, either the this.InputBuffer is full, empty, or we've already returned. If full, let's process it -- we now
            // know the this.OutputBuffer is empty
            int numOutputBytes;
            if (this.inputBufferIndex == this.inputBlockSize)
            {
                Debug.Assert(this.inputBuffer != null && this.outputBuffer != null);
                numOutputBytes = this.transform.TransformBlock(this.inputBuffer, 0, this.inputBlockSize, this.outputBuffer, 0);

                // write out the bytes we just got
                if (useAsync)
                    await this.stream.WriteAsync(new ReadOnlyMemory<byte>(this.outputBuffer, 0, numOutputBytes), cancellationToken).ConfigureAwait(false);
                else
                    this.stream.Write(this.outputBuffer, 0, numOutputBytes);

                // reset the this.InputBuffer
                this.inputBufferIndex = 0;
            }
            while (bytesToWrite > 0)
            {
                if (bytesToWrite >= this.inputBlockSize)
                {
                    // We have at least an entire block's worth to transform
                    int numWholeBlocks = bytesToWrite / this.inputBlockSize;

                    // If the transform will handle multiple blocks at once, do that
                    if (this.transform.CanTransformMultipleBlocks && numWholeBlocks > 1)
                    {
                        int numWholeBlocksInBytes = numWholeBlocks * this.inputBlockSize;

                        // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
                        byte[]? tempOutputBuffer = ArrayPool<byte>.Shared.Rent(checked(numWholeBlocks * this.outputBlockSize));
                        numOutputBytes = 0;

                        try
                        {
                            numOutputBytes = TransformBlock(this.transform, buffer.Slice(currentInputIndex, numWholeBlocksInBytes), tempOutputBuffer, 0);

                            if (useAsync)
                            {
                                await this.stream.WriteAsync(new ReadOnlyMemory<byte>(tempOutputBuffer, 0, numOutputBytes), cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                this.stream.Write(tempOutputBuffer, 0, numOutputBytes);
                            }

                            currentInputIndex += numWholeBlocksInBytes;
                            bytesToWrite -= numWholeBlocksInBytes;
                            CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes));
                            ArrayPool<byte>.Shared.Return(tempOutputBuffer);
                            tempOutputBuffer = null;
                        }
                        catch
                        {
                            CryptographicOperations.ZeroMemory(new Span<byte>(tempOutputBuffer, 0, numOutputBytes));
                            tempOutputBuffer = null;
                            throw;
                        }
                    }
                    else
                    {
                        Debug.Assert(this.outputBuffer != null);

                        // do it the slow way
                        numOutputBytes = TransformBlock(this.transform, buffer.Slice(currentInputIndex, this.inputBlockSize), this.outputBuffer, 0);

                        if (useAsync)
                            await this.stream.WriteAsync(new ReadOnlyMemory<byte>(this.outputBuffer, 0, numOutputBytes), cancellationToken).ConfigureAwait(false);
                        else
                            this.stream.Write(this.outputBuffer, 0, numOutputBytes);

                        currentInputIndex += this.inputBlockSize;
                        bytesToWrite -= this.inputBlockSize;
                    }
                }
                else
                {
                    Debug.Assert(this.inputBuffer != null);

                    // In this case, we don't have an entire block's worth left, so store it up in the input buffer, which by now must be empty.
                    buffer.Slice(currentInputIndex, bytesToWrite).CopyTo(this.inputBuffer);
                    this.inputBufferIndex += bytesToWrite;
                    return;
                }
            }
            return;

            unsafe static int TransformBlock(ICryptoTransform transform, ReadOnlyMemory<byte> inputBuffer, byte[] outputBuffer, int outputOffset)
            {
                if (MemoryMarshal.TryGetArray(inputBuffer, out ArraySegment<byte> segment))
                {
                    // Skip the copy if readonlymemory is actually an array.
                    Debug.Assert(segment.Array is not null);
                    return transform.TransformBlock(segment.Array, segment.Offset, inputBuffer.Length, outputBuffer, outputOffset);
                }
                else
                {
                    // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
                    byte[]? rentedBuffer = ArrayPool<byte>.Shared.Rent(inputBuffer.Length);
                    int result = default;

                    // Pin the rented buffer for security.
                    fixed (byte* _ = &rentedBuffer[0])
                    {
                        try
                        {
                            inputBuffer.CopyTo(rentedBuffer);
                            result = transform.TransformBlock(rentedBuffer, 0, inputBuffer.Length, outputBuffer, outputOffset);
                        }
                        finally
                        {
                            CryptographicOperations.ZeroMemory(rentedBuffer.AsSpan(0, inputBuffer.Length));
                        }
                    }

                    ArrayPool<byte>.Shared.Return(rentedBuffer);
                    rentedBuffer = null;
                    return result;
                }
            }
        }

        private async ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            // To avoid a race with a stream's position pointer & generating race conditions with internal buffer indexes in our own streams
            // that don't natively support async IO operations when there are multiple async requests outstanding, we will block the
            // application's main thread if it does a second IO request until the first one completes.

            await AsyncActiveSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await WriteAsyncCore(buffer, cancellationToken, useAsync: true).ConfigureAwait(false);
            }
            finally
            {
                this.lazyAsyncActiveSemaphore.Release();
            }
        }
    }
    */
}