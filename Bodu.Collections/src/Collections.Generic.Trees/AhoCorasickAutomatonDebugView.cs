// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickAutomatonDebugView.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Collections.Generic.Trees;

/// <summary>
/// Provides a debugger-friendly view of an <see cref="AhoCorasickAutomaton" />, rendering its patterns as a flat array.
/// </summary>
internal sealed class AhoCorasickAutomatonDebugView
{
    /// <summary>The automaton whose patterns are surfaced in the debugger.</summary>
    private readonly AhoCorasickAutomaton _automaton;

    /// <summary>
    /// Initializes a new instance of the <see cref="AhoCorasickAutomatonDebugView" /> class.
    /// </summary>
    /// <param name="automaton">The automaton to surface in the debugger.</param>
    /// <exception cref="ArgumentNullException"><paramref name="automaton" /> is <see langword="null" />.</exception>
    public AhoCorasickAutomatonDebugView(AhoCorasickAutomaton automaton)
    {
        _automaton = automaton ?? throw new ArgumentNullException(nameof(automaton));
    }

    /// <summary>
    /// Gets a snapshot of the automaton's patterns.
    /// </summary>
    /// <value>An array of the automaton's distinct patterns, in first-seen order.</value>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public string[] Items => _automaton.Patterns.ToArray();
}
