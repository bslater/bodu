// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.CheckDigits;

/// <summary>
/// Represents the abstract base class from which all decimal check-digit algorithms in this library derive.
/// </summary>
/// <remarks>
/// <para>
/// A check-digit algorithm consumes a sequence of decimal digits (<c>'0'</c> to <c>'9'</c>) and yields a single
/// trailing digit that, when appended to the body, allows the concatenated sequence to be validated — matching the
/// domain of card numbers, national identifiers, and serial codes on which these algorithms are typically applied.
/// </para>
/// <para>
/// This is a deliberately separate family from the byte-stream oriented
/// <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm" />, and it does <b>not</b> derive from it. The two serve
/// different purposes: a non-cryptographic hash produces a fixed-length opaque <em>byte</em> digest over arbitrary
/// input, whereas a check digit performs error detection over a constrained ASCII text alphabet and emits a single
/// <see cref="char" /> result. Modeling one as the other would either narrow the hash contract or reduce the check
/// character to an encoding artifact, so the families are kept distinct by design.
/// </para>
/// <para>
/// The streaming surface — <see cref="CheckValueAlgorithm.Append(ReadOnlySpan{char})" />,
/// <see cref="CheckValueAlgorithm.Reset" />, and <see cref="GetCurrentCheckDigit" /> — will nonetheless feel familiar
/// to anyone who has used a hash algorithm: <see cref="CheckValueAlgorithm.Append(ReadOnlySpan{char})" /> accumulates
/// input, <see cref="CheckValueAlgorithm.Reset" /> restarts the computation, and reading the current check digit is
/// non-destructive and idempotent. That resemblance is incidental convenience for the reader's intuition, not a shared
/// contract. On an empty body every built-in algorithm in this library returns the digit <c>'0'</c>; concrete
/// implementations document any exception.
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
/// // Use a concrete derivative through the abstract surface.
/// CheckDigitAlgorithm algo = new Luhn();
/// algo.Append("7992739871");
/// char check = algo.GetCurrentCheckDigit();   // '3'
///
/// // Reset between independent computations.
/// algo.Reset();
/// algo.Append('1');
/// algo.Append("7893729977");
///]]>
/// </code>
/// </example>
/// <seealso cref="AlphanumericCheckDigitAlgorithm" /> <seealso cref="MultiCharCheckDigitAlgorithm" />
/// <seealso cref="System.IO.Hashing.NonCryptographicHashAlgorithm" />
public abstract class CheckDigitAlgorithm
    : CheckValueAlgorithm
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckDigitAlgorithm" /> class.
    /// </summary>
    protected CheckDigitAlgorithm()
    {
    }

    /// <summary>
    /// Returns the check digit computed for the body absorbed since the last <see cref="CheckValueAlgorithm.Reset" />
    /// (or since construction).
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

    /// <inheritdoc />
    /// <remarks>
    /// Delegates to <see cref="GetCurrentCheckDigit" />; prefer that member when the single-character result suffices,
    /// as it avoids the string allocation.
    /// </remarks>
    public sealed override string GetCurrentCheckValue() =>
        GetCurrentCheckDigit().ToString();
}
