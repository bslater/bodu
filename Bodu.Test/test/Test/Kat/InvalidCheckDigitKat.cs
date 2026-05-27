// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvalidCheckDigitKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a check-digit algorithm's <c>IsValid</c> surface, asserting that the
/// algorithm correctly rejects a malformed or tampered full value.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="FullValue">
/// The complete payload including (possibly incorrect) check digit, passed to the algorithm's validation method.
/// </param>
/// <param name="Reason">
/// A short human-readable explanation of why this row should be rejected, for example <c>"transposed digits"</c> or
/// <c>"single-digit corruption"</c>.
/// </param>
public sealed record InvalidCheckDigitKat(
    string Name,
    string FullValue,
    string Reason) : IKat;
