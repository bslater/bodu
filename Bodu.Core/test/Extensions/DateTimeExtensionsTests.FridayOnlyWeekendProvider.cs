// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.FridayOnlyWeekendProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Test-only <see cref="IWeekendDefinitionProvider" /> in which only Friday is classified as a weekend day.
    /// Used to exercise the provider-backed overloads of the weekday/weekend extensions.
    /// </summary>
    public sealed class FridayOnlyWeekendProvider
        : IWeekendDefinitionProvider
    {

        public bool IsWeekend(DayOfWeek dayOfWeek) => dayOfWeek == DayOfWeek.Friday;

    }

}
