// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiSubject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if MSG
namespace Bodu.Formats.Outlook.Msg;
#elif OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#endif

/// <summary>
/// Normalizes a stored <c>PidTagSubject</c> value for presentation.
/// </summary>
/// <remarks>
/// Outlook stores a subject with a prefix (<c>RE:</c>, <c>FW:</c>, and the like) as U+0001, then a character whose
/// code is the prefix length plus one, then the full subject text (MS-PST §2.4.5.1.2; the same encoding appears in
/// other MAPI stores). The marker pair is stripped and the full text — prefix included — is returned; a subject that
/// does not carry the marker is returned as stored. The property collection always surfaces the stored value. This
/// file lives in <c>Bodu.Formats.Outlook/shared/</c> and is source-compiled into each Outlook format reader.
/// </remarks>
internal static class MapiSubject
{
    /// <summary>The marker character that introduces a prefixed subject.</summary>
    private const char PrefixMarker = '\u0001';

    /// <summary>
    /// Removes the subject-prefix marker pair from a stored subject.
    /// </summary>
    /// <param name="subject">The stored subject, or <see langword="null" />.</param>
    /// <returns>The subject text without the marker pair, or the input when it carries none.</returns>
    internal static string? Normalize(string? subject) =>
        subject is { Length: >= 2 } && subject[0] == PrefixMarker && subject[1] <= subject.Length - 1
            ? subject[2..]
            : subject;
}
