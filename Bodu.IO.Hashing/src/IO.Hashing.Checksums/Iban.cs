// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Iban.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Computes the two-digit check sequence of an International Bank Account Number (IBAN) using the ISO 13616
/// algorithm atop ISO 7064 MOD 97-10. This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// An IBAN takes the textual form <c>CCDDBBAN</c> where <c>CC</c> is a two-letter ISO 3166 country code,
/// <c>DD</c> is a two-digit check, and <c>BBAN</c> is the Basic Bank Account Number specific to the issuing
/// country. To compute or validate the check, the first four characters are moved to the end (<c>BBAN + CC + DD</c>),
/// letters are expanded to their numeric value (<c>'A'</c>=10 … <c>'Z'</c>=35), and the resulting decimal string
/// is evaluated under ISO 7064 MOD 97-10.
/// </para>
/// <para>
/// The streaming surface accepts a body comprising <c>CC + BBAN</c> — that is, the IBAN with the check placeholder
/// <i>omitted</i> — and produces the two check digits via <see cref="GetCurrentCheckDigits(Span{char})" />.
/// <see cref="IsValid(ReadOnlySpan{char})" /> accepts the complete <c>CC + DD + BBAN</c> form; whitespace and
/// other formatting characters are <b>not</b> tolerated, in keeping with ISO 13616 strict mode. Callers should
/// normalise textual IBANs (for example, by removing spaces) before passing them in.
/// </para>
/// <para>
/// <b>Worked example.</b> For the body <c>"GBWEST12345698765432"</c>, the computed check is <c>"82"</c>, and the
/// resulting IBAN <c>"GB82WEST12345698765432"</c> is therefore valid.
/// </para>
/// <note type="important">This algorithm is <b>not</b> cryptographically secure and should <b>not</b> be used for
/// password hashing, digital signatures, or integrity validation in security-sensitive applications.</note>
/// </remarks>
public sealed class Iban
    : MultiCharCheckDigitAlgorithm
{
    /// <summary>The fixed check-code length of <c>2</c> decimal digits.</summary>
    public const int CheckDigits = 2;

    /// <summary>The length of the country-code prefix (<c>2</c> letters).</summary>
    public const int CountryCodeLength = 2;

    private char _cc0;
    private char _cc1;
    private int _consumed;
    private int _rBban;

    /// <summary>
    /// Initializes a new instance of the <see cref="Iban" /> class.
    /// </summary>
    public Iban()
    {
    }

    /// <inheritdoc />
    public override string AlgorithmName => "IBAN";

    /// <inheritdoc />
    public override CheckDigitInputAlphabet InputAlphabet => CheckDigitInputAlphabet.AlphanumericUppercase;

    /// <inheritdoc />
    public override int CheckLength => CheckDigits;

    /// <inheritdoc />
    public override void Append(ReadOnlySpan<char> body)
    {
        int consumed = _consumed;
        int r = _rBban;

        for (int i = 0; i < body.Length; i++)
        {
            char ch = body[i];
            if ((uint)(ch - '0') > 9u && (uint)(ch - 'A') > 25u)
                ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(ch, nameof(body));

            if (consumed == 0)
            {
                _cc0 = ch;
            }
            else if (consumed == 1)
            {
                _cc1 = ch;
            }
            else
            {
                r = FoldChar(r, ch);
            }

            consumed++;
        }

        _consumed = consumed;
        _rBban = r;
    }

    /// <inheritdoc />
    public override void Reset()
    {
        _cc0 = default;
        _cc1 = default;
        _consumed = 0;
        _rBban = 0;
    }

    /// <inheritdoc />
    public override int GetCurrentCheckDigits(Span<char> destination)
    {
        if (destination.Length < CheckLength)
            throw new ArgumentException($"Destination span must be at least {CheckLength} characters long.", nameof(destination));

        int r = _rBban;
        if (_consumed >= 1) r = FoldChar(r, _cc0);
        if (_consumed >= 2) r = FoldChar(r, _cc1);

        // Fold in the two trailing placeholder zero digits.
        r = (r * 100) % 97;
        int check = (98 - r) % 97;
        destination[0] = (char)('0' + (check / 10));
        destination[1] = (char)('0' + (check % 10));
        return CheckLength;
    }

    /// <summary>
    /// Computes the IBAN check digits for the supplied country-code-plus-BBAN body without allocating a
    /// streaming instance.
    /// </summary>
    /// <param name="body">The body characters — the complete IBAN minus its two-digit check sequence.</param>
    /// <returns>The check code as a two-character string of ASCII decimal digits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="body" /> contains any character outside the alphanumeric uppercase alphabet.
    /// </exception>
    public static string Compute(ReadOnlySpan<char> body)
    {
        Iban iban = new();
        iban.Append(body);
        return iban.GetCurrentCheckDigits();
    }

    /// <summary>
    /// Determines whether the supplied IBAN, in its contiguous (unspaced) canonical form, is consistent under
    /// ISO 13616.
    /// </summary>
    /// <param name="iban">
    /// The complete IBAN in canonical form — two-letter country code, two-digit check, then the BBAN — with no
    /// internal whitespace or formatting characters.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the IBAN is empty, or if it is at least four characters long, alphanumeric,
    /// and its rearranged decimal expansion has remainder <c>1</c> modulo 97; otherwise, <see langword="false" />.
    /// </returns>
    public static bool IsValid(ReadOnlySpan<char> iban)
    {
        if (iban.IsEmpty) return true;
        if (iban.Length < 4) return false;

        // Positions 2 and 3 must be ASCII decimal digits (the check code).
        if ((uint)(iban[2] - '0') > 9u || (uint)(iban[3] - '0') > 9u) return false;

        // Rearrangement: BBAN + CC + DD — stream through MOD 97-10.
        int r = 0;
        for (int i = 4; i < iban.Length; i++)
        {
            if (!TryFold(ref r, iban[i])) return false;
        }

        for (int i = 0; i < 4; i++)
        {
            if (!TryFold(ref r, iban[i])) return false;
        }

        return r == 1;
    }

    private static int FoldChar(int r, char ch)
    {
        if ((uint)(ch - '0') <= 9u)
            return ((r * 10) + (ch - '0')) % 97;
        return ((r * 100) + (ch - 'A' + 10)) % 97;
    }

    private static bool TryFold(ref int r, char ch)
    {
        if ((uint)(ch - '0') <= 9u)
        {
            r = ((r * 10) + (ch - '0')) % 97;
            return true;
        }

        if ((uint)(ch - 'A') <= 25u)
        {
            r = ((r * 100) + (ch - 'A' + 10)) % 97;
            return true;
        }

        return false;
    }
}
