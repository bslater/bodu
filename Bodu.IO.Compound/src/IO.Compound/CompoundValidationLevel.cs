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
/// All levels enforce the memory-safety invariants required to parse a file without faulting — the signature,
/// byte-order marker, sector sizes, file truncation, allocation-table bounds, and the presence of a root storage are
/// always validated. The level controls only how the reader responds to the remaining, recoverable inconsistencies.
/// </remarks>
public enum CompoundValidationLevel
{
    /// <summary>
    /// Rejects every non-conformant condition, including directory entries with an invalid name length or entry type
    /// that more lenient levels silently skip.
    /// </summary>
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
