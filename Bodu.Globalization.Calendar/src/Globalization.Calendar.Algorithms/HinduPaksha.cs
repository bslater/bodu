// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduPaksha.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Identifies one of the two fortnights (paksha) of a Hindu lunar month.
/// </summary>
/// <remarks>
/// <para>
/// Each Hindu lunar month is divided into two fortnights of fifteen tithis (lunar days) each. The bright fortnight (Shukla Paksha)
/// begins at the new moon and ends at the full moon. The dark fortnight (Krishna Paksha) begins just after the full moon and ends
/// at the following new moon (Amavasya).
/// </para>
/// </remarks>
public enum HinduPaksha
{
	/// <summary>
	/// The bright fortnight (Shukla Paksha), running from the new moon (Amavasya) to the full moon (Purnima). Tithis are numbered
	/// 1 (Pratipada) through 15 (Purnima).
	/// </summary>
	Shukla = 0,

	/// <summary>
	/// The dark fortnight (Krishna Paksha), running from just after the full moon to the next new moon. Tithis are numbered 1
	/// (Pratipada) through 15 (Amavasya).
	/// </summary>
	Krishna = 1,
}
