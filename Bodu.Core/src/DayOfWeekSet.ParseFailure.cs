// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSet.ParseFailure.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public readonly partial struct DayOfWeekSet
{
    /// <summary>
    /// Specifies why a text is not a seven-character mask.
    /// </summary>
    private enum ParseFailure
    {
        /// <summary>
        /// The text is a mask.
        /// </summary>
        None,

        /// <summary>
        /// The text is not seven characters long.
        /// </summary>
        Length,

        /// <summary>
        /// A character fits neither the day at its position nor the placeholder.
        /// </summary>
        Character,
    }
}
