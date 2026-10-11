// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundValidationLevel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Specifies how strictly the compound-file reader validates a container's structure.
/// </summary>
/// <remarks>
/// All levels enforce the memory-safety invariants required to parse a file without faulting - the signature,
/// byte-order marker, sector sizes, file truncation, allocation-table bounds, and the presence of a root storage are
/// always validated. The level controls only how the reader responds to the remaining, recoverable inconsistencies.
/// Every level reads a stream smaller than 4096 bytes from the mini stream, the cutoff [MS-CFB] fixes, whatever cutoff
/// the header declares.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Salvage what a damaged legacy file still holds instead of rejecting it outright.
/// using var salvage = CompoundFile.OpenRead("damaged.doc", new CompoundFileOptions
/// {
///     ValidationLevel = CompoundValidationLevel.Minimal,
/// });
///
/// // Conformance tooling wants the opposite: fail on anything non-conformant.
/// var strict = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Strict };
///]]>
/// </code>
/// </example>
/// </remarks>
public enum CompoundValidationLevel
{
    /// <summary>
    /// Rejects every non-conformant condition, including directory entries with an invalid name length or entry type
    /// that more lenient levels silently skip.
    /// </summary>
    /// <remarks>
    /// The header must hold the values [MS-CFB] fixes, or the file is rejected with
    /// <see cref="CompoundFileError.InvalidHeader" />: a zero class identifier and zero reserved bytes, major version 3
    /// with 512-byte sectors or 4 with 4096-byte sectors, no directory sectors counted in version 3 and the length of
    /// the directory chain counted in version 4, and a mini stream cutoff of 4096.
    /// </remarks>
    Strict,

    /// <summary>
    /// The default: rejects structural corruption (allocation-table cycles, out-of-range or short sector chains, and
    /// cyclic directory links) while tolerating individually malformed directory entries by skipping them.
    /// </summary>
    Compatible,

    /// <summary>
    /// Recovers from structural corruption where it can: cyclic, out-of-range, or short sector chains and cyclic
    /// directory links stop and yield the data collected so far instead of throwing.
    /// </summary>
    Minimal,
}
