// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSet.ParseFailure.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

internal static partial class CalendarValueSet
{
    /// <summary>
    /// Specifies why a text is not a calendar value set in the form a reader expected.
    /// </summary>
    internal enum ParseFailure
    {
        /// <summary>
        /// The text is a set in the expected form.
        /// </summary>
        None,

        /// <summary>
        /// The text does not have one character per value of the domain.
        /// </summary>
        Length,

        /// <summary>
        /// A character fits neither the value at its position nor the form's other characters.
        /// </summary>
        Character,

        /// <summary>
        /// The text is not a comma-separated list of the domain's values and ranges.
        /// </summary>
        List,
    }
}
