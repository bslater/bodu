// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Base class for <see cref="StreamExtensions" /> tests. Provides the non-seekable stream fixture used to exercise
/// the growable-buffer code path.
/// </summary>
[TestClass]
public partial class StreamExtensionsTests
{
    /// <summary>
    /// A <see cref="MemoryStream" /> that reports itself as non-seekable, forcing the <see cref="StreamExtensions" />
    /// read helpers down their growable-buffer path while still serving content from memory.
    /// </summary>
    private sealed class NonSeekableMemoryStream : MemoryStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonSeekableMemoryStream" /> class over the supplied bytes.
        /// </summary>
        /// <param name="buffer">The backing content exposed by the stream.</param>
        public NonSeekableMemoryStream(byte[] buffer)
            : base(buffer)
        {
        }

        /// <inheritdoc />
        public override bool CanSeek => false;
    }
}
