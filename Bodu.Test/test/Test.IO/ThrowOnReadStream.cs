// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowOnReadStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.IO;

/// <summary>
/// A readable stream whose every read attempt throws and which records whether a read was attempted.
/// </summary>
/// <remarks>
/// <para>
/// The stream serves two complementary scenarios: asserting that a consumer never reads its input (via
/// <see cref="WasRead" />), and driving a consumer's read-failure (catch) path. By default each read throws
/// <see cref="InvalidOperationException" />; supply an exception factory to the constructor to throw a different
/// exception type — for example <see cref="IOException" /> — when exercising error-propagation paths.
/// </para>
/// <para>
/// Every read overload — synchronous and both asynchronous forms — sets <see cref="WasRead" /> before throwing, so the
/// flag reflects an attempt through any surface.
/// </para>
/// <para>
/// This class is intended exclusively for test harness use and must not appear in production code.
/// </para>
/// </remarks>
public sealed class ThrowOnReadStream
    : System.IO.Stream
{
    private readonly Func<Exception> _exceptionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrowOnReadStream" /> class whose reads throw the exception
    /// produced by <paramref name="exceptionFactory" />.
    /// </summary>
    /// <param name="exceptionFactory">
    /// A factory invoked to produce the exception thrown by each read attempt, or <see langword="null" /> to throw
    /// <see cref="InvalidOperationException" />.
    /// </param>
    public ThrowOnReadStream(Func<Exception>? exceptionFactory = null)
    {
        _exceptionFactory = exceptionFactory ?? CreateDefaultException;
    }

    /// <summary>
    /// Gets a value indicating whether a read operation was attempted.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if any read overload was invoked; otherwise, <see langword="false" />.
    /// </returns>
    public bool WasRead { get; private set; }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream has no length.</exception>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown on set — the stream is not seekable.</exception>
    public override long Position
    {
        get => 0;
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        WasRead = true;
        throw _exceptionFactory();
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        WasRead = true;
        return Task.FromException<int>(_exceptionFactory());
    }

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        WasRead = true;
        return ValueTask.FromException<int>(_exceptionFactory());
    }

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream is not seekable.</exception>
    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream is not resizable.</exception>
    public override void SetLength(long value) =>
        throw new NotSupportedException();

    /// <inheritdoc />
    /// <exception cref="NotSupportedException">Always thrown — the stream is not writable.</exception>
    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    private static Exception CreateDefaultException() =>
        new InvalidOperationException("The stream should not be read.");
}
