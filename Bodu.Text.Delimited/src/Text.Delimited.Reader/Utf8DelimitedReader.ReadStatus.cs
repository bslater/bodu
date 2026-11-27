// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReader.ReadStatus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Reader;

public ref partial struct Utf8DelimitedReader
{
    /// <summary>
    /// Enumerates the outcomes of reading a field or a record from the cursor.
    /// </summary>
    private enum ReadStatus
    {
        /// <summary>
        /// The field or record was read whole.
        /// </summary>
        Complete,

        /// <summary>
        /// Text other than the delimiter or a line ending follows a field's closing quote.
        /// </summary>
        TextAfterClosingQuote,

        /// <summary>
        /// No record remains: the end of the input was reached.
        /// </summary>
        EndOfInput,

        /// <summary>
        /// The block, which is not the last, ends before the field or record does.
        /// </summary>
        NeedMoreData,
    }
}
