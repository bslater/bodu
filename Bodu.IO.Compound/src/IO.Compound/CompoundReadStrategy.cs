// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReadStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Specifies how the compound-file reader sources the bytes of an opened container.
/// </summary>
public enum CompoundReadStrategy
{
    /// <summary>
    /// The default: read the whole file into memory at open time, so access never touches the original source
    /// afterward.
    /// </summary>
    Buffered,

    /// <summary>
    /// Read sectors on demand from a seekable source, bounding memory for large files. The source must remain open and
    /// unmodified for the lifetime of the returned file.
    /// </summary>
    Streaming,

    /// <summary>
    /// Choose <see cref="Buffered" /> for small sources and <see cref="Streaming" /> for large ones, comparing a
    /// seekable source's length against <see cref="CompoundFileOptions.MaxBufferedBytes" />.
    /// </summary>
    Auto,
}
