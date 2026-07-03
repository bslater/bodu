// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumerableExtensions.ZipLongest.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Extensions;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Zips two sequences into <c>(First, Second)</c> tuples, continuing until both are exhausted and padding the
    /// shorter sequence with <see langword="default" /> values.
    /// </summary>
    /// <typeparam name="TFirst">The type of elements in the first sequence.</typeparam>
    /// <typeparam name="TSecond">The type of elements in the second sequence.</typeparam>
    /// <param name="first">The first sequence to zip.</param>
    /// <param name="second">The second sequence to zip.</param>
    /// <returns>
    /// A sequence of <c>(First, Second)</c> tuples whose length equals the longer of the two inputs. Once one sequence
    /// is exhausted its side is filled with <see langword="default" /> until the other is exhausted.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method uses deferred execution and enumerates each sequence exactly once, disposing both enumerators.
    /// Unlike <see cref="System.Linq.Enumerable.Zip{TFirst, TSecond}(IEnumerable{TFirst}, IEnumerable{TSecond})" />,
    /// which stops at the shorter input, this method continues until both inputs are exhausted.
    /// </para>
    /// <para>
    /// This overload pads with <c>default(TFirst)</c> and <c>default(TSecond)</c>, which is <see langword="null" /> for
    /// reference types. Use the overload that accepts explicit defaults to control the padding value.
    /// </para>
    /// <para>
    /// If one input is infinite, the result is infinite; bound it with
    /// <see cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)" /> when zipping an unbounded source.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Zip continues to the longer input; the exhausted side is padded with default(T).
    /// foreach (var (number, letter) in new[] { 1, 2, 3 }.ZipLongest(new[] { "a" }))
    ///     Console.WriteLine($"{number}:{letter ?? "<null>"}");
    /// // => 1:a
    /// // => 2:<null>
    /// // => 3:<null>
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<(TFirst First, TSecond Second)> ZipLongest<TFirst, TSecond>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second) =>
        first.ZipLongest(second, default!, default!);

    /// <summary>
    /// Zips two sequences into <c>(First, Second)</c> tuples, continuing until both are exhausted and padding the
    /// shorter sequence with the supplied default values.
    /// </summary>
    /// <typeparam name="TFirst">The type of elements in the first sequence.</typeparam>
    /// <typeparam name="TSecond">The type of elements in the second sequence.</typeparam>
    /// <param name="first">The first sequence to zip.</param>
    /// <param name="second">The second sequence to zip.</param>
    /// <param name="firstDefault">
    /// The value substituted for the first side once <paramref name="first" /> is exhausted.
    /// </param>
    /// <param name="secondDefault">
    /// The value substituted for the second side once <paramref name="second" /> is exhausted.
    /// </param>
    /// <returns>
    /// A sequence of <c>(First, Second)</c> tuples whose length equals the longer of the two inputs, padding the
    /// exhausted side with the corresponding supplied default.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="first" /> or <paramref name="second" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This method uses deferred execution and enumerates each sequence exactly once, disposing both enumerators.
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Supply explicit padding values used once a side is exhausted.
    /// var result = new[] { 1 }.ZipLongest(new[] { "a", "b" }, firstDefault: -1, secondDefault: "?").ToArray();
    /// // => result == [ (1, "a"), (-1, "b") ]
    ///]]>
    /// </code>
    /// </example>
    public static IEnumerable<(TFirst First, TSecond Second)> ZipLongest<TFirst, TSecond>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        TFirst firstDefault,
        TSecond secondDefault) =>
        first.ZipLongest(second, firstDefault, secondDefault, static (x, y) => (x, y));

    /// <summary>
    /// Zips two sequences with a projection, continuing until both are exhausted and padding the shorter sequence with
    /// the supplied default values.
    /// </summary>
    /// <typeparam name="TFirst">The type of elements in the first sequence.</typeparam>
    /// <typeparam name="TSecond">The type of elements in the second sequence.</typeparam>
    /// <typeparam name="TResult">The type of the projected result.</typeparam>
    /// <param name="first">The first sequence to zip.</param>
    /// <param name="second">The second sequence to zip.</param>
    /// <param name="firstDefault">
    /// The value substituted for the first side once <paramref name="first" /> is exhausted.
    /// </param>
    /// <param name="secondDefault">
    /// The value substituted for the second side once <paramref name="second" /> is exhausted.
    /// </param>
    /// <param name="selector">A projection applied to each pair of first and second elements.</param>
    /// <returns>
    /// A sequence containing the result of <paramref name="selector" /> applied to each pair, whose length equals the
    /// longer of the two inputs, padding the exhausted side with the corresponding supplied default.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="first" />, <paramref name="second" />, or <paramref name="selector" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// This method uses deferred execution and enumerates each sequence exactly once, disposing both enumerators.
    /// <paramref name="selector" /> is invoked once per emitted element.
    /// </remarks>
    public static IEnumerable<TResult> ZipLongest<TFirst, TSecond, TResult>(
        this IEnumerable<TFirst> first,
        IEnumerable<TSecond> second,
        TFirst firstDefault,
        TSecond secondDefault,
        Func<TFirst, TSecond, TResult> selector)
    {
        ThrowHelper.ThrowIfNull(first);
        ThrowHelper.ThrowIfNull(second);
        ThrowHelper.ThrowIfNull(selector);

        return Iterator();

        IEnumerable<TResult> Iterator()
        {
            using IEnumerator<TFirst> firstEnumerator = first.GetEnumerator();
            using IEnumerator<TSecond> secondEnumerator = second.GetEnumerator();

            bool hasFirst;
            bool hasSecond;

            // Bitwise '|' (not '||') is intentional so both enumerators advance on every iteration while either may
            // still have elements; short-circuiting would leave one side stranded once the other is exhausted.
            while ((hasFirst = firstEnumerator.MoveNext()) | (hasSecond = secondEnumerator.MoveNext()))
            {
                TFirst firstValue = hasFirst ? firstEnumerator.Current : firstDefault;
                TSecond secondValue = hasSecond ? secondEnumerator.Current : secondDefault;
                yield return selector(firstValue, secondValue);
            }
        }
    }
}
