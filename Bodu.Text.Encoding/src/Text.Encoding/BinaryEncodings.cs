// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BinaryEncodings.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Pre-bound singleton — no allocation per call.
/// IBinaryEncoding hex    = BinaryEncodings.Base16Upper;
/// IBinaryEncoding jwt    = BinaryEncodings.Base64UrlSafe;
/// IBinaryEncoding bitcoin = BinaryEncodings.Base58;
///
/// string encoded = jwt.Encode(payload);
/// byte[] decoded = jwt.Decode(encoded);
///
/// // Look up by name — accepts canonical names and well-known aliases.
/// IBinaryEncoding fromConfig = BinaryEncodings.Get(appConfig["encoding"] ?? "base64");
/// IBinaryEncoding fromAlias  = BinaryEncodings.Get("hex");           // same as Base16Lower
///]]>
/// </code>
/// </example>
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
    /// Gets the RFC 9285 Base45 encoding — the compact alphanumeric encoding used to carry binary data inside a QR
    /// code's Alphanumeric mode (no padding).
    /// </summary>
    public static IBinaryEncoding Base45 { get; } = new Base45Adapter();

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
    /// Gets the GMP-style Base62 encoding (alphabet <c>0-9 A-Z a-z</c>; leading zero bytes preserved as leading
    /// <c>0</c> characters).
    /// </summary>
    public static IBinaryEncoding Base62 { get; } = new Base62Adapter();

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
    /// Gets the Git-style Base85 encoding (Git binary-patch alphabet with compact, self-delimiting partial groups; no
    /// <c>z</c> shortcut or Adobe delimiters).
    /// </summary>
    public static IBinaryEncoding Base85Git { get; } = new Base85VariantAdapter(Base85Variant.GitCompact, "base85-git", "Git-style Base85 alphabet with compact self-delimiting partial groups.");

    /// <summary>
    /// Gets the ZeroMQ Z85 encoding (RFC 32; shell-safe alphabet; input must be a multiple of four bytes).
    /// </summary>
    public static IBinaryEncoding Z85 { get; } = new Base85VariantAdapter(Base85Variant.Z85, "z85", "ZeroMQ Z85 (RFC 32 shell-safe alphabet, 4-byte aligned).");

    /// <summary>
    /// Returns the encoding for the supplied case-insensitive name. The recognised names are the values returned by
    /// each instance's <see cref="IBinaryEncoding.Name" />: <c>"base16"</c>, <c>"base16-lower"</c>,
    /// <c>"base16-upper"</c>, <c>"base32"</c>, <c>"base32hex"</c>, <c>"base32-crockford"</c>, <c>"z-base-32"</c>,
    /// <c>"base45"</c>, <c>"base64"</c>, <c>"base64-urlsafe"</c>, <c>"base64-mime"</c>, <c>"base58"</c>,
    /// <c>"base58-ripple"</c>, <c>"base62"</c>, <c>"ascii85"</c>, <c>"base85-git"</c>, <c>"z85"</c>.
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
            "base45" => Base45,
            "base64" => Base64,
            "base64-urlsafe" or "base64url" => Base64UrlSafe,
            "base64-mime" => Base64Mime,
            "base58" or "base58-bitcoin" or "base58-flickr" => Base58,
            "base58-ripple" => Base58Ripple,
            "base62" => Base62,
            "ascii85" or "base85" => Ascii85,
            "base85-git" or "git-base85" or "b85" => Base85Git,
            "z85" => Z85,
            _ => throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, EncodingResourceStrings.Arg_Invalid_UnknownEncodingName, name), nameof(name)),
        };
    }

    /// <summary>
    /// Adapts lower-case Base16 to the <see cref="IBinaryEncoding" /> contract.
    /// </summary>
    private sealed class Base16LowerAdapter
        : IBinaryEncoding
    {
        /// <inheritdoc />
        public string Description => "Base16 / hexadecimal, lower case (default Bodu Base16 form; compatible with Convert.ToHexStringLower).";

        /// <inheritdoc />
        public string Name => "base16-lower";

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base16.Decode(chars);

        /// <inheritdoc />
        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base16.Encode(bytes);

        /// <inheritdoc />
        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base16.GetMaxDecodedLength(charCount);

        /// <inheritdoc />
        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base16.GetEncodedLength(byteCount);

        /// <inheritdoc />
        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base16.IsValid(source);

        /// <inheritdoc />
        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base16.TryDecode(source, destination, out bytesWritten);

        /// <inheritdoc />
        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base16.TryEncode(source, destination, out charsWritten);
    }

    /// <summary>
    /// Adapts upper-case Base16 to the <see cref="IBinaryEncoding" /> contract.
    /// </summary>
    private sealed class Base16UpperAdapter
        : IBinaryEncoding
    {
        /// <inheritdoc />
        public string Description => "Base16 / hexadecimal, upper case (RFC 4648 §8 canonical case; compatible with Convert.ToHexString).";

        /// <inheritdoc />
        public string Name => "base16-upper";

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base16.Decode(chars);

        /// <inheritdoc />
        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base16.Encode(bytes, BaseFormattingOptions.UpperCase);

        /// <inheritdoc />
        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base16.GetMaxDecodedLength(charCount);

        /// <inheritdoc />
        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base16.GetEncodedLength(byteCount, BaseFormattingOptions.UpperCase);

        /// <inheritdoc />
        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base16.IsValid(source);

        /// <inheritdoc />
        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base16.TryDecode(source, destination, out bytesWritten);

        /// <inheritdoc />
        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base16.TryEncode(source, destination, out charsWritten, BaseFormattingOptions.UpperCase);
    }

    /// <summary>
    /// Adapts RFC 9285 Base45 to the <see cref="IBinaryEncoding" /> contract.
    /// </summary>
    private sealed class Base45Adapter
        : IBinaryEncoding
    {
        /// <inheritdoc />
        public string Description => "RFC 9285 Base45 (QR-code Alphanumeric-mode alphabet; no padding).";

        /// <inheritdoc />
        public string Name => "base45";

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base45.Decode(chars);

        /// <inheritdoc />
        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base45.Encode(bytes);

        /// <inheritdoc />
        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base45.GetMaxDecodedLength(charCount);

        /// <inheritdoc />
        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base45.GetMaxEncodedLength(byteCount);

        /// <inheritdoc />
        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base45.IsValid(source);

        /// <inheritdoc />
        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base45.TryDecode(source, destination, out bytesWritten);

        /// <inheritdoc />
        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base45.TryEncode(source, destination, out charsWritten);
    }

    /// <summary>
    /// Adapts a configured <see cref="Base32Variant" /> to the <see cref="IBinaryEncoding" /> contract.
    /// </summary>
    private sealed class Base32VariantAdapter
        : IBinaryEncoding
    {
        /// <summary>The Base32 variant this adapter delegates to.</summary>
        private readonly Base32Variant _variant;

        /// <summary>
        /// Initializes a new instance of the <see cref="Base32VariantAdapter" /> class.
        /// </summary>
        /// <param name="variant">The Base32 variant the adapter delegates to.</param>
        /// <param name="name">The canonical name surfaced through <see cref="Name" />.</param>
        /// <param name="description">
        /// The human-readable description surfaced through <see cref="Description" />.
        /// </param>
        public Base32VariantAdapter(Base32Variant variant, string name, string description)
        {
            _variant = variant;
            Name = name;
            Description = description;
        }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base32.Decode(chars, _variant);

        /// <inheritdoc />
        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base32.Encode(bytes, _variant);

        /// <inheritdoc />
        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base32.GetMaxDecodedLength(charCount);

        /// <inheritdoc />
        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base32.GetEncodedLength(byteCount, _variant);

        /// <inheritdoc />
        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base32.IsValid(source, _variant);

        /// <inheritdoc />
        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base32.TryDecode(source, destination, out bytesWritten, _variant);

        /// <inheritdoc />
        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base32.TryEncode(source, destination, out charsWritten, _variant);
    }

    /// <summary>
    /// Adapts GMP-style Base62 to the <see cref="IBinaryEncoding" /> contract.
    /// </summary>
    private sealed class Base62Adapter
        : IBinaryEncoding
    {
        /// <inheritdoc />
        public string Description => "GMP-style Base62 (0-9, A-Z, a-z; leading zero bytes preserved).";

        /// <inheritdoc />
        public string Name => "base62";

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base62.Decode(chars);

        /// <inheritdoc />
        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base62.Encode(bytes);

        /// <inheritdoc />
        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base62.GetMaxDecodedLength(charCount);

        /// <inheritdoc />
        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base62.GetMaxEncodedLength(byteCount);

        /// <inheritdoc />
        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base62.IsValid(source);

        /// <inheritdoc />
        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base62.TryDecode(source, destination, out bytesWritten);

        /// <inheritdoc />
        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base62.TryEncode(source, destination, out charsWritten);
    }

    /// <summary>
    /// Adapts a configured <see cref="Base58Variant" /> to the <see cref="IBinaryEncoding" /> contract.
    /// </summary>
    private sealed class Base58VariantAdapter
        : IBinaryEncoding
    {
        /// <summary>The Base58 variant this adapter delegates to.</summary>
        private readonly Base58Variant _variant;

        /// <summary>
        /// Initializes a new instance of the <see cref="Base58VariantAdapter" /> class.
        /// </summary>
        /// <param name="variant">The Base58 variant the adapter delegates to.</param>
        /// <param name="name">The canonical name surfaced through <see cref="Name" />.</param>
        /// <param name="description">
        /// The human-readable description surfaced through <see cref="Description" />.
        /// </param>
        public Base58VariantAdapter(Base58Variant variant, string name, string description)
        {
            _variant = variant;
            Name = name;
            Description = description;
        }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base58.Decode(chars, _variant);

        /// <inheritdoc />
        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base58.Encode(bytes, _variant);

        /// <inheritdoc />
        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base58.GetMaxDecodedLength(charCount);

        /// <inheritdoc />
        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base58.GetMaxEncodedLength(byteCount);

        /// <inheritdoc />
        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base58.IsValid(source, _variant);

        /// <inheritdoc />
        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base58.TryDecode(source, destination, out bytesWritten, _variant);

        /// <inheritdoc />
        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base58.TryEncode(source, destination, out charsWritten, _variant);
    }

    /// <summary>
    /// Adapts a configured <see cref="Base64Variant" /> to the <see cref="IBinaryEncoding" /> contract.
    /// </summary>
    private sealed class Base64VariantAdapter
        : IBinaryEncoding
    {
        /// <summary>The Base64 variant this adapter delegates to.</summary>
        private readonly Base64Variant _variant;

        /// <summary>
        /// Initializes a new instance of the <see cref="Base64VariantAdapter" /> class.
        /// </summary>
        /// <param name="variant">The Base64 variant the adapter delegates to.</param>
        /// <param name="name">The canonical name surfaced through <see cref="Name" />.</param>
        /// <param name="description">
        /// The human-readable description surfaced through <see cref="Description" />.
        /// </param>
        public Base64VariantAdapter(Base64Variant variant, string name, string description)
        {
            _variant = variant;
            Name = name;
            Description = description;
        }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base64.Decode(chars, _variant);

        /// <inheritdoc />
        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base64.Encode(bytes, _variant);

        /// <inheritdoc />
        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base64.GetMaxDecodedLength(charCount);

        /// <inheritdoc />
        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base64.GetEncodedLength(byteCount, _variant);

        /// <inheritdoc />
        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base64.IsValid(source, _variant);

        /// <inheritdoc />
        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base64.TryDecode(source, destination, out bytesWritten, _variant);

        /// <inheritdoc />
        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base64.TryEncode(source, destination, out charsWritten, _variant);
    }

    /// <summary>
    /// Adapts a configured <see cref="Base85Variant" /> to the <see cref="IBinaryEncoding" /> contract.
    /// </summary>
    private sealed class Base85VariantAdapter
        : IBinaryEncoding
    {
        /// <summary>The Base85 variant this adapter delegates to.</summary>
        private readonly Base85Variant _variant;

        /// <summary>
        /// Initializes a new instance of the <see cref="Base85VariantAdapter" /> class.
        /// </summary>
        /// <param name="variant">The Base85 variant the adapter delegates to.</param>
        /// <param name="name">The canonical name surfaced through <see cref="Name" />.</param>
        /// <param name="description">
        /// The human-readable description surfaced through <see cref="Description" />.
        /// </param>
        public Base85VariantAdapter(Base85Variant variant, string name, string description)
        {
            _variant = variant;
            Name = name;
            Description = description;
        }

        /// <inheritdoc />
        public string Description { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public byte[] Decode(ReadOnlySpan<char> chars) => global::Bodu.Text.Encoding.Base85.Decode(chars, _variant);

        /// <inheritdoc />
        public string Encode(ReadOnlySpan<byte> bytes) => global::Bodu.Text.Encoding.Base85.Encode(bytes, _variant);

        /// <inheritdoc />
        public int GetMaxDecodedLength(int charCount) => global::Bodu.Text.Encoding.Base85.GetMaxDecodedLength(charCount);

        /// <inheritdoc />
        public int GetMaxEncodedLength(int byteCount) => global::Bodu.Text.Encoding.Base85.GetMaxEncodedLength(byteCount, _variant);

        /// <inheritdoc />
        public bool IsValid(ReadOnlySpan<char> source) => global::Bodu.Text.Encoding.Base85.IsValid(source, _variant);

        /// <inheritdoc />
        public bool TryDecode(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten) =>
            global::Bodu.Text.Encoding.Base85.TryDecode(source, destination, out bytesWritten, _variant);

        /// <inheritdoc />
        public bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten) =>
            global::Bodu.Text.Encoding.Base85.TryEncode(source, destination, out charsWritten, _variant);
    }
}
