// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferCapacityKat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Buffers;

/// <summary>
/// Represents a known-answer test row for pooled-buffer capacity behaviour: a starting capacity, an append size that
/// will (or will not) provoke growth, and the expected post-append capacity invariant (<c>&gt;=</c> the requested
/// element count).
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="InitialCapacity">The capacity the buffer is constructed with.</param>
/// <param name="AppendCount">The number of elements appended.</param>
/// <param name="ExpectsGrowth">Whether the append is expected to grow the underlying buffer.</param>
public sealed record BufferCapacityKat(
    string Name,
    int InitialCapacity,
    int AppendCount,
    bool ExpectsGrowth) : IKat;
