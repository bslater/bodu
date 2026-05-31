// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IRandomGenerator.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

/// <summary>
/// Defines the minimal contract for a pluggable, non-cryptographic random-integer source used by shuffle, sampling, and
/// randomized-collection helpers throughout the library.
/// </summary>
/// <remarks>
/// <para>
/// The interface exists as a seam between consumers (helpers such as <c>IListExtensions.Shuffle</c> and
/// <c>IEnumerableExtensions.Randomize</c>) and the underlying random source. By depending on the abstraction rather
/// than on a concrete implementation, callers can substitute a deterministic generator in tests, supply a thread-local
/// generator under contention, or plug in a domain-specific PRNG without touching the helpers.
/// </para>
/// <para>
/// Two implementations ship with the library: <see cref="XorShiftRandom" /> — a fast, lock-free xorshift generator
/// suitable for tight inner loops — and <c>SystemRandomAdapter</c>, which wraps <see cref="System.Random" /> for
/// callers that prefer the BCL distribution and seeding semantics. Either is appropriate for shuffle and sampling work;
/// neither is suitable for cryptographic use. Consumers that require cryptographic randomness should use
/// <see cref="System.Security.Cryptography.RandomNumberGenerator" /> directly.
/// </para>
/// <para>
/// Implementations are not required to be thread-safe unless documented otherwise. Helpers that consume an
/// <see cref="IRandomGenerator" /> from multiple threads must either choose a thread-safe implementation or wrap access
/// externally.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Substitute a deterministic generator in tests.
/// IRandomGenerator rng = new XorShiftRandom(seed: 42);
/// int dieRoll = rng.Next(6) + 1; // value in [1, 6]
///]]>
/// </example>
public interface IRandomGenerator
{
    /// <summary>
    /// Returns a non-negative random integer that is less than the specified maximum.
    /// </summary>
    /// <param name="maxValue">The exclusive upper bound.</param>
    /// <returns>A random integer in the range [0, maxValue).</returns>
    int Next(int maxValue);
}
