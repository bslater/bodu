// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyDto.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Serialization;

/// <summary>
/// A transport-shaped money representation compatible with Google's <c>google.type.Money</c>: a currency code, a whole
/// number of major units, and a nano-unit fraction.
/// </summary>
/// <param name="CurrencyCode">The ISO 4217 alphabetic currency code.</param>
/// <param name="Units">The whole units of the amount.</param>
/// <param name="Nanos">
/// The fractional units in billionths (10^-9). Must be in the range -999,999,999 to 999,999,999 and share the sign of
/// <paramref name="Units" /> when both are non-zero.
/// </param>
/// <remarks>
/// This is an interchange shape only; it is not the internal money representation. Use <see cref="MoneyDtoAdapter" /> to
/// convert to and from <see cref="Money" />.
/// </remarks>
public sealed record MoneyDto(string CurrencyCode, long Units, int Nanos);
