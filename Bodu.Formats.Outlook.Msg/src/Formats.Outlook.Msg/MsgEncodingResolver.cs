// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgEncodingResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Resolves the <see cref="Encoding" /> used to decode code-page (<c>PT_STRING8</c>) string properties.
/// </summary>
/// <remarks>
/// The type constructor registers <see cref="CodePagesEncodingProvider" /> so the Windows code pages that dominate
/// real-world messages (Windows-1252, Shift-JIS, and the rest) resolve on all platforms. Resolution prefers the message
/// code page, then the internet code page, then falls back to Windows-1252 — the historical default for messages that
/// declare nothing.
/// </remarks>
internal static class MsgEncodingResolver
{
    /// <summary>The Windows-1252 code page used when a message declares no usable code page.</summary>
    private const int FallbackCodePage = 1252;

    /// <summary>
    /// Initializes static members of the <see cref="MsgEncodingResolver" /> class.
    /// </summary>
    /// <remarks>
    /// Registers <see cref="CodePagesEncodingProvider" /> exactly once per process; registration is idempotent.
    /// </remarks>
    static MsgEncodingResolver() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    /// <summary>
    /// Resolves the string encoding for a message.
    /// </summary>
    /// <param name="messageCodePage">The declared message code page, when present.</param>
    /// <param name="internetCodePage">The declared internet code page, when present.</param>
    /// <returns>
    /// The encoding for the first declared code page that resolves; Windows-1252 when neither is declared or neither
    /// resolves.
    /// </returns>
    internal static Encoding GetEncoding(int? messageCodePage, int? internetCodePage) =>
        TryGetEncoding(messageCodePage)
            ?? TryGetEncoding(internetCodePage)
            ?? Encoding.GetEncoding(FallbackCodePage);

    /// <summary>
    /// Attempts to resolve a code page to an encoding.
    /// </summary>
    /// <param name="codePage">The code page, when declared.</param>
    /// <returns>The encoding, or <see langword="null" /> when undeclared, out of range, or unknown.</returns>
    private static Encoding? TryGetEncoding(int? codePage)
    {
        if (codePage is not int value || value <= 0 || value > 65535)
            return null;

        try
        {
            return Encoding.GetEncoding(value);
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
