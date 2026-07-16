// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckValueAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Represents the common root of the check-digit algorithm family, unifying the streaming surface shared by
/// <see cref="CheckDigitAlgorithm" /> (decimal bodies, single check digit),
/// <see cref="AlphanumericCheckDigitAlgorithm" /> (alphanumeric bodies, single check character), and
/// <see cref="MultiCharCheckDigitAlgorithm" /> (multi-character check codes).
/// </summary>
/// <remarks>
/// <para>
/// Every algorithm in the family absorbs body characters through <see cref="Append(ReadOnlySpan{char})" />, restarts
/// through <see cref="Reset" />, and exposes its current check value non-destructively. This root makes that shared
/// contract polymorphic: <see cref="GetCurrentCheckValue" /> returns the check value as a string of length
/// <see cref="CheckLength" /> regardless of which branch of the family the concrete algorithm belongs to, so callers
/// can validate heterogeneous identifier formats through a single reference type.
/// </para>
/// <para>
/// The intermediate base classes retain their more precise result surfaces — <c>GetCurrentCheckDigit()</c> returning a
/// <see cref="char" /> on the single-character branches and <c>GetCurrentCheckDigits(Span{char})</c> on the
/// multi-character branch — and implement <see cref="GetCurrentCheckValue" /> by delegation.
/// </para>
/// <para>
/// Instances are <b>not</b> thread-safe. Each thread that needs a running check should construct its own instance.
/// </para>
/// <note type="important">Check-digit algorithms are error-detection primitives, <b>not</b> cryptographic functions.
/// They must <b>not</b> be used for password hashing, digital signatures, or integrity validation in security-sensitive
/// applications.</note>
/// </remarks>
/// <seealso cref="CheckDigitAlgorithm" /> <seealso cref="AlphanumericCheckDigitAlgorithm" />
/// <seealso cref="MultiCharCheckDigitAlgorithm" />
public abstract class CheckValueAlgorithm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckValueAlgorithm" /> class.
    /// </summary>
    protected CheckValueAlgorithm()
    {
    }

    /// <summary>
    /// Gets the canonical name of the algorithm, suitable for diagnostic output and logging.
    /// </summary>
    /// <value>A short, stable identifier such as <c>"Luhn"</c>, <c>"IBAN"</c>, or <c>"ISO 7064 MOD 97-10"</c>.</value>
    public abstract string AlgorithmName { get; }

    /// <summary>
    /// Gets the number of characters in the check value this algorithm emits.
    /// </summary>
    /// <value>
    /// A positive integer; <c>1</c> for the single-character families, or the fixed code length (for example <c>2</c>
    /// for ISO 7064 MOD 97-10) on the multi-character branch.
    /// </value>
    public virtual int CheckLength =>
        1;

    /// <summary>
    /// Absorbs the supplied characters into the running check-value state.
    /// </summary>
    /// <param name="body">
    /// The characters to append. Each element must belong to the algorithm's accepted input alphabet. An empty span is
    /// a permitted no-op.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the algorithm's accepted input alphabet.
    /// </exception>
    public abstract void Append(ReadOnlySpan<char> body);

    /// <summary>
    /// Absorbs a single character into the running check-value state.
    /// </summary>
    /// <param name="ch">The character to append. Must belong to the algorithm's accepted input alphabet.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ch" /> is outside the algorithm's accepted input alphabet.
    /// </exception>
    public void Append(char ch) =>
        Append([ch]);

    /// <summary>
    /// Returns the check value computed for the body absorbed since the last <see cref="Reset" /> (or since
    /// construction) as a newly allocated string.
    /// </summary>
    /// <returns>A string of length <see cref="CheckLength" /> containing the check value.</returns>
    /// <remarks>
    /// This call is non-destructive; the running state is unaffected and the method may be invoked any number of times
    /// with identical results between appends. The single-character branches also expose the result as a
    /// <see cref="char" /> via <c>GetCurrentCheckDigit()</c>, which avoids the string allocation.
    /// </remarks>
    public abstract string GetCurrentCheckValue();

    /// <summary>
    /// Resets the algorithm to its initial state, discarding any characters previously absorbed.
    /// </summary>
    /// <remarks>
    /// Equivalent in behavior to constructing a fresh instance of the same concrete type.
    /// </remarks>
    public abstract void Reset();
}
