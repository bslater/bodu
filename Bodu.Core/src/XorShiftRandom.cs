// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandom.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu;

/// <summary>
/// Represents a high-performance, non-cryptographic pseudo-random number generator based on Marsaglia's xorshift128
/// algorithm.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="XorShiftRandom" /> derives each output by combining four 32-bit state words through three xor-and-shift
/// operations — a generator class introduced by George Marsaglia in 2003. The cost per draw is a handful of register
/// operations with no branching, no division, and no memory allocation, making it materially faster than
/// <see cref="System.Random" /> in tight inner loops on every supported runtime.
/// </para>
/// <para>
/// The type subclasses <see cref="System.Random" /> so it can be passed anywhere <see cref="System.Random" /> is
/// accepted, and it also implements <see cref="IRandomGenerator" /> for use with the library's shuffle and sampling
/// helpers. The default constructor seeds from <see cref="Environment.TickCount" />; the seeded constructors are the
/// preferred choice in tests and reproducible benchmarks.
/// </para>
/// <para>
/// Instances are not thread-safe — state updates are non-atomic and concurrent draws will corrupt the internal state.
/// Use a per-thread instance, an external lock, or a thread-local pool when sharing across threads is required.
/// </para>
/// <para>
/// The algorithm produces a uniform distribution over the 32-bit output range and has a period of <c>2^128 − 1</c>. It
/// is <em>not</em> suitable for cryptographic use, secret material, or any context where an attacker can observe
/// outputs and recover state. Use <see cref="System.Security.Cryptography.RandomNumberGenerator" /> for those cases.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Reproducible shuffle and sample in a test.
/// var rng = new XorShiftRandom(seed: 1234);
/// int    roll  = rng.Next(6) + 1;           // value in [1, 6]
/// double angle = rng.NextDouble() * Math.Tau;
///
/// var bag = new[] { "A", "B", "C", "D" };
/// ShuffleHelpers.Shuffle(bag, rng);
///]]>
/// </example>
public sealed class XorShiftRandom
    : System.Random,
    IRandomGenerator
{
    /// <summary>The delegate that produces the next 32 random bits, allowing the core generator to be overridden.</summary>
    private readonly Func<uint> _bitsSource;

    /// <summary>The fourth of the four 32-bit state words of the xorshift128 generator, and the source of each generated value.</summary>
    private uint _w;

    /// <summary>The first of the four 32-bit state words of the xorshift128 generator.</summary>
    private uint _x;

    /// <summary>The second of the four 32-bit state words of the xorshift128 generator.</summary>
    private uint _y;

    /// <summary>The third of the four 32-bit state words of the xorshift128 generator.</summary>
    private uint _z;

    /// <summary>
    /// Initializes a new instance of the <see cref="XorShiftRandom" /> class using a system-generated seed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default seed is derived from <see cref="Environment.TickCount" /> at the time of construction.
    /// </para>
    /// <para>
    /// <see cref="Environment.TickCount" /> has finite resolution (commonly ~15.6 ms on Windows, finer on Linux), so
    /// two <see cref="XorShiftRandom" /> instances created back-to-back within the same tick will observe the same seed
    /// and therefore yield identical sequences. This is a deliberate trade-off for the cheapest possible default
    /// seeding on a non-cryptographic PRNG. Callers that require either guarantee should use a different constructor:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// For <em>reproducibility</em> (tests, benchmarks, replayable simulations) use <see cref="XorShiftRandom(int)" />
    /// or <see cref="XorShiftRandom(uint)" /> with an explicit seed.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// For <em>distinct sequences across many instances</em> derive the seed from <see cref="System.Random.Shared" />
    /// (e.g. <c>new XorShiftRandom(Random.Shared.Next())</c>) or, when stronger entropy is required, from
    /// <see cref="System.Security.Cryptography.RandomNumberGenerator" />.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    public XorShiftRandom()
        : this((uint)Environment.TickCount)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="XorShiftRandom" /> class with a 32-bit signed integer seed.
    /// </summary>
    /// <param name="seed">The seed used to initialize the random generator.</param>
    public XorShiftRandom(int seed)
        : this((uint)seed)
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="XorShiftRandom" /> class with a 32-bit unsigned seed.
    /// </summary>
    /// <param name="seed">The seed used to initialize the random generator.</param>
    public XorShiftRandom(uint seed)
    {
        // Initialize four internal states with XOR-variations of the seed for better distribution
        _x = seed;
        _y = seed ^ 0x6C8E9CF5U;
        _z = seed ^ 0x94D049BBU;
        _w = seed ^ 0x5A17D7F9U;

        // Cache one delegate per instance so bounded-int draws do not allocate per call.
        _bitsSource = NextUInt32;
    }

    /// <inheritdoc />
    public override int Next() => Next(int.MaxValue);

    /// <summary>
    /// Returns a non-negative random integer that is strictly less than the specified exclusive upper bound.
    /// </summary>
    /// <param name="maxValue">The exclusive upper bound of the random number to be generated.</param>
    /// <returns>A 32-bit signed integer in the half-open interval <c>[0, <paramref name="maxValue" />)</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxValue" /> is less than or equal to zero.
    /// </exception>
    /// <remarks>
    /// Matches the contract of <see cref="System.Random.Next(int)" />. Draws are produced through Lemire-style
    /// rejection rather than modulo, eliminating the modulo-bias that would otherwise skew small bounds.
    /// </remarks>
    public override int Next(int maxValue)
    {
        ThrowHelper.ThrowIfLessThanOrEqual(maxValue, 0);
        return (int)BoundedNextUInt32((uint)maxValue, _bitsSource);
    }

    /// <summary>
    /// Returns a random integer in the half-open interval
    /// <c>[<paramref name="minValue" />, <paramref name="maxValue" />)</c>.
    /// </summary>
    /// <param name="minValue">The inclusive lower bound of the random number to be generated.</param>
    /// <param name="maxValue">The exclusive upper bound of the random number to be generated.</param>
    /// <returns>
    /// A 32-bit signed integer greater than or equal to <paramref name="minValue" /> and strictly less than
    /// <paramref name="maxValue" />; or <paramref name="minValue" /> when the two bounds are equal.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="minValue" /> is strictly greater than <paramref name="maxValue" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Matches the contract of <see cref="System.Random.Next(int, int)" />, including its handling of an empty range:
    /// when <paramref name="minValue" /> equals <paramref name="maxValue" /> the method returns
    /// <paramref name="minValue" /> without throwing. Only a strictly inverted range (<paramref name="minValue" /> &gt;
    /// <paramref name="maxValue" />) raises <see cref="ArgumentException" />.
    /// </para>
    /// <para>
    /// The full <see cref="int" /> span <c>Next(int.MinValue, int.MaxValue)</c> is supported — the range is computed in
    /// 64-bit space so the subtraction never overflows, and bounded generation uses Lemire-style rejection rather than
    /// modulo to avoid modulo-bias.
    /// </para>
    /// </remarks>
    public override int Next(int minValue, int maxValue)
    {
        ThrowHelper.ThrowIfGreaterThanOther(minValue, maxValue);

        // Compute range in long-space so the subtraction never overflows, even for the
        // full-int span Next(int.MinValue, int.MaxValue) — every int range fits in uint.
        uint range = (uint)((long)maxValue - (long)minValue);
        return range == 0
            ? minValue
            : (int)((long)minValue + BoundedNextUInt32(range, _bitsSource));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void NextBytes(byte[] buffer)
    {
        ThrowHelper.ThrowIfNull(buffer);

        for (int i = 0; i < buffer.Length; i++)
        {
            if ((i & 3) == 0)
            {
                uint rnd = NextUInt32();
                buffer[i++] = (byte)(rnd & 0xFF);
                if (i < buffer.Length) buffer[i++] = (byte)((rnd >> 8) & 0xFF);
                if (i < buffer.Length) buffer[i++] = (byte)((rnd >> 16) & 0xFF);
                if (i < buffer.Length) buffer[i++] = (byte)((rnd >> 24) & 0xFF);
                i--; // account for loop increment
            }
        }
    }

    /// <inheritdoc />
    public override double NextDouble() => ScaleToUnitInterval(NextUInt32());

    /// <summary>
    /// Returns an unbiased pseudo-random <see cref="uint" /> uniformly distributed in <c>[0, maxExclusive)</c> using
    /// Lemire's debiased bounded-integer method.
    /// </summary>
    /// <param name="maxExclusive">The exclusive upper bound. Must be greater than zero.</param>
    /// <param name="source">A delegate that yields uniformly distributed 32-bit pseudo-random values.</param>
    /// <returns>An unbiased value in the range <c>[0, maxExclusive)</c>.</returns>
    /// <remarks>
    /// <para>
    /// Implements Daniel Lemire's "Fast Random Integer Generation in an Interval" (ACM TOMS, 2019). The wide
    /// multiplication plus a rare-reject loop yields a strictly uniform distribution, unlike <c>x % maxExclusive</c>
    /// which introduces modulo bias whenever <paramref name="maxExclusive" /> does not evenly divide <c>2^32</c>. The
    /// bias is negligible for small ranges but reaches up to 50% for ranges close to <c>2^31</c>.
    /// </para>
    /// <para>
    /// The threshold <c>2^32 mod maxExclusive</c> bounds the rejection rate: at most one extra draw is expected for
    /// each call, and the worst case is well under 50% rejection probability.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint BoundedNextUInt32(uint maxExclusive, Func<uint> source)
    {
        ulong product = (ulong)source() * maxExclusive;
        uint lowBits = (uint)product;
        if (lowBits < maxExclusive)
        {
            // threshold = 2^32 mod maxExclusive; lowBits below this are biased and must be redrawn.
            uint threshold = (0u - maxExclusive) % maxExclusive;
            while (lowBits < threshold)
            {
                product = (ulong)source() * maxExclusive;
                lowBits = (uint)product;
            }
        }

        return (uint)(product >> 32);
    }

    /// <summary>
    /// Scales a 32-bit unsigned random value into the half-open unit interval [0.0, 1.0).
    /// </summary>
    /// <param name="value">The raw 32-bit pseudo-random value to scale.</param>
    /// <returns>
    /// A double-precision value in the range [0.0, 1.0). The upper bound is excluded — when <paramref name="value" />
    /// is <see cref="uint.MaxValue" /> the result is strictly less than 1.0.
    /// </returns>
    /// <remarks>
    /// Multiplies by <c>1 / 2^32</c> rather than dividing by <see cref="uint.MaxValue" />. Dividing by
    /// <see cref="uint.MaxValue" /> yields exactly 1.0 for the largest input, which would violate the
    /// <see cref="System.Random.NextDouble" /> contract of <c>[0.0, 1.0)</c>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ScaleToUnitInterval(uint value) =>
        value * (1.0 / 4294967296.0);

    /// <summary>
    /// Generates the next 32-bit random number using XOR-shift algorithm.
    /// </summary>
    /// <returns>A pseudo-random <see cref="uint" /> drawn from the current generator state.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint NextUInt32()
    {
        uint t = _x ^ (_x << 11);
        _x = _y;
        _y = _z;
        _z = _w;
        _w ^= (_w >> 19) ^ t ^ (t >> 8);
        return _w;
    }
}
