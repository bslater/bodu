// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeFormatInfoExtensions.LastDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu.Globalization.Extensions;

public static partial class DateTimeFormatInfoExtensions
{
    /// <summary>
    /// Returns the last day of the week based on the specified <see cref="DateTimeFormatInfo"/>.
    /// </summary>
    /// <param name="info">The <see cref="DateTimeFormatInfo"/> containing week configuration.</param>
    /// <returns>The last <see cref="DayOfWeek"/> of the week according to the provided culture.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="info"/> is <see langword="null"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DayOfWeek LastDayOfWeek(this DateTimeFormatInfo info)
    {
        ThrowHelper.ThrowIfNull(info);

        return (DayOfWeek)(((int)info.FirstDayOfWeek + 6) % 7);
    }
}