// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Lei.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the two-digit check sequence of a Legal Entity Identifier (LEI) as specified by ISO 17442. This class
/// cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// A Legal Entity Identifier is a twenty-character alphanumeric code comprising an eighteen-character body followed by
/// a two-digit check. The check is evaluated using ISO 7064 MOD 97-10 directly over the body, without the IBAN-style
/// rearrangement: letters are expanded by value <c>'A'</c>=10 … <c>'Z'</c>=35, the resulting decimal string is folded
/// through modulus ninety-seven, and the check is chosen so that the running remainder of the complete twenty-character
/// sequence equals <c>1</c>.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"54930084UKLVMY22DS"</c>, the computed check is <c>"16"</c>, and the
/// resulting LEI <c>"54930084UKLVMY22DS16"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
/// <example>
///<![CDATA[
/// Single-call computation against the 18-character body.
/// string check = Lei.Compute("54930084UKLVMY22DS");   // "16"
///
/// Full-sequence validation.
/// bool ok = Lei.IsValid("54930084UKLVMY22DS16");      // true
///
/// Streaming use when the body is built up incrementally.
/// var algo = new Lei();
/// algo.Append("54930084UKLVMY22DS");
/// string code = algo.GetCurrentCheckDigits();         // "16"
///]]>
/// </example>
public sealed class Lei
    : MultiCharCheckDigitAlgorithm
{
    /// <summary>
    /// The required body length of <c>18</c> characters.
    /// </summary>
    public const int BodyLength = 18;

    /// <summary>
    /// The fixed check-code length of <c>2</c> decimal digits.
    /// </summary>
    public const int CheckDigits = 2;

    /// <summary>
    /// The required full-sequence length of <c>20</c> characters.
    /// </summary>
    public const int SequenceLength = 20;

    private readonly Iso7064Mod97_10 _engine = new Iso7064Mod97_10();

    /// <summary>
    /// Initializes a new instance of the <see cref="Lei" /> class.
    /// </summary>
    public Lei()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "LEI";

    /// <inheritdoc />
    public override int CheckLength => CheckDigits;

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.AlphanumericUppercase;

    /// <summary>
    /// Computes the LEI check digits for the supplied body without allocating a streaming instance.
    /// </summary>
    /// <param name="body">The body characters. Each must be an ASCII decimal digit or uppercase Latin letter.</param>
    /// <returns>The check code as a two-character string of ASCII decimal digits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the alphanumeric uppercase alphabet.
    /// </exception>
    public static string Compute(ReadOnlySpan<char> body) =>
        Iso7064Mod97_10.Compute(body);

    /// <summary>
    /// Determines whether the supplied sequence, comprising an eighteen-character body followed by a two-digit LEI
    /// check, is consistent under ISO 17442.
    /// </summary>
    /// <param name="valueIncludingCheck">The complete twenty-character LEI.</param>
    /// <returns>
    /// <see langword="true" /> if the sequence is empty or evaluates as valid; otherwise, <see langword="false" /> —
    /// including the case where any character is outside the alphanumeric uppercase alphabet or the check characters
    /// are not decimal digits.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> valueIncludingCheck) =>
        Iso7064Mod97_10.IsValid(valueIncludingCheck);

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body) =>
        _engine.Append(body);

    /// <inheritdoc />
    public override int GetCurrentCheckDigits(Span<char> destination) =>
        _engine.GetCurrentCheckDigits(destination);

    /// <inheritdoc />
    public override void Reset() =>
        _engine.Reset();
}
