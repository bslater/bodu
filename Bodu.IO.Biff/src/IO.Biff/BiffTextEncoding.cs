// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffTextEncoding.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.IO.Biff;

/// <summary>
/// Resolves the <see cref="Encoding" /> for a BIFF code page, including the two private values the format uses for
/// Apple Roman and the pre-BIFF5 Windows ANSI code page.
/// </summary>
internal static class BiffTextEncoding
{
    /// <summary>The <c>CODEPAGE</c> value BIFF uses for Apple Roman (Macintosh) text.</summary>
    private const int AppleRomanMarker = 0x8000;

    /// <summary>The <c>CODEPAGE</c> value BIFF2 and BIFF3 used for Windows ANSI text.</summary>
    private const int LegacyAnsiMarker = 0x8001;

    /// <summary>The code page number of the Apple Roman encoding.</summary>
    private const int AppleRomanCodePage = 10000;

    /// <summary>
    /// Initializes static members of the <see cref="BiffTextEncoding" /> class.
    /// </summary>
    /// <remarks>
    /// Registers the code-page encoding provider so legacy single- and double-byte code pages resolve on every runtime.
    /// </remarks>
    static BiffTextEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Maps a raw <c>CODEPAGE</c> record value to the code page number the encoding provider recognizes.
    /// </summary>
    /// <param name="rawCodePage">The value carried by the <c>CODEPAGE</c> record.</param>
    /// <returns>The Windows code page number.</returns>
    public static int Normalize(int rawCodePage) =>
        rawCodePage switch
        {
            AppleRomanMarker => AppleRomanCodePage,
            LegacyAnsiMarker => BiffLimits.DefaultCodePage,
            0 => BiffLimits.DefaultCodePage,
            _ => rawCodePage,
        };

    /// <summary>
    /// Resolves the encoding for a code page.
    /// </summary>
    /// <param name="codePage">The Windows code page number, or a raw BIFF marker value.</param>
    /// <returns>The encoding that decodes byte strings written under <paramref name="codePage" />.</returns>
    /// <exception cref="BiffFormatException">
    /// Thrown when <paramref name="codePage" /> is not a code page the runtime can resolve.
    /// </exception>
    public static Encoding GetEncoding(int codePage)
    {
        int normalized = Normalize(codePage);
        if (normalized == BiffLimits.UnicodeCodePage)
            return Encoding.Unicode;

        try
        {
            return Encoding.GetEncoding(normalized);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            throw new BiffFormatException(
                string.Format(CultureInfo.CurrentCulture, BiffResourceStrings.Format_Invalid_BiffCodePage, codePage),
                ex);
        }
    }
}
