// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

using Bodu.IO.Hashing.Checksums;

/// <summary>
/// Represents the abstract base class from which single-character check-digit algorithms that operate on a wider
/// input alphabet, or whose check character may be the sentinel <c>'X'</c>, derive.
/// </summary>
/// <remarks>
/// <para>
/// Parallel in design to <see cref="CheckDigitAlgorithm" />, this base relaxes two constraints: the input may be
/// any subset of ASCII declared by <see cref="InputAlphabet" /> (decimal digits, or decimal digits and uppercase
/// Latin letters), and the emitted check character may be any value of <see cref="OutputAlphabet" /> — notably
/// including the <c>'X'</c> sentinel used by ISBN-10 and ISO 7064 MOD 11-2 to represent the check value ten.
/// </para>
/// <para>
/// The streaming surface — <see cref="Append(ReadOnlySpan{char})" />, <see cref="Reset" />, and
/// <see cref="GetCurrentCheckDigit" /> — mirrors the familiar hash-algorithm idiom. Reading the current check
/// character is non-destructive and idempotent. Concrete implementations document their empty-body behaviour.
/// </para>
/// <para>
/// Instances are <b>not</b> thread-safe. Each thread that needs a running check should construct its own instance.
/// </para>
/// <note type="important">Check-digit algorithms are error-detection primitives, <b>not</b> cryptographic
/// functions. They must <b>not</b> be used for password hashing, digital signatures, or integrity validation in
/// security-sensitive applications.</note>
/// </remarks>
public abstract class AlphanumericCheckDigitAlgorithm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlphanumericCheckDigitAlgorithm" /> class.
    /// </summary>
    protected AlphanumericCheckDigitAlgorithm()
    {
    }

    /// <summary>
    /// Gets the canonical name of the algorithm, suitable for diagnostic output and logging.
    /// </summary>
    /// <returns>A short, stable identifier such as <c>"ISBN-10"</c>, <c>"ISIN"</c>, or <c>"SEDOL"</c>.</returns>
    public abstract string AlgorithmName { get; }

    /// <summary>
    /// Gets the subset of ASCII from which this algorithm accepts body characters.
    /// </summary>
    /// <returns>The declared input alphabet.</returns>
    public abstract CheckDigitInputAlphabet InputAlphabet { get; }

    /// <summary>
    /// Gets the subset of ASCII from which this algorithm may emit its check character.
    /// </summary>
    /// <returns>The declared output alphabet.</returns>
    public abstract CheckDigitOutputAlphabet OutputAlphabet { get; }

    /// <summary>
    /// Absorbs the supplied characters into the running check-character state.
    /// </summary>
    /// <param name="body">
    /// The characters to append. Each element must belong to <see cref="InputAlphabet" />. An empty span is a
    /// permitted no-op.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside <see cref="InputAlphabet" />.
    /// </exception>
    public abstract void Append(ReadOnlySpan<char> body);

    /// <summary>
    /// Absorbs a single character into the running check-character state.
    /// </summary>
    /// <param name="ch">The character to append. Must belong to <see cref="InputAlphabet" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ch" /> is outside <see cref="InputAlphabet" />.
    /// </exception>
    public void Append(char ch) =>
        Append([ch]);

    /// <summary>
    /// Resets the algorithm to its initial state, discarding any characters previously absorbed.
    /// </summary>
    /// <remarks>Equivalent in behaviour to constructing a fresh instance of the same concrete type.</remarks>
    public abstract void Reset();

    /// <summary>
    /// Returns the check character computed for the body absorbed since the last <see cref="Reset" /> (or since
    /// construction).
    /// </summary>
    /// <returns>
    /// The check character as an ASCII character drawn from <see cref="OutputAlphabet" />.
    /// </returns>
    /// <remarks>This call is non-destructive; the running state is unaffected and the method may be invoked any
    /// number of times with identical results between appends.</remarks>
    public abstract char GetCurrentCheckDigit();
}
