// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContainer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook.Pst;

/// <summary>
/// Translates a container exception into the reader's own, so that a container-level failure surfaces as
/// <see cref="OutlookPstFormatException" /> rather than as a <see cref="PstFileException" />.
/// </summary>
/// <remarks>
/// A corrupt page, block, heap, or table can fail at any point after the store opened - when a folder's table is
/// enumerated, an object's properties are decoded, or a payload is streamed. The public members of
/// <see cref="OutlookMailStore" /> and of the views it hands out catch the container's family and rethrow it through
/// <see cref="Wrap" />, keeping the documented exception contract: callers observe
/// <see cref="OutlookFormatException" /> descendants only, with the container exception preserved as the inner
/// exception, as the <c>.msg</c> reader does for the compound-file container.
/// </remarks>
internal static class PstContainer
{
    /// <summary>
    /// Wraps a container exception in the reader's format exception.
    /// </summary>
    /// <param name="exception">The container exception.</param>
    /// <returns>The exception to throw, with <paramref name="exception" /> as its inner exception.</returns>
    /// <remarks>
    /// The header is read only when the store opens, so a header fault means the source is not a valid PST file; an
    /// unsupported variant or content encoding keeps its own message; anything else is a malformed container, whatever
    /// the reader was doing when it met it.
    /// </remarks>
    internal static OutlookPstFormatException Wrap(PstFileException exception)
    {
        string message = exception switch
        {
            PstUnsupportedFormatException => OutlookPstResourceStrings.Format_Invalid_PstContainerUnsupported,
            { Error: PstFileError.InvalidHeader } => OutlookPstResourceStrings.Format_Invalid_PstNotPst,
            _ => OutlookPstResourceStrings.Format_Invalid_PstContainerMalformed,
        };

        return new OutlookPstFormatException(message, exception);
    }
}
