// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixtureDateSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Plugins.TestPlugin.Dependency;

/// <summary>
/// A dependency assembly consumed by the dependent fixture plugin. It carries no plugin attribute, so it also serves
/// as the missing-attribute fixture for the loader tests.
/// </summary>
public static class FixtureDateSource
{
    /// <summary>
    /// Returns the fixture date, 4 July of the requested year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The fixture date.</returns>
    public static DateOnly For(int year) =>
        new(year, 7, 4);
}
