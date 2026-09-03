// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileError.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Categorizes the failure a <see cref="PstFileException" /> reports, so callers can distinguish a missing object from
/// structural corruption without parsing messages.
/// </summary>
public enum PstFileError
{
    /// <summary>No category was recorded.</summary>
    None = 0,

    /// <summary>The file header is malformed.</summary>
    InvalidHeader,

    /// <summary>The file uses a recognized but unsupported variant.</summary>
    UnsupportedFormat,

    /// <summary>A B-tree page is malformed or fails validation.</summary>
    InvalidPage,

    /// <summary>A block's geometry or trailer is invalid, or a read escapes the file.</summary>
    InvalidBlock,

    /// <summary>A multi-block data tree is malformed or references a missing block.</summary>
    InvalidDataTree,

    /// <summary>A subnode tree is malformed or references a missing subnode.</summary>
    InvalidSubnodeTree,

    /// <summary>A heap-on-node structure is malformed.</summary>
    InvalidHeap,

    /// <summary>A property context is malformed.</summary>
    InvalidPropertyContext,

    /// <summary>A table context is malformed.</summary>
    InvalidTableContext,

    /// <summary>A property value reference does not resolve.</summary>
    InvalidPropertyValue,

    /// <summary>The requested node does not exist.</summary>
    NodeNotFound,

    /// <summary>The requested property does not exist.</summary>
    PropertyNotFound,

    /// <summary>
    /// A structure's declared size or fan-out exceeds a resource limit the session was opened with (see
    /// <see cref="PstFileOptions.MaxNodeDataLength" /> and <see cref="PstFileOptions.MaxDataTreeLeaves" />).
    /// </summary>
    LimitExceeded,
}
