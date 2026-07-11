// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReadStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Specifies how the compound-file reader sources the bytes of an opened container.
/// </summary>
/// <remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Bound memory for a large container: read sectors on demand instead of buffering it all.
/// using var file = CompoundFile.OpenRead("large-archive.msg", new CompoundFileOptions
/// {
///     ReadStrategy = CompoundReadStrategy.Streaming,
/// });
///
/// // Or let the reader pick per source: Buffered below MaxBufferedBytes, Streaming above it.
/// var options = new CompoundFileOptions { ReadStrategy = CompoundReadStrategy.Auto };
///]]>
/// </code>
/// </example>
/// </remarks>
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
