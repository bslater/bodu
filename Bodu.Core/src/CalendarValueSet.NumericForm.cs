// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSet.NumericForm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

internal static partial class CalendarValueSet
{
    /// <summary>
    /// Specifies a text form that every numeric calendar value set writes and reads.
    /// </summary>
    internal enum NumericForm
    {
        /// <summary>
        /// The canonical list, each run of two or more consecutive values as an inclusive range; format <c>G</c>.
        /// </summary>
        List,

        /// <summary>
        /// Every value listed, without ranges; format <c>L</c>.
        /// </summary>
        Values,

        /// <summary>
        /// One <c>0</c> or <c>1</c> per value of the domain, lowest first; format <c>B</c>, <c>0</c>, <c>1</c> or
        /// <c>01</c>.
        /// </summary>
        Binary,
    }
}
