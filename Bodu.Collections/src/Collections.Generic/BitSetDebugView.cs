// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BitSetDebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides a debugger-friendly view of a <see cref="BitSet" />, showing the indices of the set bits in ascending
/// order.
/// </summary>
internal sealed class BitSetDebugView
{
    /// <summary>The <see cref="BitSet" /> instance being debugged.</summary>
    private readonly BitSet _bitSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="BitSetDebugView" /> class.
    /// </summary>
    /// <param name="bitSet">The set instance to inspect. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException"><paramref name="bitSet" /> is <see langword="null" />.</exception>
    public BitSetDebugView(BitSet bitSet)
    {
        _bitSet = bitSet ?? throw new ArgumentNullException(nameof(bitSet));
    }

    /// <summary>
    /// Gets the indices of the set bits, displayed in the debugger as root-level entries.
    /// </summary>
    /// <remarks>
    /// Shown in the debugger as root-level entries for quick inspection.
    /// </remarks>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public int[] SetBits =>
        [.. _bitSet];
}
