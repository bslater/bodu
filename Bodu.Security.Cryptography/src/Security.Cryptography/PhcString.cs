// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PhcString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides helpers for the PHC string format shared by the Argon2 and scrypt password-hash encodings — most notably
/// the unpadded Base64 ("B64") encoding the format mandates for salt and hash fields.
/// </summary>
/// <remarks>
/// The PHC string format is the de facto modular-crypt encoding documented at
/// <see href="https://github.com/P-H-C/phc-string-format/blob/master/phc-sf-spec.md" />. Its B64 variant uses the
/// standard Base64 alphabet (with <c>+</c> and <c>/</c>) but omits the <c>=</c> padding characters.
/// </remarks>
internal static class PhcString
{
    /// <summary>
    /// Encodes <paramref name="data" /> as unpadded Base64 using the standard alphabet.
    /// </summary>
    /// <param name="data">The bytes to encode.</param>
    /// <returns>The Base64 text with any trailing <c>=</c> padding removed.</returns>
    internal static string EncodeBase64(ReadOnlySpan<byte> data) =>
        Convert.ToBase64String(data).TrimEnd('=');

    /// <summary>
    /// Decodes unpadded Base64 text produced by <see cref="EncodeBase64(ReadOnlySpan{byte})" />.
    /// </summary>
    /// <param name="text">The unpadded Base64 text.</param>
    /// <returns>The decoded bytes.</returns>
    /// <exception cref="FormatException"><paramref name="text" /> is not valid Base64.</exception>
    internal static byte[] DecodeBase64(string text)
    {
        var padding = (4 - (text.Length % 4)) % 4;
        if (padding == 3)
            throw new FormatException(CryptoResourceStrings.Format_Invalid_PhcString);

        var padded = padding == 0 ? text : text + new string('=', padding);

        try
        {
            return Convert.FromBase64String(padded);
        }
        catch (FormatException)
        {
            throw new FormatException(CryptoResourceStrings.Format_Invalid_PhcString);
        }
    }

    /// <summary>
    /// Splits a PHC string into its <c>$</c>-delimited fields, validating that it begins with an empty leading field.
    /// </summary>
    /// <param name="encoded">The PHC string to split.</param>
    /// <returns>The fields following the leading <c>$</c>.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="encoded" /> is <see langword="null" /> or does not begin with <c>$</c>.
    /// </exception>
    internal static string[] SplitFields(string encoded)
    {
        if (string.IsNullOrEmpty(encoded) || encoded[0] != '$')
            throw new FormatException(CryptoResourceStrings.Format_Invalid_PhcString);

        return encoded.Split('$');
    }

    /// <summary>
    /// Throws a <see cref="FormatException" /> describing a malformed PHC string.
    /// </summary>
    /// <returns>This method never returns; the return type allows it to be used in a <c>throw</c> expression.</returns>
    /// <exception cref="FormatException">Always thrown.</exception>
    [System.Diagnostics.CodeAnalysis.DoesNotReturn]
    internal static FormatException Invalid() =>
        throw new FormatException(CryptoResourceStrings.Format_Invalid_PhcString);
}
