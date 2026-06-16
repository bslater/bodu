// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGenerator.ThueMorse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Extensions;

public static partial class SequenceGenerator
{
    /// <summary>
    /// Yields the first <paramref name="count" /> terms of the Thue–Morse sequence as a stream of <c>0</c>s and
    /// <c>1</c>s.
    /// </summary>
    /// <param name="count">The number of terms to emit. Must be non-negative.</param>
    /// <returns>
    /// A lazily evaluated, finite sequence of <see cref="int" /> values, each equal to <c>0</c> or <c>1</c>, where
    /// element <c>n</c> is the parity of the number of set bits in the binary representation of <c>n</c>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count" /> is negative.</exception>
    /// <remarks>
    /// <para>
    /// Use this generator to drive cube-free sequence demonstrations, fair-division algorithms, or anywhere a
    /// deterministic but non-periodic binary feed is useful. Compared to building the sequence by repeated bitwise
    /// complement and concatenation, this implementation uses a per-index <c>popcount</c> reduction so it can produce a
    /// single index without first materializing the preceding <c>n</c> bits.
    /// </para>
    /// <para>
    /// A <paramref name="count" /> of <c>0</c> returns an empty sequence. The sequence is finite and deterministic —
    /// the same input always produces the same output. Iteration is deferred and carries no allocations beyond the
    /// iterator state.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// var prefix = SequenceGenerator.ThueMorse(16).ToArray(); // => [0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0]
    ///
    /// // Convenient as the schedule for a fair turn-taking algorithm:
    /// // player A goes on 0, player B goes on 1.
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<int> ThueMorse(int count)
    {
        ThrowHelper.ThrowIfLessThan(count, 0);

        for (int i = 0; i < count; i++)
        {
            int parity = 0, n = i;
            while (n > 0)
            {
                parity ^= n & 1;
                n >>= 1;
            }

            yield return parity;
        }
    }
}
