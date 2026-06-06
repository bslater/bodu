// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCategory.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides a coarse-grained classification of a notable date in the notable-date document model.
/// </summary>
/// <remarks>
/// <para>
/// A category is a presentation and grouping aid, not an identity. A notable date is identified by its stable id; two
/// notable dates may share a category freely. A rule may override the category inherited from its parent notable date.
/// </para>
/// </remarks>
/// <seealso cref="NotableDateFilter" /> <seealso href="../guides/calendar/notable-dates.html">Using NotableDateService
/// (guide)</seealso>
public enum NotableDateCategory
{
    /// <summary>
    /// No primary category is assigned.
    /// </summary>
    None = 0,

    /// <summary>
    /// A public holiday, typically recognized with an official day off work or school.
    /// </summary>
    PublicHoliday,

    /// <summary>
    /// A bank or financial-institution holiday, which may differ from the public holiday schedule.
    /// </summary>
    BankHoliday,

    /// <summary>
    /// A secular or cultural observance that does not belong to a more specific category.
    /// </summary>
    Observance,

    /// <summary>
    /// A date dedicated to remembering significant historical events, individuals, or groups.
    /// </summary>
    Remembrance,

    /// <summary>
    /// A cultural celebration tied to a specific community, ethnicity, or tradition.
    /// </summary>
    Cultural,

    /// <summary>
    /// A religious observance, ceremony, or holy day significant within a specific faith or denomination.
    /// </summary>
    Religious,

    /// <summary>
    /// A seasonal marker such as a solstice, equinox, or daylight-saving transition.
    /// </summary>
    Seasonal,

    /// <summary>
    /// A civic or national commemoration that may not carry a statutory day off.
    /// </summary>
    Civic,

    /// <summary>
    /// A school holiday or term boundary observed by educational institutions.
    /// </summary>
    School,

    /// <summary>
    /// An observance specific to a subnational region, state, or province.
    /// </summary>
    Regional,

    /// <summary>
    /// A notable date that does not fit any other primary category.
    /// </summary>
    Other,
}
