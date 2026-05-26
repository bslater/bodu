// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferWriteKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a pooled-buffer write scenario: a starting capacity, an
/// append payload, and the expected final element count.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="InitialCapacity">The capacity the buffer is constructed with.</param>
/// <param name="Append">The element payload appended to the buffer.</param>
/// <param name="ExpectedCount">The expected element count after the append completes.</param>
public sealed record BufferWriteKat(
    string Name,
    int InitialCapacity,
    int[] Append,
    int ExpectedCount) : IKat;
