// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncReaderWriterLock.Releaser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncReaderWriterLock
{
    /// <summary>
    /// Represents acquired access to an <see cref="AsyncReaderWriterLock" />. Disposing the releaser releases the
    /// read or write access it represents.
    /// </summary>
    /// <remarks>
    /// The releaser is a lightweight value type returned by <see cref="ReaderAsync()" /> and <see cref="WriterAsync()" />.
    /// It is intended to be consumed by a single <c>using</c> statement and disposed exactly once. Copying a releaser
    /// and disposing more than one copy releases the access more than once and is undefined behavior, mirroring the
    /// contract of the framework's value-type enumerators.
    /// </remarks>
    public readonly struct Releaser : IDisposable
    {
        private readonly AsyncReaderWriterLock? _owner;
        private readonly bool _isWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="Releaser" /> struct bound to the specified owner.
        /// </summary>
        /// <param name="owner">The lock that was acquired.</param>
        /// <param name="isWriter"><see langword="true" /> if the releaser represents write access; otherwise, <see langword="false" />.</param>
        internal Releaser(AsyncReaderWriterLock owner, bool isWriter)
        {
            _owner = owner;
            _isWriter = isWriter;
        }

        /// <summary>
        /// Releases the read or write access represented by this releaser.
        /// </summary>
        public void Dispose()
        {
            if (_owner is null)
                return;

            if (_isWriter)
                _owner.ReleaseWriter();
            else
                _owner.ReleaseReader();
        }
    }
}
