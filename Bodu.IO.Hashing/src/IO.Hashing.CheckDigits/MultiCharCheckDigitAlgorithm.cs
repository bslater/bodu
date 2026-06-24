// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MultiCharCheckDigitAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Represents the abstract base class from which check-digit algorithms that emit a fixed-length multi-character check
/// code — most notably ISO 7064 MOD 97-10 and its derivatives (IBAN, LEI) — derive.
/// </summary>
/// <remarks>
/// <para>
/// Parallel in design to <see cref="CheckDigitAlgorithm" />, this base generalizes the output from a single character
/// to a run of <see cref="CheckLength" /> decimal digits, and broadens the input to whichever subset of ASCII the
/// concrete algorithm accepts via <see cref="InputAlphabet" />.
/// </para>
/// <para>
/// Like the rest of this family, the type is intentionally separate from the byte-stream oriented
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm" /> and does <b>not</b> derive from it. A
/// non-cryptographic hash produces a fixed-length opaque <em>byte</em> digest over arbitrary input; a check code
/// performs error detection over a constrained ASCII text alphabet and emits a short run of <see cref="char" /> values.
/// The families are kept distinct by design rather than unified under one base type.
/// </para>
/// <para>
/// The streaming surface — <see cref="Append(ReadOnlySpan{char})" />, <see cref="Reset" />, and the two
/// <c>GetCurrentCheckDigits</c> overloads — will nonetheless feel familiar to anyone who has used a hash algorithm:
/// input is accumulated, the computation can be restarted, and reading the current check code is non-destructive and
/// idempotent. That resemblance is incidental convenience, not a shared contract. Concrete implementations document
/// their empty-body behavior.
/// </para>
/// <para>
/// Instances are <b>not</b> thread-safe. Each thread that needs a running check should construct its own instance.
/// </para>
/// <note type="important">Check-digit algorithms are error-detection primitives, <b>not</b> cryptographic functions.
/// They must <b>not</b> be used for password hashing, digital signatures, or integrity validation in security-sensitive
/// applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// // Use a concrete derivative through the abstract surface — IBAN emits a two-digit check.
/// MultiCharCheckDigitAlgorithm algo = new Iban();
/// algo.Append("GBWEST12345698765432");                 // country code + BBAN
///
/// Span<char> check = stackalloc char[algo.CheckLength];
/// algo.GetCurrentCheckDigits(check);                   // "82"
///]]>
/// </example>
/// <seealso cref="CheckDigitAlgorithm" /> <seealso cref="AlphanumericCheckDigitAlgorithm" />
/// <seealso cref="System.IO.Hashing.NonCryptographicHashAlgorithm" />
public abstract class MultiCharCheckDigitAlgorithm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MultiCharCheckDigitAlgorithm" /> class.
    /// </summary>
    protected MultiCharCheckDigitAlgorithm()
    {
    }

    /// <summary>
    /// Gets the canonical name of the algorithm, suitable for diagnostic output and logging.
    /// </summary>
    /// <value>
    /// A short, stable identifier such as <c>"ISO 7064 MOD 97-10"</c>, <c>"IBAN"</c>, or <c>"LEI"</c>.
    /// </value>
    public abstract string AlgorithmName { get; }

    /// <summary>
    /// Gets the fixed number of decimal-digit characters emitted as the trailing check code.
    /// </summary>
    /// <value>A positive integer; for example, <c>2</c> for ISO 7064 MOD 97-10.</value>
    public abstract int CheckLength { get; }

    /// <summary>
    /// Gets the subset of ASCII from which this algorithm accepts body characters.
    /// </summary>
    /// <value>The declared input alphabet.</value>
    public abstract CheckDigitInputAlphabet InputAlphabet { get; }

    /// <summary>
    /// Absorbs the supplied characters into the running check-code state.
    /// </summary>
    /// <param name="body">
    /// The characters to append. Each element must belong to <see cref="InputAlphabet" />. An empty span is a permitted
    /// no-op.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside <see cref="InputAlphabet" />.
    /// </exception>
    public abstract void Append(ReadOnlySpan<char> body);

    /// <summary>
    /// Absorbs a single character into the running check-code state.
    /// </summary>
    /// <param name="ch">The character to append. Must belong to <see cref="InputAlphabet" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ch" /> is outside <see cref="InputAlphabet" />.
    /// </exception>
    public void Append(char ch) =>
        Append([ch]);

    /// <summary>
    /// Returns the current check code as a newly allocated <see cref="string" />.
    /// </summary>
    /// <returns>A string of length <see cref="CheckLength" /> containing the trailing check characters.</returns>
    /// <remarks>
    /// Allocating convenience wrapper over <see cref="GetCurrentCheckDigits(Span{char})" />. Prefer the span overload
    /// in hot paths where the allocation is undesirable.
    /// </remarks>
    public string GetCurrentCheckDigits()
    {
        Span<char> buffer = stackalloc char[CheckLength];
        GetCurrentCheckDigits(buffer);
        return new string(buffer);
    }

    /// <summary>
    /// Writes the current check code into the supplied destination span.
    /// </summary>
    /// <param name="destination">
    /// The span to receive the trailing check characters. Must be at least <see cref="CheckLength" /> in length.
    /// </param>
    /// <returns>The number of characters written, always equal to <see cref="CheckLength" />.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="destination" /> is shorter than <see cref="CheckLength" />.
    /// </exception>
    /// <remarks>
    /// This call is non-destructive; the running state is unaffected and the method may be invoked any number of times
    /// with identical results between appends.
    /// </remarks>
    public abstract int GetCurrentCheckDigits(Span<char> destination);

    /// <summary>
    /// Resets the algorithm to its initial state, discarding any characters previously absorbed.
    /// </summary>
    /// <remarks>
    /// Equivalent in behavior to constructing a fresh instance of the same concrete type.
    /// </remarks>
    public abstract void Reset();
}
