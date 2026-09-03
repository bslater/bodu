// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiEncodingResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Text;

#if MSG
namespace Bodu.Formats.Outlook.Msg;
#elif OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#endif

/// <summary>
/// Resolves the <see cref="Encoding" /> used to decode code-page (<c>PT_STRING8</c>) string properties.
/// </summary>
/// <remarks>
/// The type constructor registers <see cref="CodePagesEncodingProvider" /> so the Windows code pages that dominate
/// real-world messages (Windows-1252, Shift-JIS, and the rest) resolve on all platforms. Resolution prefers the message
/// code page, then the internet code page, then falls back to Windows-1252 — the historical default for messages that
/// declare nothing (Latin-1 when the code-page provider is unavailable). The UTF-16 code pages (1200 and 1201) are not
/// usable for <c>PT_STRING8</c> payloads and fall through to the next candidate; resolved encodings are cached per
/// code page. Every method always returns an encoding and never throws. This file lives in <c>Bodu.Formats.Outlook/shared/</c> and is source-compiled into each Outlook
/// format reader — the same code-page properties govern <c>PT_STRING8</c> decoding in a <c>.msg</c> container and a
/// PST property context; the consuming project selects the namespace via its <c>DefineConstants</c>.
/// </remarks>
internal static class MapiEncodingResolver
{
    /// <summary>The Windows-1252 code page used when a message declares no usable code page.</summary>
    private const int FallbackCodePage = 1252;

    /// <summary>The UTF-8 code page, served by a preamble-free instance.</summary>
    private const int Utf8CodePage = 65001;

    /// <summary>The UTF-16 little-endian code page, which cannot describe a code-page string.</summary>
    private const int Utf16CodePage = 1200;

    /// <summary>The UTF-16 big-endian code page, which cannot describe a code-page string.</summary>
    private const int Utf16BigEndianCodePage = 1201;

    /// <summary>The resolved encoding per code page, or <see langword="null" /> for a code page that does not resolve.</summary>
    private static readonly ConcurrentDictionary<int, Encoding?> s_cache = new();

    /// <summary>The UTF-8 encoding without a preamble.</summary>
    private static readonly Encoding s_utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    /// <summary>The encoding served when nothing declared resolves.</summary>
    private static readonly Encoding s_fallback;

    /// <summary>
    /// Initializes static members of the <see cref="MapiEncodingResolver" /> class.
    /// </summary>
    /// <remarks>
    /// Registers <see cref="CodePagesEncodingProvider" /> exactly once per process (registration is idempotent) and
    /// resolves the fallback encoding once, backstopped by Latin-1 should the provider be unavailable.
    /// </remarks>
    static MapiEncodingResolver()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        s_fallback = TryGetEncoding(FallbackCodePage) ?? Encoding.Latin1;
    }

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
            ?? s_fallback;

    /// <summary>
    /// Resolves the encoding of an HTML body stored as bytes: the internet code page is authoritative for HTML, so it
    /// is tried before the message code page — the reverse of the precedence code-page strings use.
    /// </summary>
    /// <param name="internetCodePage">The declared internet code page, when present.</param>
    /// <param name="messageCodePage">The declared message code page, when present.</param>
    /// <returns>
    /// The encoding for the first declared code page that resolves, internet first; Windows-1252 when neither is
    /// declared or neither resolves.
    /// </returns>
    internal static Encoding GetHtmlEncoding(int? internetCodePage, int? messageCodePage) =>
        GetEncoding(internetCodePage, messageCodePage);

    /// <summary>
    /// Resolves the string encoding for a decoded property collection, honoring the inheritance rule: a storage that
    /// declares no code page of its own uses its parent's encoding.
    /// </summary>
    /// <param name="properties">The storage's decoded properties.</param>
    /// <param name="inherited">The parent's encoding, or <see langword="null" /> at the root.</param>
    /// <returns>The encoding for the storage's code-page strings.</returns>
    internal static Encoding Resolve(MapiPropertyCollection properties, Encoding? inherited)
    {
        int? messageCodePage = properties.GetInt32(MapiPropertyIds.MessageCodepage);
        int? internetCodePage = properties.GetInt32(MapiPropertyIds.InternetCodepage);

        if (messageCodePage is null && internetCodePage is null && inherited is not null)
            return inherited;

        return GetEncoding(messageCodePage, internetCodePage);
    }

    /// <summary>
    /// Attempts to resolve a code page to an encoding.
    /// </summary>
    /// <param name="codePage">The code page, when declared.</param>
    /// <returns>
    /// The encoding, or <see langword="null" /> when undeclared, out of range, unknown, or a UTF-16 code page.
    /// </returns>
    private static Encoding? TryGetEncoding(int? codePage)
    {
        if (codePage is not int value || value <= 0 || value > 65535 || value is Utf16CodePage or Utf16BigEndianCodePage)
            return null;

        if (value == Utf8CodePage)
            return s_utf8;

        return s_cache.GetOrAdd(value, static key =>
        {
            try
            {
                return Encoding.GetEncoding(key);
            }
            catch (NotSupportedException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        });
    }
}
