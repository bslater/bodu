// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AhoCorasickCore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Internal;

/// <summary>
/// Represents a single state in an Aho-Corasick automaton, holding its goto transitions, failure link, output link, and
/// — when the state terminates a pattern — the pattern and its associated value.
/// </summary>
/// <typeparam name="TValue">The type of value stored at pattern-terminating states.</typeparam>
internal sealed class AhoCorasickNode<TValue>
{
    /// <summary>
    /// Gets or sets the goto transitions keyed by character, or <see langword="null" /> when the state is a leaf.
    /// </summary>
    public Dictionary<char, AhoCorasickNode<TValue>>? Children { get; set; }

    /// <summary>
    /// Gets or sets the failure link — the state for the longest proper suffix of this state's path that is itself a
    /// path in the goto trie. <see langword="null" /> only on the root.
    /// </summary>
    public AhoCorasickNode<TValue>? Fail { get; set; }

    /// <summary>
    /// Gets or sets the output link — the nearest state on the failure chain that terminates a pattern, or
    /// <see langword="null" /> when no proper suffix of this state's path is a pattern.
    /// </summary>
    public AhoCorasickNode<TValue>? Output { get; set; }

    /// <summary>
    /// Gets or sets the pattern terminating at this state, or <see langword="null" /> when the state is not terminal.
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the value associated with the pattern terminating at this state.
    /// </summary>
    public TValue Value { get; set; } = default!;
}

/// <summary>
/// Provides the shared goto-trie construction, breadth-first link building, and state-transition logic for the
/// Aho-Corasick automaton types, operating over a generic <see cref="AhoCorasickNode{TValue}" /> so the unkeyed and
/// keyed automatons reuse a single implementation.
/// </summary>
/// <remarks>
/// Character transitions are ordinal — the automaton types deliberately do not accept a custom
/// <see cref="IEqualityComparer{Char}" />, because a comparer would have to govern failure-link construction as well as
/// matching; callers who need folded matching normalize patterns and text up front.
/// </remarks>
internal static class AhoCorasickCore
{
    /// <summary>
    /// Adds the specified pattern to the goto trie rooted at <paramref name="root" />, creating any missing states.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at pattern-terminating states.</typeparam>
    /// <param name="root">The root state of the goto trie.</param>
    /// <param name="pattern">The pattern whose characters form the path.</param>
    /// <returns>The state at which the pattern terminates.</returns>
    public static AhoCorasickNode<TValue> AddPattern<TValue>(AhoCorasickNode<TValue> root, string pattern)
    {
        AhoCorasickNode<TValue> node = root;
        foreach (char ch in pattern)
        {
            node.Children ??= new Dictionary<char, AhoCorasickNode<TValue>>();
            if (!node.Children.TryGetValue(ch, out AhoCorasickNode<TValue>? next))
            {
                next = new AhoCorasickNode<TValue>();
                node.Children.Add(ch, next);
            }

            node = next;
        }

        return node;
    }

    /// <summary>
    /// Walks the goto trie from the specified root following the characters of <paramref name="pattern" />.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at pattern-terminating states.</typeparam>
    /// <param name="root">The root state to walk from.</param>
    /// <param name="pattern">The character sequence to follow.</param>
    /// <returns>The state reached by the pattern, or <see langword="null" /> if the path does not exist.</returns>
    public static AhoCorasickNode<TValue>? Find<TValue>(AhoCorasickNode<TValue> root, ReadOnlySpan<char> pattern)
    {
        AhoCorasickNode<TValue> node = root;
        foreach (char ch in pattern)
        {
            if (node.Children is null || !node.Children.TryGetValue(ch, out AhoCorasickNode<TValue>? next))
                return null;

            node = next;
        }

        return node;
    }

    /// <summary>
    /// Assigns failure and output links to every state via the classic breadth-first pass over the completed goto trie.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at pattern-terminating states.</typeparam>
    /// <param name="root">The root state of the goto trie.</param>
    /// <remarks>
    /// Links are global invariants of the complete pattern set — this pass must run after every pattern has been added,
    /// which is why the automaton types are immutable once built.
    /// </remarks>
    public static void BuildLinks<TValue>(AhoCorasickNode<TValue> root)
    {
        var queue = new Queue<AhoCorasickNode<TValue>>();

        if (root.Children is not null)
        {
            // Depth-one states fail to the root and can have no pattern-bearing proper suffix.
            foreach (AhoCorasickNode<TValue> child in root.Children.Values)
            {
                child.Fail = root;
                child.Output = null;
                queue.Enqueue(child);
            }
        }

        while (queue.Count > 0)
        {
            AhoCorasickNode<TValue> node = queue.Dequeue();
            if (node.Children is null)
                continue;

            foreach (KeyValuePair<char, AhoCorasickNode<TValue>> transition in node.Children)
            {
                char ch = transition.Key;
                AhoCorasickNode<TValue> child = transition.Value;

                // Follow the parent's failure chain to the deepest state with a matching transition.
                AhoCorasickNode<TValue>? fail = node.Fail;
                AhoCorasickNode<TValue>? target = null;
                while (fail is not null && (fail.Children is null || !fail.Children.TryGetValue(ch, out target)))
                {
                    fail = fail.Fail;
                    target = null;
                }

                child.Fail = target ?? root;
                child.Output = child.Fail.Pattern is not null ? child.Fail : child.Fail.Output;
                queue.Enqueue(child);
            }
        }
    }

    /// <summary>
    /// Advances the automaton by one character, following failure links until a goto transition matches or the root is
    /// reached.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at pattern-terminating states.</typeparam>
    /// <param name="root">The root state of the automaton.</param>
    /// <param name="state">The current state.</param>
    /// <param name="ch">The next text character.</param>
    /// <returns>The state after consuming <paramref name="ch" />.</returns>
    public static AhoCorasickNode<TValue> Step<TValue>(AhoCorasickNode<TValue> root, AhoCorasickNode<TValue> state, char ch)
    {
        while (true)
        {
            if (state.Children is not null && state.Children.TryGetValue(ch, out AhoCorasickNode<TValue>? next))
                return next;

            if (ReferenceEquals(state, root))
                return root;

            state = state.Fail!;
        }
    }

    /// <summary>
    /// Returns the first pattern-terminating state reportable from <paramref name="state" /> — the state itself when
    /// terminal, otherwise its output link.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at pattern-terminating states.</typeparam>
    /// <param name="state">The state reached after consuming a text character.</param>
    /// <returns>
    /// The head of the terminal chain, or <see langword="null" /> when no pattern ends at this position.
    /// </returns>
    public static AhoCorasickNode<TValue>? FirstTerminal<TValue>(AhoCorasickNode<TValue> state) =>
        state.Pattern is not null ? state : state.Output;

    /// <summary>
    /// Counts every pattern occurrence in the specified text.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at pattern-terminating states.</typeparam>
    /// <param name="root">The root state of the automaton.</param>
    /// <param name="text">The text to scan.</param>
    /// <returns>The total number of matches, overlapping and nested occurrences included.</returns>
    public static int CountMatches<TValue>(AhoCorasickNode<TValue> root, ReadOnlySpan<char> text)
    {
        int count = 0;
        AhoCorasickNode<TValue> state = root;
        foreach (char ch in text)
        {
            state = Step(root, state, ch);
            for (AhoCorasickNode<TValue>? terminal = FirstTerminal(state); terminal is not null; terminal = terminal.Output)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Determines whether any pattern occurs in the specified text, exiting at the first hit.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at pattern-terminating states.</typeparam>
    /// <param name="root">The root state of the automaton.</param>
    /// <param name="text">The text to scan.</param>
    /// <returns><see langword="true" /> if at least one pattern occurs; otherwise, <see langword="false" />.</returns>
    public static bool HasMatch<TValue>(AhoCorasickNode<TValue> root, ReadOnlySpan<char> text)
    {
        AhoCorasickNode<TValue> state = root;
        foreach (char ch in text)
        {
            state = Step(root, state, ch);
            if (FirstTerminal(state) is not null)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Collects every pattern-terminating state reportable at the current position into <paramref name="buffer" />,
    /// ordered ascending by pattern length.
    /// </summary>
    /// <typeparam name="TValue">The value type stored at pattern-terminating states.</typeparam>
    /// <param name="state">The state reached after consuming the text character at the current position.</param>
    /// <param name="buffer">The reusable buffer receiving the terminal states; cleared before filling.</param>
    /// <remarks>
    /// The output chain naturally surfaces the longest pattern first (the state's own path, then ever-shorter
    /// suffixes), so the chain is buffered and reversed to honor the documented ascending-length order.
    /// </remarks>
    public static void CollectTerminalsAscending<TValue>(AhoCorasickNode<TValue> state, List<AhoCorasickNode<TValue>> buffer)
    {
        buffer.Clear();
        for (AhoCorasickNode<TValue>? terminal = FirstTerminal(state); terminal is not null; terminal = terminal.Output)
            buffer.Add(terminal);

        buffer.Reverse();
    }
}
