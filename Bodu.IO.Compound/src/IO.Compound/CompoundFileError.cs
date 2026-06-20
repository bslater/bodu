// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileError.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Categorizes the structural failure that caused a <see cref="CompoundFileFormatException" />.
/// </summary>
/// <remarks>
/// The category provides a stable, message-independent way to reason about why a compound file was rejected, which is
/// especially useful when validating a reader against a corpus of deliberately malformed files.
/// </remarks>
public enum CompoundFileError
{
    /// <summary>
    /// The failure does not fall into a more specific category.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The data does not begin with the compound-file signature.
    /// </summary>
    InvalidSignature,

    /// <summary>
    /// The header byte-order marker is not the expected little-endian value.
    /// </summary>
    InvalidByteOrder,

    /// <summary>
    /// The header declares an unsupported sector or mini-sector size.
    /// </summary>
    InvalidSectorSize,

    /// <summary>
    /// The header is otherwise malformed or declares an unsupported layout.
    /// </summary>
    InvalidHeader,

    /// <summary>
    /// The data is shorter than the structure it declares.
    /// </summary>
    TruncatedFile,

    /// <summary>
    /// The double-indirect file-allocation table (DIFAT) is malformed.
    /// </summary>
    InvalidDifat,

    /// <summary>
    /// A sector chain references a sector outside the file.
    /// </summary>
    SectorOutOfRange,

    /// <summary>
    /// A regular file-allocation table (FAT) chain forms a cycle.
    /// </summary>
    FatCycle,

    /// <summary>
    /// A mini-FAT chain is malformed or references an out-of-range mini sector.
    /// </summary>
    InvalidMiniFat,

    /// <summary>
    /// A materialized sector chain is shorter than the declared stream size.
    /// </summary>
    StreamChainTooShort,

    /// <summary>
    /// The directory stream is malformed.
    /// </summary>
    InvalidDirectory,

    /// <summary>
    /// The directory tree contains a cyclic or duplicated entry link.
    /// </summary>
    DirectoryCycle,

    /// <summary>
    /// The root storage entry is missing or has the wrong type.
    /// </summary>
    InvalidRootStorage,

    /// <summary>
    /// A stream is not a well-formed OLE property set.
    /// </summary>
    InvalidPropertySet,
}
