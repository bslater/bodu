// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlphanumericCheckDigitAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Represents the abstract base class from which single-character check-digit algorithms that operate on a wider input
/// alphabet, or whose check character may be the sentinel <c>'X'</c>, derive.
/// </summary>
/// <remarks>
/// <para>
/// Parallel in design to <see cref="CheckDigitAlgorithm" />, this base relaxes two constraints: the input may be any
/// subset of ASCII declared by <see cref="InputAlphabet" /> (decimal digits, or decimal digits and uppercase Latin
/// letters), and the emitted check character may be any value of <see cref="OutputAlphabet" /> — notably including the
/// <c>'X'</c> sentinel used by ISBN-10 and ISO 7064 MOD 11-2 to represent the check value ten.
/// </para>
/// <para>
/// Like the rest of this family, the type is intentionally separate from the byte-stream oriented
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm" /> and does <b>not</b> derive from it. A
/// non-cryptographic hash produces a fixed-length opaque <em>byte</em> digest over arbitrary input; a check character
/// performs error detection over a constrained ASCII text alphabet and emits a single <see cref="char" />. The families
/// are kept distinct by design rather than unified under one base type.
/// </para>
/// <para>
/// The streaming surface — <see cref="Append(ReadOnlySpan{char})" />, <see cref="Reset" />, and
/// <see cref="GetCurrentCheckDigit" /> — will nonetheless feel familiar to anyone who has used a hash algorithm: input
/// is accumulated, the computation can be restarted, and reading the current check character is non-destructive and
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
/// <code language="csharp">
///<![CDATA[
/// // Use a concrete derivative through the abstract surface — ISIN accepts a mix of
/// // ASCII letters (the country code) and digits.
/// AlphanumericCheckDigitAlgorithm algo = new Isin();
/// algo.Append("US037833100");                          // Apple Inc.
/// char check = algo.GetCurrentCheckDigit();            // '5'
///
/// // Inspect the declared alphabets for input validation upstream.
/// CheckDigitInputAlphabet  inputs  = algo.InputAlphabet;
/// CheckDigitOutputAlphabet outputs = algo.OutputAlphabet;
///]]>
/// </code>
/// </example>
/// <seealso cref="CheckDigitAlgorithm" /> <seealso cref="MultiCharCheckDigitAlgorithm" />
/// <seealso cref="System.IO.Hashing.NonCryptographicHashAlgorithm" />
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
    /// <value>A short, stable identifier such as <c>"ISBN-10"</c>, <c>"ISIN"</c>, or <c>"SEDOL"</c>.</value>
    public abstract string AlgorithmName { get; }

    /// <summary>
    /// Gets the subset of ASCII from which this algorithm accepts body characters.
    /// </summary>
    /// <value>The declared input alphabet.</value>
    public abstract CheckDigitInputAlphabet InputAlphabet { get; }

    /// <summary>
    /// Gets the subset of ASCII from which this algorithm may emit its check character.
    /// </summary>
    /// <value>The declared output alphabet.</value>
    public abstract CheckDigitOutputAlphabet OutputAlphabet { get; }

    /// <summary>
    /// Absorbs the supplied characters into the running check-character state.
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
    /// Absorbs a single character into the running check-character state.
    /// </summary>
    /// <param name="ch">The character to append. Must belong to <see cref="InputAlphabet" />.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ch" /> is outside <see cref="InputAlphabet" />.
    /// </exception>
    public void Append(char ch) =>
        Append([ch]);

    /// <summary>
    /// Returns the check character computed for the body absorbed since the last <see cref="Reset" /> (or since
    /// construction).
    /// </summary>
    /// <returns>The check character as an ASCII character drawn from <see cref="OutputAlphabet" />.</returns>
    /// <remarks>
    /// This call is non-destructive; the running state is unaffected and the method may be invoked any number of times
    /// with identical results between appends.
    /// </remarks>
    public abstract char GetCurrentCheckDigit();

    /// <summary>
    /// Resets the algorithm to its initial state, discarding any characters previously absorbed.
    /// </summary>
    /// <remarks>
    /// Equivalent in behavior to constructing a fresh instance of the same concrete type.
    /// </remarks>
    public abstract void Reset();
}
