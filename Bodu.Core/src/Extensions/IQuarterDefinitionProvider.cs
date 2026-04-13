// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IQuarterDefinitionProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

/// <summary>
/// Defines a provider interface for custom quarter calculation logic based on <see cref="DateTime"/> or <see cref="DateOnly"/> values.
/// </summary>
/// <remarks>
/// Implement this interface to support custom fiscal or logical quarter systems in methods that accept a
/// <see cref="CalendarQuarterDefinition.Custom"/> value. This interface is used by a wide range of APIs that require quarter-related
/// computations for both <see cref="DateTime"/> and <see cref="DateOnly"/> inputs.
/// <para>
/// Example use cases include 4–4–5 calendars, academic terms, retail fiscal calendars, or historical reporting quarters that do not
/// align with calendar or standard financial quarters.
/// </para>
/// <para>Methods that support this interface include:</para>
/// <list type="bullet">
/// <item><see cref="DateTimeExtensions.Quarter(DateTime, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateTimeExtensions.FirstDayOfQuarter(DateTime, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateTimeExtensions.LastDayOfQuarter(DateTime, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateTimeExtensions.IsFirstDayOfQuarter(DateTime, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateTimeExtensions.IsLastDayOfQuarter(DateTime, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateOnlyExtensions.Quarter(DateOnly, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateOnlyExtensions.FirstDayOfQuarter(DateOnly, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateOnlyExtensions.LastDayOfQuarter(DateOnly, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateOnlyExtensions.IsFirstDayOfQuarter(DateOnly, IQuarterDefinitionProvider)"/></item>
/// <item><see cref="DateOnlyExtensions.IsLastDayOfQuarter(DateOnly, IQuarterDefinitionProvider)"/></item>
/// </list>
/// <para>
/// Implementers must define logic for determining both the quarter number and the corresponding start and end dates for a given date or
/// quarter index. Overloads support both <see cref="DateTime"/> and <see cref="DateOnly"/> inputs to allow compatibility with
/// time-specific or date-only calculations.
/// </para>
/// </remarks>
public interface IQuarterDefinitionProvider
{
    /// <summary>
    /// Returns the quarter number (1–4) that contains the specified <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The input <see cref="DateTime"/> for which to determine the quarter.</param>
    /// <returns>An integer in the range 1 to 4 representing the quarter that includes <paramref name="dateTime"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="dateTime"/> does not fall within a valid or recognized quarter definition.
    /// </exception>
    int GetQuarter(DateTime dateTime);

    /// <summary>
    /// Returns the quarter number (1–4) that contains the specified <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="dateOnly">The input <see cref="DateOnly"/> for which to determine the quarter.</param>
    /// <returns>An integer in the range 1 to 4 representing the quarter that includes <paramref name="dateOnly"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="dateOnly"/> does not fall within a valid or recognized quarter definition.
    /// </exception>
    int GetQuarter(DateOnly dateOnly);

    /// <summary>
    /// Returns the last day of the quarter that contains the specified <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The input <see cref="DateTime"/> for which to determine the end of the quarter.</param>
    /// <returns>
    /// A <see cref="DateTime"/> set to midnight (00:00:00) on the final day of the quarter that includes <paramref name="dateTime"/>.
    /// The result preserves the original <see cref="DateTime.Kind"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="dateTime"/> does not fall within a valid or recognized quarter.
    /// </exception>
    DateTime GetQuarterEnd(DateTime dateTime);

    /// <summary>
    /// Returns the last day of the specified quarter number (1–4) within the configured fiscal year.
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <returns>
    /// A <see cref="DateTime"/> set to midnight (00:00:00) on the last day of the specified quarter. The result preserves the intended
    /// <see cref="DateTime.Kind"/> and contextual interpretation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="quarter"/> value is not between 1 and 4.</exception>
    DateTime GetQuarterEnd(int quarter);

    /// <summary>
    /// Returns the last day of the quarter that contains the specified <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="dateOnly">The input <see cref="DateOnly"/> for which to determine the end of the quarter.</param>
    /// <returns>A <see cref="DateOnly"/> representing the final day of the quarter that includes <paramref name="dateOnly"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="dateOnly"/> does not fall within a valid or recognized quarter.
    /// </exception>
    DateOnly GetQuarterEndDate(DateOnly dateOnly);

    /// <summary>
    /// Returns the last day of the specified quarter number (1–4) within the configured fiscal year.
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <returns>A <see cref="DateOnly"/> representing the final day of the specified quarter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="quarter"/> value is not between 1 and 4.</exception>
    DateOnly GetQuarterEndDate(int quarter);

    /// <summary>
    /// Returns the first day of the quarter that contains the specified <see cref="DateTime"/>.
    /// </summary>
    /// <param name="dateTime">The input <see cref="DateTime"/> for which to determine the start of the quarter.</param>
    /// <returns>
    /// A <see cref="DateTime"/> set to midnight (00:00:00) on the first day of the quarter that includes <paramref name="dateTime"/>.
    /// The result preserves the original <see cref="DateTime.Kind"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="dateTime"/> does not fall within a valid or recognized quarter.
    /// </exception>
    DateTime GetQuarterStart(DateTime dateTime);

    /// <summary>
    /// Returns the first day of the specified quarter number (1–4) within the configured fiscal year.
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <returns>
    /// A <see cref="DateTime"/> set to midnight (00:00:00) on the first day of the specified quarter. The result preserves the
    /// intended <see cref="DateTime.Kind"/> and contextual interpretation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="quarter"/> value is not between 1 and 4.</exception>
    DateTime GetQuarterStart(int quarter);

    /// <summary>
    /// Returns the first day of the quarter that contains the specified <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="dateOnly">The input <see cref="DateOnly"/> for which to determine the start of the quarter.</param>
    /// <returns>A <see cref="DateOnly"/> representing the first day of the quarter that includes <paramref name="dateOnly"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <paramref name="dateOnly"/> does not fall within a valid or recognized quarter.
    /// </exception>
    DateOnly GetQuarterStartDate(DateOnly dateOnly);

    /// <summary>
    /// Returns the first day of the specified quarter number (1–4) within the configured fiscal year.
    /// </summary>
    /// <param name="quarter">The quarter number (1–4).</param>
    /// <returns>A <see cref="DateOnly"/> representing the first day of the specified quarter.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="quarter"/> value is not between 1 and 4.</exception>
    DateOnly GetQuarterStartDate(int quarter);
}