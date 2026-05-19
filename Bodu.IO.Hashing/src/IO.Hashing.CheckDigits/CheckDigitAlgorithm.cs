// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Represents the abstract base class from which all decimal check-digit algorithms in this library derive.
/// </summary>
/// <remarks>
/// <para>
/// A check-digit algorithm consumes a sequence of decimal digits (<c>'0'</c> to <c>'9'</c>) and yields a single
/// trailing digit that, when appended to the body, allows the concatenated sequence to be validated. Unlike the
/// byte-stream oriented <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm" />, this class operates on
/// <see cref="char" /> sequences natively and emits a single <see cref="char" /> result — matching the domain of card
/// numbers, national identifiers, and serial codes on which these algorithms are typically applied.
/// </para>
/// <para>
/// The streaming surface — <see cref="Append(ReadOnlySpan{char})" />, <see cref="Reset" />, and
/// <see cref="GetCurrentCheckDigit" /> — mirrors the familiar hash-algorithm idiom. Reading the current check digit is
/// non-destructive and idempotent. On an empty body every built-in algorithm in this library returns the digit
/// <c>'0'</c>; concrete implementations document any exception.
/// </para>
/// <para>
/// Instances are <b>not</b> thread-safe. Each thread that needs a running check should construct its own instance.
/// </para>
/// <note type="important">Check-digit algorithms are error-detection primitives, <b>not</b> cryptographic functions.
/// They must <b>not</b> be used for password hashing, digital signatures, or integrity validation in security-sensitive
/// applications.</note>
/// </remarks>
public abstract class CheckDigitAlgorithm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckDigitAlgorithm" /> class.
    /// </summary>
    protected CheckDigitAlgorithm()
    {
    }

    /// <summary>
    /// Gets the canonical name of the algorithm, suitable for diagnostic output and logging.
    /// </summary>
    /// <returns>A short, stable identifier such as <c>"Luhn"</c>, <c>"Damm"</c>, or <c>"Verhoeff"</c>.</returns>
    public abstract string AlgorithmName { get; }

    /// <summary>
    /// Absorbs the supplied decimal digits into the running check-digit state.
    /// </summary>
    /// <param name="digits">
    /// The characters to append. Each element must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>). An empty span
    /// is a permitted no-op.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digits" /> contains any character outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    public abstract void Append(ReadOnlySpan<char> digits);

    /// <summary>
    /// Absorbs a single decimal digit into the running check-digit state.
    /// </summary>
    /// <param name="digit">The character to append. Must be an ASCII decimal digit (<c>'0'</c> to <c>'9'</c>).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="digit" /> is outside the range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    public void Append(char digit) =>
        Append([digit]);

    /// <summary>
    /// Returns the check digit computed for the body absorbed since the last <see cref="Reset" /> (or since
    /// construction).
    /// </summary>
    /// <returns>
    /// The check digit as an ASCII character in the range <c>'0'</c> to <c>'9'</c>. For an empty body all built-in
    /// algorithms return <c>'0'</c>.
    /// </returns>
    /// <remarks>
    /// This call is non-destructive; the running state is unaffected and the method may be invoked any number of times
    /// with identical results between appends.
    /// </remarks>
    public abstract char GetCurrentCheckDigit();

    /// <summary>
    /// Resets the algorithm to its initial state, discarding any digits previously absorbed.
    /// </summary>
    /// <remarks>
    /// Equivalent in behavior to constructing a fresh instance of the same concrete type.
    /// </remarks>
    public abstract void Reset();
}
