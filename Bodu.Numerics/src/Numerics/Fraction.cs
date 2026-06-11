// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fraction.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Text.Json.Serialization;
using Bodu.Numerics.Serialization;

namespace Bodu.Numerics;

/// <summary>
/// Represents an immutable exact rational number — a ratio of two integers — backed by an arbitrary
/// <see cref="IBinaryInteger{TSelf}" /> component type.
/// </summary>
/// <typeparam name="T">
/// The integer type used to store the numerator and denominator. Use a fixed-width type such as <see cref="int" /> or
/// <see cref="long" /> for compact storage, or <see cref="BigInteger" /> for arithmetic that never overflows.
/// </typeparam>
/// <remarks>
/// <para>
/// A <see cref="Fraction{T}" /> is always held in canonical form: the denominator is strictly positive, the numerator
/// carries the sign, and the two components share no common factor other than one. Equal rational values therefore have
/// identical components and compare and hash equally.
/// </para>
/// <para>
/// Arithmetic is exact. Intermediate results are evaluated with <see cref="BigInteger" /> precision and the canonical
/// result is narrowed back to <typeparamref name="T" />; when the narrowed component does not fit a fixed-width
/// <typeparamref name="T" /> an <see cref="OverflowException" /> is thrown. Selecting <see cref="BigInteger" /> as
/// <typeparamref name="T" /> removes that limit entirely.
/// </para>
/// <para>
/// When <typeparamref name="T" /> is an unsigned integer type, negative rationals cannot be represented; operations
/// that would produce a negative component — such as negating a non-zero value — throw an
/// <see cref="OverflowException" /> at run time.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Construct from components; the value is reduced to canonical form on creation.
/// Fraction<int> oneHalf = new Fraction<int>(2, 4);   // 1/2
/// Fraction<int> oneThird = new Fraction<int>(1, 3);
///
/// // Exact arithmetic through the operators.
/// Fraction<int> sum = oneHalf + oneThird;            // 5/6
///
/// // Parse and format round-trip through the invariant "numerator/denominator" form.
/// Fraction<int> parsed = Fraction<int>.Parse("3/8");
/// string text = parsed.ToString();                   // "3/8"
///]]>
/// </code>
/// </example>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(FractionJsonConverterFactory))]
public readonly partial struct Fraction<T>
    where T : IBinaryInteger<T>
{
    /// <summary>
    /// Indicates whether <typeparamref name="T" /> is a bounded integer type and therefore has a finite range.
    /// </summary>
    private static readonly bool s_isBounded;

    /// <summary>
    /// The smallest value <typeparamref name="T" /> can represent when it is bounded.
    /// </summary>
    private static readonly T s_minBacking;

    /// <summary>
    /// The largest value <typeparamref name="T" /> can represent when it is bounded.
    /// </summary>
    private static readonly T s_maxBacking;

    /// <summary>
    /// <see cref="s_minBacking" /> widened to <see cref="BigInteger" />, cached for the non-throwing narrowing check.
    /// </summary>
    private static readonly BigInteger s_minBackingBig;

    /// <summary>
    /// <see cref="s_maxBacking" /> widened to <see cref="BigInteger" />, cached for the non-throwing narrowing check.
    /// </summary>
    private static readonly BigInteger s_maxBackingBig;

    /// <summary>
    /// The canonical denominator backing <see cref="Denominator" />, or zero for a default-initialized instance.
    /// </summary>
    private readonly T _denominator;

    /// <summary>
    /// Initializes static members of the <see cref="Fraction{T}" /> struct.
    /// </summary>
    static Fraction()
    {
        s_isBounded = TryGetBounds(out s_minBacking, out s_maxBacking);
        if (s_isBounded)
        {
            s_minBackingBig = BigInteger.CreateChecked(s_minBacking);
            s_maxBackingBig = BigInteger.CreateChecked(s_maxBacking);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fraction{T}" /> struct representing the whole number
    /// <paramref name="value" />.
    /// </summary>
    /// <param name="value">The integer value the fraction represents.</param>
    public Fraction(T value)
    {
        Numerator = value;
        _denominator = T.One;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fraction{T}" /> struct from the specified numerator and
    /// denominator, reducing the result to canonical form.
    /// </summary>
    /// <param name="numerator">The numerator of the rational value.</param>
    /// <param name="denominator">The denominator of the rational value.</param>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="denominator" /> is zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public Fraction(T numerator, T denominator)
    {
        NumericsThrowHelper.ThrowIfDenominatorZero(denominator);

        var n = BigInteger.CreateChecked(numerator);
        var d = BigInteger.CreateChecked(denominator);

        if (d.Sign < 0)
        {
            n = -n;
            d = -d;
        }

        var g = BigInteger.GreatestCommonDivisor(n, d);
        if (g > BigInteger.One)
        {
            n /= g;
            d /= g;
        }

        Numerator = T.CreateChecked(n);
        _denominator = T.CreateChecked(d);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Fraction{T}" /> struct from components that are already in
    /// canonical form, bypassing reduction.
    /// </summary>
    /// <param name="numerator">The canonical numerator.</param>
    /// <param name="denominator">The canonical, strictly positive denominator.</param>
    /// <param name="canonical">
    /// A discriminator that selects the no-reduction initialization path. The value itself is not inspected.
    /// </param>
    private Fraction(T numerator, T denominator, bool canonical)
    {
        _ = canonical;
        Numerator = numerator;
        _denominator = denominator;
    }

    /// <summary>
    /// Gets a <see cref="Fraction{T}" /> representing the value zero.
    /// </summary>
    /// <returns>The rational value <c>0/1</c>.</returns>
    public static Fraction<T> Zero =>
        new(T.Zero, T.One, canonical: true);

    /// <summary>
    /// Gets a <see cref="Fraction{T}" /> representing the value one.
    /// </summary>
    /// <returns>The rational value <c>1/1</c>.</returns>
    public static Fraction<T> One =>
        new(T.One, T.One, canonical: true);

    /// <summary>
    /// Gets a <see cref="Fraction{T}" /> representing the value negative one.
    /// </summary>
    /// <returns>The rational value <c>-1/1</c>.</returns>
    /// <exception cref="OverflowException">
    /// Thrown if <typeparamref name="T" /> is an unsigned type and cannot represent <c>-1</c>.
    /// </exception>
    public static Fraction<T> MinusOne =>
        FromBigInteger(BigInteger.MinusOne, BigInteger.One);

    /// <summary>
    /// Gets the smallest finite value a <see cref="Fraction{T}" /> backed by <typeparamref name="T" /> can represent.
    /// </summary>
    /// <returns>The rational value <c>T.MinValue/1</c>.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if <typeparamref name="T" /> is an unbounded integer type, such as <see cref="BigInteger" />.
    /// </exception>
    /// <remarks>
    /// A bound exists only when <typeparamref name="T" /> implements <see cref="IMinMaxValue{TSelf}" />. Unbounded
    /// backing types model the full set of rationals and therefore have no minimum value.
    /// </remarks>
    public static Fraction<T> MinValue =>
        s_isBounded
            ? new Fraction<T>(s_minBacking, T.One, canonical: true)
            : throw new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Op_NotSupported_UnboundedMinValue, typeof(T)));

    /// <summary>
    /// Gets the largest finite value a <see cref="Fraction{T}" /> backed by <typeparamref name="T" /> can represent.
    /// </summary>
    /// <returns>The rational value <c>T.MaxValue/1</c>.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if <typeparamref name="T" /> is an unbounded integer type, such as <see cref="BigInteger" />.
    /// </exception>
    /// <remarks>
    /// A bound exists only when <typeparamref name="T" /> implements <see cref="IMinMaxValue{TSelf}" />. Unbounded
    /// backing types model the full set of rationals and therefore have no maximum value.
    /// </remarks>
    public static Fraction<T> MaxValue =>
        s_isBounded
            ? new Fraction<T>(s_maxBacking, T.One, canonical: true)
            : throw new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, NumericsResourceStrings.Op_NotSupported_UnboundedMaxValue, typeof(T)));

    /// <summary>
    /// Gets the numerator of the rational value in canonical form.
    /// </summary>
    /// <returns>The signed numerator.</returns>
    public T Numerator { get; }

    /// <summary>
    /// Gets the denominator of the rational value in canonical form.
    /// </summary>
    /// <returns>The strictly positive denominator.</returns>
    /// <value>
    /// A value of one is reported for a default-initialized instance so that <c>default(Fraction&lt;T&gt;)</c> behaves
    /// as the rational value zero.
    /// </value>
    public T Denominator =>
        T.IsZero(_denominator) ? T.One : _denominator;

    /// <summary>
    /// Gets the sign of the rational value.
    /// </summary>
    /// <returns>
    /// <c>-1</c> if the value is negative, <c>0</c> if the value is zero, and <c>1</c> if the value is positive.
    /// </returns>
    public int Sign =>
        T.IsZero(Numerator) ? 0 : (T.IsNegative(Numerator) ? -1 : 1);

    /// <summary>
    /// Deconstructs the rational value into its canonical numerator and denominator.
    /// </summary>
    /// <param name="numerator">When this method returns, contains the canonical numerator.</param>
    /// <param name="denominator">When this method returns, contains the canonical denominator.</param>
    public void Deconstruct(out T numerator, out T denominator)
    {
        numerator = Numerator;
        denominator = Denominator;
    }

    /// <summary>
    /// Creates a canonical <see cref="Fraction{T}" /> from the specified numerator and denominator.
    /// </summary>
    /// <param name="numerator">The numerator of the rational value.</param>
    /// <param name="denominator">The denominator of the rational value.</param>
    /// <returns>The reduced rational value.</returns>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="denominator" /> is zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> Create(T numerator, T denominator) =>
        new(numerator, denominator);

    /// <summary>
    /// Attempts to create a canonical <see cref="Fraction{T}" /> from the specified numerator and denominator.
    /// </summary>
    /// <param name="numerator">The numerator of the rational value.</param>
    /// <param name="denominator">The denominator of the rational value.</param>
    /// <param name="result">When this method returns, contains the created value, or zero on failure.</param>
    /// <returns><see langword="true" /> if the value was created; otherwise, <see langword="false" />.</returns>
    public static bool TryCreate(T numerator, T denominator, out Fraction<T> result)
    {
        if (T.IsZero(denominator))
        {
            result = default;
            return false;
        }

        if (TryReduceToCanonical(BigInteger.CreateChecked(numerator), BigInteger.CreateChecked(denominator), out T n, out T d))
        {
            result = new Fraction<T>(n, d, canonical: true);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Computes the greatest common divisor of two integers.
    /// </summary>
    /// <param name="left">The first integer.</param>
    /// <param name="right">The second integer.</param>
    /// <returns>
    /// The largest non-negative integer that divides both <paramref name="left" /> and <paramref name="right" />, or
    /// zero when both arguments are zero.
    /// </returns>
    /// <exception cref="OverflowException">
    /// Thrown if the greatest common divisor cannot be represented by <typeparamref name="T" />.
    /// </exception>
    /// <remarks>
    /// The divisor is evaluated with <see cref="BigInteger" /> precision so that the magnitude of a signed minimum
    /// value — whose absolute value is not itself representable by <typeparamref name="T" /> — is handled correctly.
    /// </remarks>
    public static T GreatestCommonDivisor(T left, T right) =>
        T.CreateChecked(BigInteger.GreatestCommonDivisor(
            BigInteger.CreateChecked(left),
            BigInteger.CreateChecked(right)));

    /// <summary>
    /// Computes the least common multiple of two integers.
    /// </summary>
    /// <param name="left">The first integer.</param>
    /// <param name="right">The second integer.</param>
    /// <returns>
    /// The smallest non-negative integer that is a multiple of both arguments, or zero when either argument is zero.
    /// </returns>
    /// <exception cref="OverflowException">
    /// Thrown if the least common multiple cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static T LeastCommonMultiple(T left, T right)
    {
        if (T.IsZero(left) || T.IsZero(right))
            return T.Zero;

        var a = BigInteger.Abs(BigInteger.CreateChecked(left));
        var b = BigInteger.Abs(BigInteger.CreateChecked(right));
        BigInteger result = a / BigInteger.GreatestCommonDivisor(a, b) * b;

        return T.CreateChecked(result);
    }

    /// <summary>
    /// Creates a canonical <see cref="Fraction{T}" /> from a numerator and denominator of arbitrary magnitude.
    /// </summary>
    /// <param name="numerator">The numerator of the rational value.</param>
    /// <param name="denominator">The denominator of the rational value.</param>
    /// <returns>The reduced rational value narrowed to <typeparamref name="T" />.</returns>
    /// <exception cref="DivideByZeroException">Thrown if <paramref name="denominator" /> is zero.</exception>
    /// <exception cref="OverflowException">
    /// Thrown if the canonical numerator or denominator cannot be represented by <typeparamref name="T" />.
    /// </exception>
    public static Fraction<T> FromBigInteger(BigInteger numerator, BigInteger denominator)
    {
        NumericsThrowHelper.ThrowIfDenominatorZero(denominator);

        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        var g = BigInteger.GreatestCommonDivisor(numerator, denominator);
        if (g > BigInteger.One)
        {
            numerator /= g;
            denominator /= g;
        }

        return new Fraction<T>(T.CreateChecked(numerator), T.CreateChecked(denominator), canonical: true);
    }

    /// <summary>
    /// Attempts to create a canonical <see cref="Fraction{T}" /> from a numerator and denominator.
    /// </summary>
    /// <param name="numerator">The numerator of the rational value.</param>
    /// <param name="denominator">The denominator of the rational value.</param>
    /// <param name="result">When this method returns, contains the created value, or zero on failure.</param>
    /// <returns><see langword="true" /> if the value was created; otherwise, <see langword="false" />.</returns>
    public static bool TryFromBigInteger(BigInteger numerator, BigInteger denominator, out Fraction<T> result)
    {
        if (denominator.IsZero)
        {
            result = default;
            return false;
        }

        if (TryReduceToCanonical(numerator, denominator, out T n, out T d))
        {
            result = new Fraction<T>(n, d, canonical: true);
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// Reduces a numerator and denominator of arbitrary magnitude to canonical form and narrows both components to
    /// <typeparamref name="T" /> without throwing.
    /// </summary>
    /// <param name="numerator">The numerator, assumed to pair with a non-zero <paramref name="denominator" />.</param>
    /// <param name="denominator">The non-zero denominator.</param>
    /// <param name="canonicalNumerator">
    /// On success, the reduced numerator narrowed to <typeparamref name="T" />.
    /// </param>
    /// <param name="canonicalDenominator">
    /// On success, the reduced, strictly positive denominator narrowed to <typeparamref name="T" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when both canonical components fit <typeparamref name="T" />; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Shared non-throwing core behind <see cref="TryCreate" /> and <see cref="TryFromBigInteger" />; the throwing
    /// constructor and <see cref="FromBigInteger" /> retain their own narrowing so their
    /// <see cref="OverflowException" /> carries the framework conversion message.
    /// </remarks>
    private static bool TryReduceToCanonical(BigInteger numerator, BigInteger denominator, out T canonicalNumerator, out T canonicalDenominator)
    {
        if (denominator.Sign < 0)
        {
            numerator = -numerator;
            denominator = -denominator;
        }

        var g = BigInteger.GreatestCommonDivisor(numerator, denominator);
        if (g > BigInteger.One)
        {
            numerator /= g;
            denominator /= g;
        }

        return TryNarrow(numerator, out canonicalNumerator) & TryNarrow(denominator, out canonicalDenominator);
    }

    /// <summary>
    /// Narrows a <see cref="BigInteger" /> to <typeparamref name="T" /> without throwing, range-checking first when
    /// <typeparamref name="T" /> is bounded.
    /// </summary>
    /// <param name="value">The value to narrow.</param>
    /// <param name="result">
    /// On success, <paramref name="value" /> as <typeparamref name="T" />; otherwise the default.
    /// </param>
    /// <returns><see langword="true" /> when <paramref name="value" /> fits <typeparamref name="T" />.</returns>
    private static bool TryNarrow(BigInteger value, out T result)
    {
        if (s_isBounded && (value < s_minBackingBig || value > s_maxBackingBig))
        {
            result = default!;
            return false;
        }

        result = T.CreateChecked(value);
        return true;
    }

    /// <summary>
    /// Probes whether <typeparamref name="T" /> is a bounded integer type and, when it is, captures its bounds.
    /// </summary>
    /// <param name="minValue">On return, the minimum value of <typeparamref name="T" />.</param>
    /// <param name="maxValue">On return, the maximum value of <typeparamref name="T" />.</param>
    /// <returns><see langword="true" /> if the backing type is bounded; otherwise, <see langword="false" />.</returns>
    private static bool TryGetBounds(out T minValue, out T maxValue)
    {
        // The IMinMaxValue<T> constraint cannot be placed on Fraction<T> without excluding unbounded backing types,
        // so boundedness is detected by scanning T's implemented interfaces rather than by catching a constraint
        // violation. The reflective MinValue/MaxValue read below runs only once T is known to implement the
        // interface, so the constrained MakeGenericMethod call can no longer throw.
        if (!ImplementsMinMaxValue())
        {
            minValue = default!;
            maxValue = default!;
            return false;
        }

        minValue = InvokeExtreme(nameof(MinValueOf));
        maxValue = InvokeExtreme(nameof(MaxValueOf));
        return true;
    }

    /// <summary>
    /// Determines whether <typeparamref name="T" /> implements <see cref="IMinMaxValue{TSelf}" /> and therefore exposes
    /// a finite range.
    /// </summary>
    /// <returns><see langword="true" /> when <typeparamref name="T" /> is a bounded type.</returns>
    private static bool ImplementsMinMaxValue()
    {
        foreach (Type contract in typeof(T).GetInterfaces())
        {
            if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IMinMaxValue<>))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Invokes the named generic extreme-value helper for the backing type <typeparamref name="T" />.
    /// </summary>
    /// <param name="methodName">The name of the bounded-type extreme-value helper to invoke.</param>
    /// <returns>The extreme value of <typeparamref name="T" /> yielded by the helper.</returns>
    /// <exception cref="ArgumentException">Thrown if <typeparamref name="T" /> is not a bounded type.</exception>
    private static T InvokeExtreme(string methodName)
    {
        MethodInfo? definition = typeof(Fraction<T>).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        var extreme = definition!.MakeGenericMethod(typeof(T)).Invoke(null, null);
        return (T)extreme!;
    }

    /// <summary>
    /// Returns the smallest value a bounded integer type can represent.
    /// </summary>
    /// <typeparam name="TBounded">A bounded integer type.</typeparam>
    /// <returns>The minimum value of <typeparamref name="TBounded" />.</returns>
    private static TBounded MinValueOf<TBounded>()
        where TBounded : IMinMaxValue<TBounded> =>
        TBounded.MinValue;

    /// <summary>
    /// Returns the largest value a bounded integer type can represent.
    /// </summary>
    /// <typeparam name="TBounded">A bounded integer type.</typeparam>
    /// <returns>The maximum value of <typeparamref name="TBounded" />.</returns>
    private static TBounded MaxValueOf<TBounded>()
        where TBounded : IMinMaxValue<TBounded> =>
        TBounded.MaxValue;
}
