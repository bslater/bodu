// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodings.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Provides ready-made <see cref="IBinaryEncoding" /> instances for every variant supported by the library.
/// </summary>
/// <remarks>
/// <para>
/// Each property returns a thread-safe singleton bound to a specific variant. The pattern is analogous to
/// <c>System.Text.Encoding.UTF8</c>: a constant instance that can be passed around, stored in configuration, or used
/// through <see cref="IBinaryEncoding" /> by code that must remain agnostic to the concrete encoding.
/// </para>
/// <para>
/// Code that knows the encoding at compile time should prefer the static methods on <see cref="Base16" />,
/// <see cref="Base32" />, <see cref="Base64" />, <see cref="Base58" />, or <see cref="Base85" /> — they are slightly
/// faster (no virtual dispatch) and surface variant-specific options (line breaks, padding control, alternate variants)
/// that the unified interface intentionally hides.
/// </para>
/// </remarks>
public static class BinaryEncodings
{
    /// <summary>
    /// Gets the Adobe Ascii85 encoding (4-byte groups → 5 characters; <c>z</c> shortcut for all-zero groups; partial
    /// trailing groups permitted).
    /// </summary>
    public static IBinaryEncoding Ascii85 { get; } = new Base85VariantAdapter(Base85Variant.Ascii85, "ascii85", "Adobe Ascii85 (! to u plus 'z' all-zero shortcut).");

    /// <summary>
    /// Gets the lower-case hexadecimal (Base16) encoding — the same canonical form
    /// <see cref="global::Bodu.Text.Encoding.Base16.Encode(byte[], BaseFormattingOptions)" /> produces with default
    /// options.
    /// </summary>
    public static IBinaryEncoding Base16Lower { get; } = new Base16LowerAdapter();

    /// <summary>
    /// Gets the upper-case hexadecimal (Base16) encoding — matches RFC 4648 §8 canonical case and
    /// <see cref="System.Convert.ToHexString(byte[])" />.
    /// </summary>
    public static IBinaryEncoding Base16Upper { get; } = new Base16UpperAdapter();

    /// <summary>
    /// Gets the RFC 4648 §6 Standard Base32 encoding (alphabet <c>A-Z 2-7</c>, padded with <c>=</c>).
    /// </summary>
    public static IBinaryEncoding Base32 { get; } = new Base32VariantAdapter(Base32Variant.Standard, "base32", "RFC 4648 §6 Standard Base32 (A-Z, 2-7, padded).");

    /// <summary>
    /// Gets the Crockford Base32 encoding (human-friendly alphabet excluding <c>I</c>, <c>L</c>, <c>O</c>, <c>U</c>; no
    /// padding by default).
    /// </summary>
    public static IBinaryEncoding Base32Crockford { get; } = new Base32VariantAdapter(Base32Variant.Crockford, "base32-crockford", "Crockford Base32 (0-9, A-Z minus I/L/O/U; no padding).");

    /// <summary>
    /// Gets the RFC 4648 §7 base32hex (HexExtended) Base32 encoding (alphabet <c>0-9 A-V</c>, padded with <c>=</c>).
    /// </summary>
    public static IBinaryEncoding Base32Hex { get; } = new Base32VariantAdapter(Base32Variant.HexExtended, "base32hex", "RFC 4648 §7 base32hex (0-9, A-V, padded).");

    /// <summary>
    /// Gets the z-base-32 encoding (human-oriented lowercase alphabet; no padding by default).
    /// </summary>
    public static IBinaryEncoding Base32ZBase32 { get; } = new Base32VariantAdapter(Base32Variant.ZBase32, "z-base-32", "z-base-32 (human-oriented lowercase alphabet; no padding).");

    /// <summary>
    /// Gets the Bitcoin/Flickr Base58 encoding — the alphabet used by Bitcoin addresses, IPFS CIDs, Solana, and many
    /// derivative protocols.
    /// </summary>
    public static IBinaryEncoding Base58 { get; } = new Base58VariantAdapter(Base58Variant.BitcoinFlickr, "base58", "Bitcoin/Flickr Base58 (1-9, A-Z minus O/I, a-z minus l).");

    /// <summary>
    /// Gets the Ripple Base58 encoding (XRP ledger alphabet, a permutation of Bitcoin/Flickr).
    /// </summary>
    public static IBinaryEncoding Base58Ripple { get; } = new Base58VariantAdapter(Base58Variant.Ripple, "base58-ripple", "Ripple Base58 (XRP ledger permutation).");

    /// <summary>
    /// Gets the RFC 4648 §4 Standard Base64 encoding (alphabet <c>A-Z a-z 0-9 + /</c>, padded with <c>=</c>).
    /// </summary>
    public static IBinaryEncoding Base64 { get; } = new Base64VariantAdapter(Base64Variant.Standard, "base64", "RFC 4648 §4 Standard Base64 (A-Z, a-z, 0-9, +, /; padded).");

    /// <summary>
    /// Gets the RFC 2045 MIME Base64 encoding (Standard alphabet with mandatory 76-character line wrapping).
    /// </summary>
    public static IBinaryEncoding Base64Mime { get; } = new Base64VariantAdapter(Base64Variant.Mime, "base64-mime", "RFC 2045 MIME Base64 (76-char wrapped).");

    /// <summary>
    /// Gets the RFC 4648 §5 URL- and filename-safe Base64 encoding (alphabet <c>A-Z a-z 0-9 - _</c>; no padding by
    /// default).
    /// </summary>
    public static IBinaryEncoding Base64UrlSafe { get; } = new Base64VariantAdapter(Base64Variant.UrlSafe, "base64-urlsafe", "RFC 4648 §5 URL- and filename-safe Base64 (-, _; no padding).");

    /// <summary>
    /// Gets the ZeroMQ Z85 encoding (RFC 32; shell-safe alphabet; input must be a multiple of four bytes).
    /// </summary>
    public static IBinaryEncoding Z85 { get; } = new Base85VariantAdapter(Base85Variant.Z85, "z85", "ZeroMQ Z85 (RFC 32 shell-safe alphabet, 4-byte aligned).");

    /// <summary>
    /// Returns the encoding for the supplied case-insensitive name. The recognised names are the values returned by
    /// each instance's <see cref="IBinaryEncoding.Name" />: <c>"base16"</c>, <c>"base16-lower"</c>,
    /// <c>"base16-upper"</c>, <c>"base32"</c>, <c>"base32hex"</c>, <c>"base32-crockford"</c>, <c>"z-base-32"</c>,
    /// <c>"base64"</c>, <c>"base64-urlsafe"</c>, <c>"base64-mime"</c>, <c>"base58"</c>, <c>"base58-ripple"</c>,
    /// <c>"ascii85"</c>, <c>"z85"</c>.
    /// </summary>
    /// <param name="name">The encoding name.</param>
    /// <returns>The matching <see cref="IBinaryEncoding" /> instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when no matching encoding is registered.</exception>
    public static IBinaryEncoding Get(string name)
    {
        ThrowHelper.ThrowIfNull(name);

        return name.ToLowerInvariant() switch
        {
            "base16" or "base16-lower" or "hex" => Base16Lower,
            "base16-upper" or "hex-upper" => Base16Upper,
            "base32" => Base32,
            "base32hex" => Base32Hex,
            "base32-crockford" => Base32Crockford,
            "z-base-32" or "zbase32" => Base32ZBase32,
            "base64" => Base64,
            "base64-urlsafe" or "base64url" => Base64UrlSafe,
            "base64-mime" => Base64Mime,
            "base58" or "base58-bitcoin" or "base58-flickr" => Base58,
            "base58-ripple" => Base58Ripple,
            "ascii85" or "base85" => Ascii85,
            "z85" => Z85,
            _ => throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, EncodingResourceStrings.Arg_Invalid_UnknownEncodingName, name), nameof(name)),
        };
    }

    private sealed class Base16LowerAdapter : IBinaryEncoding
    {
        public string Description => "Base16 / hexadecimal, lower case (default Bodu Base16 form; compatible with Convert.ToHexStringLower).";

        public string Name => "base16-lower";

        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base16.Decode(chars);

        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base16.Encode(bytes);

        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base16.GetMaxDecodedLength(charCount);

        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base16.GetEncodedLength(byteCount);

        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base16.IsValid(source);

        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base16.TryDecode(source, destination, out bytesWritten);

        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base16.TryEncode(source, destination, out charsWritten);
    }

    private sealed class Base16UpperAdapter : IBinaryEncoding
    {
        public string Description => "Base16 / hexadecimal, upper case (RFC 4648 §8 canonical case; compatible with Convert.ToHexString).";

        public string Name => "base16-upper";

        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base16.Decode(chars);

        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base16.Encode(bytes, BaseFormattingOptions.UpperCase);

        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base16.GetMaxDecodedLength(charCount);

        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base16.GetEncodedLength(byteCount, BaseFormattingOptions.UpperCase);

        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base16.IsValid(source);

        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base16.TryDecode(source, destination, out bytesWritten);

        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base16.TryEncode(source, destination, out charsWritten, BaseFormattingOptions.UpperCase);
    }

    private sealed class Base32VariantAdapter : IBinaryEncoding
    {
        private readonly Base32Variant _variant;

        public Base32VariantAdapter(Base32Variant variant, string name, string description)
        {
            _variant = variant;
            Name = name;
            Description = description;
        }

        public string Description { get; }

        public string Name { get; }

        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base32.Decode(chars, _variant);

        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base32.Encode(bytes, _variant);

        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base32.GetMaxDecodedLength(charCount);

        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base32.GetEncodedLength(byteCount, _variant);

        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base32.IsValid(source, _variant);

        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base32.TryDecode(source, destination, out bytesWritten, _variant);

        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base32.TryEncode(source, destination, out charsWritten, _variant);
    }

    private sealed class Base58VariantAdapter : IBinaryEncoding
    {
        private readonly Base58Variant _variant;

        public Base58VariantAdapter(Base58Variant variant, string name, string description)
        {
            _variant = variant;
            Name = name;
            Description = description;
        }

        public string Description { get; }

        public string Name { get; }

        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base58.Decode(chars, _variant);

        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base58.Encode(bytes, _variant);

        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base58.GetMaxDecodedLength(charCount);

        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base58.GetMaxEncodedLength(byteCount);

        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base58.IsValid(source, _variant);

        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base58.TryDecode(source, destination, out bytesWritten, _variant);

        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base58.TryEncode(source, destination, out charsWritten, _variant);
    }

    private sealed class Base64VariantAdapter : IBinaryEncoding
    {
        private readonly Base64Variant _variant;

        public Base64VariantAdapter(Base64Variant variant, string name, string description)
        {
            _variant = variant;
            Name = name;
            Description = description;
        }

        public string Description { get; }

        public string Name { get; }

        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base64.Decode(chars, _variant);

        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base64.Encode(bytes, _variant);

        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base64.GetMaxDecodedLength(charCount);

        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base64.GetEncodedLength(byteCount, _variant);

        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base64.IsValid(source, _variant);

        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base64.TryDecode(source, destination, out bytesWritten, _variant);

        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base64.TryEncode(source, destination, out charsWritten, _variant);
    }

    private sealed class Base85VariantAdapter : IBinaryEncoding
    {
        private readonly Base85Variant _variant;

        public Base85VariantAdapter(Base85Variant variant, string name, string description)
        {
            _variant = variant;
            Name = name;
            Description = description;
        }

        public string Description { get; }

        public string Name { get; }

        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base85.Decode(chars, _variant);

        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base85.Encode(bytes, _variant);

        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base85.GetMaxDecodedLength(charCount);

        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base85.GetMaxEncodedLength(byteCount, _variant);

        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base85.IsValid(source, _variant);

        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base85.TryDecode(source, destination, out bytesWritten, _variant);

        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base85.TryEncode(source, destination, out charsWritten, _variant);
    }
}
