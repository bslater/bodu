// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedSerializerTests.LongLengthStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public partial class DelimitedSerializerTests
{
    /// <summary>
    /// A <see cref="MemoryStream" /> that holds a small content but reports a <see cref="Length" /> of
    /// <see cref="int.MaxValue" /> + 1, as the stream of a file larger than 2 GiB does.
    /// </summary>
    /// <remarks>
    /// Reads return the real content and then zero, so a consumer that sizes a buffer from <see cref="Length" /> or
    /// narrows it to <see cref="int" /> fails, while one that reads to the end does not.
    /// </remarks>
    private sealed class LongLengthStream
        : MemoryStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LongLengthStream" /> class.
        /// </summary>
        /// <param name="content">The content the stream returns.</param>
        public LongLengthStream(byte[] content)
            : base(content, writable: false)
        {
        }

        /// <inheritdoc />
        /// <value><see cref="int.MaxValue" /> + 1, whatever the content holds.</value>
        public override long Length => int.MaxValue + 1L;
    }
}
