// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2Core.Geometry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal static partial class Argon2Core
{
    /// <summary>
    /// Describes the shape of one derivation's memory matrix and the schedule that fills it (RFC 9106, Section 3.2).
    /// </summary>
    private readonly struct Geometry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Geometry" /> struct for the specified variant and parameters.
        /// </summary>
        /// <param name="type">The Argon2 variant.</param>
        /// <param name="parameters">The validated cost parameters.</param>
        internal Geometry(Argon2Type type, Argon2Parameters parameters)
        {
            Type = type;
            Lanes = parameters.Parallelism;

            // m' is m rounded down to the nearest multiple of 4*p (RFC 9106 Section 3.2).
            MemoryBlocks = SyncPoints * Lanes * (parameters.MemoryKiB / (SyncPoints * Lanes));
            LaneLength = MemoryBlocks / Lanes;
            SegmentLength = LaneLength / SyncPoints;
            Passes = parameters.Iterations;
            Version = parameters.Version;
        }

        /// <summary>
        /// Gets the Argon2 variant, which selects data-dependent or data-independent indexing.
        /// </summary>
        internal Argon2Type Type { get; }

        /// <summary>
        /// Gets the number of lanes, <c>p</c>.
        /// </summary>
        internal int Lanes { get; }

        /// <summary>
        /// Gets the number of blocks in the matrix, <c>m'</c>.
        /// </summary>
        internal int MemoryBlocks { get; }

        /// <summary>
        /// Gets the number of blocks in each lane, <c>q</c>.
        /// </summary>
        internal int LaneLength { get; }

        /// <summary>
        /// Gets the number of blocks in each segment - one lane's share of one slice.
        /// </summary>
        internal int SegmentLength { get; }

        /// <summary>
        /// Gets the number of passes over the matrix, <c>t</c>.
        /// </summary>
        internal int Passes { get; }

        /// <summary>
        /// Gets the Argon2 version code, which decides whether later passes overwrite or XOR.
        /// </summary>
        internal int Version { get; }
    }
}
