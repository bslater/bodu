// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CheckDigitKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a check-digit algorithm (Luhn, Damm, ABA, EAN, GTIN, IBAN, ISBN, ISIN, LEI,
/// ISO 7064). A row pairs the body payload (without the check digit) with the expected check digit value and the
/// canonical <see cref="FullValue" /> representation including the check digit.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Payload">The body characters/digits supplied to the algorithm.</param>
/// <param name="CheckDigit">
/// The expected check digit (or multi-character check code), as a string so the same record fits single-character and
/// multi-character variants.
/// </param>
/// <param name="FullValue">
/// The canonical full representation including the check digit. For some algorithms (IBAN) the canonical form
/// interleaves the check digits rather than appending them.
/// </param>
public sealed record CheckDigitKat(
    string Name,
    string Payload,
    string CheckDigit,
    string FullValue) : IKat;
