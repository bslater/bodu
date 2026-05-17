// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsFirstDateOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls on the first day of its calendar month.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" /> represents the first day of its month; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method evaluates whether the <see cref="DateOnly.Day" /> component is equal to <c>1</c>.
    /// </para>
    /// </remarks>
    public static bool IsFirstDateOfMonth(this DateOnly date) => date.Day == 1;
}
