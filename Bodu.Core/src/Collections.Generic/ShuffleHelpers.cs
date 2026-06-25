// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShuffleHelpers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

/// <summary>
/// Provides in-place and yield-based Fisher–Yates randomization over arrays, spans, memory regions, and arbitrary
/// <see cref="IEnumerable{T}" /> sources.
/// </summary>
/// <remarks>
/// <para>
/// The helpers implement the canonical Fisher–Yates (Knuth) shuffle: each position from the end of the live region down
/// to index 1 swaps with a uniformly chosen earlier position. The resulting permutation is uniform when the supplied
/// <see cref="IRandomGenerator" /> produces uniform draws over <c>[0, exclusiveMax)</c>.
/// </para>
/// <para>
/// Two surface shapes are exposed. <c>Shuffle</c> mutates the supplied buffer in place and runs in O(n) time with no
/// extra allocation. <c>ShuffleAndYield</c> produces a deferred sequence of the shuffled elements; the <c>count</c>
/// overloads short-circuit after the first <c>count</c> draws and are equivalent to a partial Fisher–Yates — useful for
/// sampling-without-replacement when only a prefix of the shuffle is needed.
/// </para>
/// <para>
/// Randomness quality is delegated entirely to the caller-supplied <see cref="IRandomGenerator" />. For deterministic
/// shuffles in tests pass a seeded generator (such as <see cref="XorShiftRandom" /> with a fixed seed); for production
/// shuffling pass any non-cryptographic generator. None of these helpers are suitable for use in cryptographic
/// permutations, key shuffling, or any context where an attacker can observe outputs and recover state.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Deterministic shuffle of an array — useful for tests and reproducible demos.
/// var deck = Enumerable.Range(1, 52).ToArray();
/// IRandomGenerator rng = new XorShiftRandom(seed: 42);
/// ShuffleHelpers.Shuffle(deck, rng);
///
/// // Sample five cards without replacement using the yield-based overload.
/// foreach (int card in ShuffleHelpers.ShuffleAndYield(deck, rng, count: 5))
///     Console.WriteLine(card);
///]]>
/// </code>
/// </example>
public static partial class ShuffleHelpers
{ }
