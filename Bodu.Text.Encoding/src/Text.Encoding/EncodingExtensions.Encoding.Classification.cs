// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EncodingExtensions.Encoding.Classification.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public static partial class EncodingExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> is UTF-8 (Windows codepage 65001).
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns><see langword="true" /> when the codepage is 65001; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool IsUtf8(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.CodePage == 65001;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> is UTF-16 little endian (Windows
    /// codepage 1200, the value of <see cref="System.Text.Encoding.Unicode" />).
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns><see langword="true" /> when the codepage is 1200; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool IsUtf16LittleEndian(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.CodePage == 1200;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> is UTF-16 big endian (Windows codepage
    /// 1201, the value of <see cref="System.Text.Encoding.BigEndianUnicode" />).
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns><see langword="true" /> when the codepage is 1201; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool IsUtf16BigEndian(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.CodePage == 1201;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> is UTF-32 little endian (Windows
    /// codepage 12000, the value of <see cref="System.Text.Encoding.UTF32" />).
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns><see langword="true" /> when the codepage is 12000; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool IsUtf32LittleEndian(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.CodePage == 12000;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> is UTF-32 big endian (Windows codepage
    /// 12001).
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns><see langword="true" /> when the codepage is 12001; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool IsUtf32BigEndian(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.CodePage == 12001;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> is any UTF encoding (UTF-8, UTF-16 LE,
    /// UTF-16 BE, UTF-32 LE, or UTF-32 BE).
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when the codepage is one of 65001, 1200, 1201, 12000, or 12001; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool IsAnyUtf(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.CodePage is 65001 or 1200 or 1201 or 12000 or 12001;
    }

    /// <summary>
    /// Returns a value indicating whether <paramref name="encoding" /> is US-ASCII (Windows codepage 20127).
    /// </summary>
    /// <param name="encoding">The encoding to inspect.</param>
    /// <returns><see langword="true" /> when the codepage is 20127; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static bool IsAscii(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        return encoding.CodePage == 20127;
    }

    /// <summary>
    /// Returns a human-readable display name for <paramref name="encoding" /> that distinguishes BOM-emitting
    /// UTF variants from their plain counterparts (for example, <c>"UTF-8-BOM"</c> versus <c>"UTF-8"</c>).
    /// </summary>
    /// <param name="encoding">The encoding to describe.</param>
    /// <returns>
    /// A short, stable name. UTF variants return the canonical short form with a <c>-BOM</c> suffix when the
    /// encoding emits a preamble; other encodings fall back to <see cref="System.Text.Encoding.WebName" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="encoding" /> is <see langword="null" />.
    /// </exception>
    public static string GetDisplayName(this System.Text.Encoding encoding)
    {
        ThrowHelper.ThrowIfNull(encoding);

        string suffix = encoding.HasPreamble() ? "-BOM" : string.Empty;
        return encoding.CodePage switch
        {
            65001 => "UTF-8" + suffix,
            1200 => "UTF-16LE" + suffix,
            1201 => "UTF-16BE" + suffix,
            12000 => "UTF-32LE" + suffix,
            12001 => "UTF-32BE" + suffix,
            20127 => "US-ASCII",
            _ => encoding.WebName,
        };
    }
}
