// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLock.Releaser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLock
{
    /// <summary>
    /// Represents acquired access to an <see cref="AsyncReaderWriterLock" />. Disposing the releaser releases the read
    /// or write access it represents.
    /// </summary>
    /// <remarks>
    /// The releaser is a lightweight value type returned by <see cref="ReaderAsync()" /> and
    /// <see cref="WriterAsync()" />, typically consumed by a single <c>using</c> statement. It is <b>idempotent</b>:
    /// disposing it more than once, or disposing several copies of the same value, releases the underlying access
    /// exactly once. This prevents accidental double-disposal from corrupting the reader count or granting overlapping
    /// access, at the cost of one small allocation per acquisition for the shared idempotency guard.
    /// </remarks>
    public readonly struct Releaser
        : IDisposable
    {
        /// <summary>The lock to release on disposal, or <see langword="null" /> for a default releaser.</summary>
        private readonly AsyncReaderWriterLock? _owner;

        /// <summary>The shared guard ensuring the access is released at most once across releaser copies.</summary>
        private readonly ReleaseGuard? _guard;

        /// <summary>Indicates whether this releaser represents write access rather than read access.</summary>
        private readonly bool _isWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Releaser" /> struct bound to the specified owner.
        /// </summary>
        /// <param name="owner">The lock that was acquired.</param>
        /// <param name="isWriter">
        /// <see langword="true" /> if the releaser represents write access; otherwise, <see langword="false" />.
        /// </param>
        /// <param name="guard">The shared guard that ensures the access is released at most once.</param>
        internal Releaser(AsyncReaderWriterLock owner, bool isWriter, ReleaseGuard guard)
        {
            _owner = owner;
            _isWriter = isWriter;
            _guard = guard;
        }

        /// <summary>
        /// Releases the read or write access represented by this releaser. Subsequent calls, including calls on copies
        /// of the same value, are no-ops.
        /// </summary>
        public void Dispose()
        {
            if (_owner is null || _guard?.TryRelease() != true)
                return;

            if (_isWriter)
                _owner.ReleaseWriter();
            else
                _owner.ReleaseReader();
        }

        /// <summary>
        /// A one-shot guard shared by every copy of a <see cref="Releaser" /> value, used to release the underlying
        /// access exactly once regardless of how many times the releaser is disposed.
        /// </summary>
        internal sealed class ReleaseGuard
        {
            /// <summary>The release state: <c>0</c> while held, <c>1</c> once released.</summary>
            private int _released;

            /// <summary>
            /// Atomically claims the single release permitted by this guard.
            /// </summary>
            /// <returns>
            /// <see langword="true" /> for the first caller; <see langword="false" /> for every subsequent caller.
            /// </returns>
            internal bool TryRelease() =>
                Interlocked.Exchange(ref _released, 1) == 0;
        }
    }
}
