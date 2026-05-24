// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringFormattingKnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Tests.KnownAnswers;

/// <summary>
/// Provides known-answer scenarios for string casing and formatting operations.
/// </summary>
public static class StringFormattingKnownAnswers
{
    /// <summary>
    /// The default acronym list every scenario is allowed to know about unless overridden.
    /// </summary>
    private static readonly string[] DefaultAcronyms =
    [
        "API", "ASCII", "ATO", "BCL", "CPU", "CSS", "CSV", "DNS", "DTO", "EOF",
        "GUID", "HTML", "HTTP", "HTTPS", "ID", "IO", "IP", "IPv4", "IPv6", "JSON",
        "JWT", "OAuth", "PDF", "REST", "RFC", "RPC", "RSA", "SHA", "SHA256", "SQL",
        "TCP", "TLS", "UI", "URI", "URL", "UTF8", "XML", "XOF",
    ];

    /// <summary>
    /// The default minor (connective) words eligible for title-case down-casing.
    /// </summary>
    private static readonly string[] DefaultMinorWords =
    [
        "a", "an", "and", "as", "at", "but", "by", "for", "from", "in", "into",
        "nor", "of", "on", "or", "over", "per", "the", "to", "via", "with",
    ];

    /// <summary>
    /// Gets all known-answer scenarios wrapped in single-element <see cref="object" /> arrays so they can be
    /// consumed by <c>[DynamicData]</c>.
    /// </summary>
    /// <returns>The scenarios.</returns>
    public static IEnumerable<object[]> All()
    {
        foreach (StringFormattingKnownAnswer kat in Enumerate())
            yield return [kat];
    }

    /// <summary>
    /// Returns the display name used by the test runner for a given KAT row.
    /// </summary>
    /// <param name="methodInfo">The test method.</param>
    /// <param name="data">The dynamic data row.</param>
    /// <returns>The KAT display name when the row carries one; otherwise the method name.</returns>
    public static string GetDisplayName(MethodInfo methodInfo, object[] data) =>
        data[0] is StringFormattingKnownAnswer kat ? kat.DisplayName : methodInfo.Name;

    /// <summary>
    /// Enumerates every known-answer scenario, grouped by operation family.
    /// </summary>
    /// <returns>The scenarios.</returns>
    public static IEnumerable<StringFormattingKnownAnswer> Enumerate()
    {
        foreach (StringFormattingKnownAnswer kat in TitleCase()) yield return kat;
        foreach (StringFormattingKnownAnswer kat in SentenceCase()) yield return kat;
        foreach (StringFormattingKnownAnswer kat in IdentifierCase()) yield return kat;
        foreach (StringFormattingKnownAnswer kat in Slug()) yield return kat;
        foreach (StringFormattingKnownAnswer kat in Whitespace()) yield return kat;
    }

    private static IEnumerable<StringFormattingKnownAnswer> TitleCase()
    {
        yield return Kat("TITLE-0001", StringFormattingOperation.TitleCase, "Simple lowercase words", "hello world", "Hello World");
        yield return Kat("TITLE-0002", StringFormattingOperation.TitleCase, "Already title-cased words", "Hello World", "Hello World");
        yield return Kat("TITLE-0003", StringFormattingOperation.TitleCase, "Minor words are lowercase when not first or last", "the lord of the rings", "The Lord of the Rings");
        yield return Kat("TITLE-0004", StringFormattingOperation.TitleCase, "Minor word at the end is capitalized", "things to come", "Things to Come");
        yield return Kat("TITLE-0005", StringFormattingOperation.TitleCase, "Acronyms are canonicalized", "http api for json data", "HTTP API for JSON Data");
        yield return Kat("TITLE-0006", StringFormattingOperation.TitleCase, "Existing uppercase acronyms are preserved", "parse HTML and XML documents", "Parse HTML and XML Documents");
        yield return Kat("TITLE-0007", StringFormattingOperation.TitleCase, "Known mixed-case acronym is preserved", "oauth token exchange", "OAuth Token Exchange");
        yield return Kat("TITLE-0008", StringFormattingOperation.TitleCase, "Existing mixed-case brand word is preserved", "iPhone support guide", "iPhone Support Guide");
        yield return Kat("TITLE-0009", StringFormattingOperation.TitleCase, "Existing eBay-style mixed-case word is preserved", "eBay seller account", "eBay Seller Account");
        yield return Kat("TITLE-0010", StringFormattingOperation.TitleCase, "Apostrophe in personal name", "o'connor and sons", "O'Connor and Sons", notes: "Validates capitalization after apostrophe.");
        yield return Kat("TITLE-0011", StringFormattingOperation.TitleCase, "Hyphenated compound words", "cost-effective api design", "Cost-Effective API Design");
        yield return Kat("TITLE-0012", StringFormattingOperation.TitleCase, "Underscore separated identifier", "customer_id_display_name", "Customer ID Display Name");
        yield return Kat("TITLE-0013", StringFormattingOperation.TitleCase, "Kebab separated identifier", "customer-id-display-name", "Customer ID Display Name");
        yield return Kat("TITLE-0014", StringFormattingOperation.TitleCase, "Dot separated configuration key", "database.connection.timeout", "Database Connection Timeout");
        yield return Kat("TITLE-0015", StringFormattingOperation.TitleCase, "PascalCase input", "CustomerDisplayName", "Customer Display Name");
        yield return Kat("TITLE-0016", StringFormattingOperation.TitleCase, "camelCase input", "customerDisplayName", "Customer Display Name");
        yield return Kat("TITLE-0017", StringFormattingOperation.TitleCase, "Acronym followed by PascalCase word", "HTTPResponseCode", "HTTP Response Code");
        yield return Kat("TITLE-0018", StringFormattingOperation.TitleCase, "Adjacent known acronyms", "JSONRPCAPI", "JSON RPC API", notes: "Requires acronym-aware tokenization.");
        yield return Kat("TITLE-0019", StringFormattingOperation.TitleCase, "Digit-containing acronym", "ipv6 address parser", "IPv6 Address Parser");
        yield return Kat("TITLE-0020", StringFormattingOperation.TitleCase, "Digit suffix acronym", "sha256 hash value", "SHA256 Hash Value");
        yield return Kat("TITLE-0021", StringFormattingOperation.TitleCase, "Digit prefix acronym", "3des encryption mode", "3DES Encryption Mode", acronyms: [.. DefaultAcronyms, "3DES"]);
        yield return Kat("TITLE-0022", StringFormattingOperation.TitleCase, "Sentence punctuation", "hello, world! this is an api.", "Hello, World! This Is an API.");
        yield return Kat("TITLE-0023", StringFormattingOperation.TitleCase, "Repeated separators are ignored as word boundaries", "customer___display---name...value", "Customer Display Name Value");
        yield return Kat("TITLE-0024", StringFormattingOperation.TitleCase, "Path-like input", "/api/v1/customer-details/", "API V1 Customer Details");
        yield return Kat("TITLE-0025", StringFormattingOperation.TitleCase, "Empty string", "", "");
        yield return Kat("TITLE-0026", StringFormattingOperation.TitleCase, "Whitespace-only string", "   ", "");
        yield return Kat("TITLE-0027", StringFormattingOperation.TitleCase, "Single word", "customer", "Customer");
        yield return Kat("TITLE-0028", StringFormattingOperation.TitleCase, "Single acronym", "api", "API");
        yield return Kat("TITLE-0029", StringFormattingOperation.TitleCase, "Culture-sensitive Turkish title case", "istanbul izmir id", "İstanbul İzmir ID", cultureName: "tr-TR", notes: "Title case should use the supplied culture.");
        yield return Kat("TITLE-0030", StringFormattingOperation.TitleCase, "Diacritics preserved in title case", "crème brûlée recipe", "Crème Brûlée Recipe");
    }

    private static IEnumerable<StringFormattingKnownAnswer> SentenceCase()
    {
        yield return Kat("SENTENCE-0001", StringFormattingOperation.SentenceCase, "Simple lowercase sentence", "hello world", "Hello world");
        yield return Kat("SENTENCE-0002", StringFormattingOperation.SentenceCase, "Title-cased input is normalized", "Hello World", "Hello world");
        yield return Kat("SENTENCE-0003", StringFormattingOperation.SentenceCase, "Acronyms are preserved", "http api for json data", "HTTP API for JSON data");
        yield return Kat("SENTENCE-0004", StringFormattingOperation.SentenceCase, "Existing mixed-case word is preserved", "OAuth token exchange", "OAuth token exchange");
        yield return Kat("SENTENCE-0005", StringFormattingOperation.SentenceCase, "Capitalize after full stop", "first sentence. second sentence.", "First sentence. Second sentence.");
        yield return Kat("SENTENCE-0006", StringFormattingOperation.SentenceCase, "Capitalize after exclamation mark", "first sentence! second sentence!", "First sentence! Second sentence!");
        yield return Kat("SENTENCE-0007", StringFormattingOperation.SentenceCase, "Capitalize after question mark", "first sentence? second sentence?", "First sentence? Second sentence?");
        yield return Kat("SENTENCE-0008", StringFormattingOperation.SentenceCase, "Mixed sentence terminators", "first sentence. second sentence! third sentence?", "First sentence. Second sentence! Third sentence?");
        yield return Kat("SENTENCE-0009", StringFormattingOperation.SentenceCase, "Apostrophe name", "o'connor and sons", "O'connor and sons");
        yield return Kat("SENTENCE-0010", StringFormattingOperation.SentenceCase, "Hyphenated compound word", "cost-effective api design", "Cost-effective API design");
        yield return Kat("SENTENCE-0011", StringFormattingOperation.SentenceCase, "PascalCase input is tokenized", "CustomerDisplayName", "Customer display name");
        yield return Kat("SENTENCE-0012", StringFormattingOperation.SentenceCase, "Acronym followed by PascalCase word", "HTTPResponseCode", "HTTP response code");
        yield return Kat("SENTENCE-0013", StringFormattingOperation.SentenceCase, "Digit-containing acronym", "ipv6 address parser", "IPv6 address parser");
        yield return Kat("SENTENCE-0014", StringFormattingOperation.SentenceCase, "Digit suffix acronym", "sha256 hash value", "SHA256 hash value");
        yield return Kat("SENTENCE-0015", StringFormattingOperation.SentenceCase, "Turkish casing", "istanbul izmir id", "İstanbul izmir ID", cultureName: "tr-TR");
        yield return Kat("SENTENCE-0016", StringFormattingOperation.SentenceCase, "Empty string", "", "");
        yield return Kat("SENTENCE-0017", StringFormattingOperation.SentenceCase, "Whitespace-only string", "   ", "");
    }

    private static IEnumerable<StringFormattingKnownAnswer> IdentifierCase()
    {
        yield return Kat("IDENT-0001", StringFormattingOperation.CamelCase, "Simple words", "hello world", "helloWorld");
        yield return Kat("IDENT-0002", StringFormattingOperation.PascalCase, "Simple words", "hello world", "HelloWorld");
        yield return Kat("IDENT-0003", StringFormattingOperation.SnakeCase, "Simple words", "hello world", "hello_world");
        yield return Kat("IDENT-0004", StringFormattingOperation.KebabCase, "Simple words", "hello world", "hello-world");
        yield return Kat("IDENT-0005", StringFormattingOperation.TrainCase, "Simple words", "hello world", "Hello-World");
        yield return Kat("IDENT-0006", StringFormattingOperation.ConstantCase, "Simple words", "hello world", "HELLO_WORLD");
        yield return Kat("IDENT-0007", StringFormattingOperation.DotCase, "Simple words", "hello world", "hello.world");
        yield return Kat("IDENT-0008", StringFormattingOperation.CamelCase, "PascalCase input", "CustomerDisplayName", "customerDisplayName");
        yield return Kat("IDENT-0009", StringFormattingOperation.SnakeCase, "PascalCase input", "CustomerDisplayName", "customer_display_name");
        yield return Kat("IDENT-0010", StringFormattingOperation.KebabCase, "PascalCase input", "CustomerDisplayName", "customer-display-name");
        yield return Kat("IDENT-0011", StringFormattingOperation.ConstantCase, "PascalCase input", "CustomerDisplayName", "CUSTOMER_DISPLAY_NAME");
        yield return Kat("IDENT-0012", StringFormattingOperation.DotCase, "PascalCase input", "CustomerDisplayName", "customer.display.name");
        yield return Kat("IDENT-0013", StringFormattingOperation.CamelCase, "camelCase input", "customerDisplayName", "customerDisplayName");
        yield return Kat("IDENT-0014", StringFormattingOperation.PascalCase, "camelCase input", "customerDisplayName", "CustomerDisplayName");
        yield return Kat("IDENT-0015", StringFormattingOperation.SnakeCase, "camelCase input", "customerDisplayName", "customer_display_name");
        yield return Kat("IDENT-0016", StringFormattingOperation.SnakeCase, "Acronym followed by word", "HTTPResponseCode", "http_response_code");
        yield return Kat("IDENT-0017", StringFormattingOperation.KebabCase, "Acronym followed by word", "HTTPResponseCode", "http-response-code");
        yield return Kat("IDENT-0018", StringFormattingOperation.ConstantCase, "Acronym followed by word", "HTTPResponseCode", "HTTP_RESPONSE_CODE");
        yield return Kat("IDENT-0019", StringFormattingOperation.CamelCase, "Acronym followed by word", "HTTPResponseCode", "httpResponseCode");
        yield return Kat("IDENT-0020", StringFormattingOperation.PascalCase, "Acronym followed by word", "HTTPResponseCode", "HttpResponseCode");
        yield return Kat("IDENT-0021", StringFormattingOperation.SnakeCase, "Known adjacent acronyms", "JSONRPCAPI", "json_rpc_api");
        yield return Kat("IDENT-0022", StringFormattingOperation.KebabCase, "Known adjacent acronyms", "JSONRPCAPI", "json-rpc-api");
        yield return Kat("IDENT-0023", StringFormattingOperation.ConstantCase, "Known adjacent acronyms", "JSONRPCAPI", "JSON_RPC_API");
        yield return Kat("IDENT-0024", StringFormattingOperation.CamelCase, "Known adjacent acronyms", "JSONRPCAPI", "jsonRpcApi");
        yield return Kat("IDENT-0025", StringFormattingOperation.PascalCase, "Known adjacent acronyms", "JSONRPCAPI", "JsonRpcApi");
        yield return Kat("IDENT-0026", StringFormattingOperation.SnakeCase, "Underscore separated input", "customer_id_display_name", "customer_id_display_name");
        yield return Kat("IDENT-0027", StringFormattingOperation.KebabCase, "Underscore separated input", "customer_id_display_name", "customer-id-display-name");
        yield return Kat("IDENT-0028", StringFormattingOperation.PascalCase, "Underscore separated input", "customer_id_display_name", "CustomerIdDisplayName");
        yield return Kat("IDENT-0029", StringFormattingOperation.CamelCase, "Underscore separated input", "customer_id_display_name", "customerIdDisplayName");
        yield return Kat("IDENT-0030", StringFormattingOperation.SnakeCase, "Repeated separators collapse", "customer___display---name...value", "customer_display_name_value");
        yield return Kat("IDENT-0031", StringFormattingOperation.KebabCase, "Repeated separators collapse", "customer___display---name...value", "customer-display-name-value");
        yield return Kat("IDENT-0032", StringFormattingOperation.DotCase, "Repeated separators collapse", "customer___display---name...value", "customer.display.name.value");
        yield return Kat("IDENT-0033", StringFormattingOperation.CamelCase, "Mixed punctuation removed", "Hello, World! 100% ready?", "helloWorld100Ready");
        yield return Kat("IDENT-0034", StringFormattingOperation.PascalCase, "Mixed punctuation removed", "Hello, World! 100% ready?", "HelloWorld100Ready");
        yield return Kat("IDENT-0035", StringFormattingOperation.SnakeCase, "Mixed punctuation removed", "Hello, World! 100% ready?", "hello_world_100_ready");
        yield return Kat("IDENT-0036", StringFormattingOperation.SnakeCase, "Digit-containing acronym", "IPv6 address parser", "ipv6_address_parser");
        yield return Kat("IDENT-0037", StringFormattingOperation.KebabCase, "Digit-containing acronym", "IPv6 address parser", "ipv6-address-parser");
        yield return Kat("IDENT-0038", StringFormattingOperation.ConstantCase, "Digit-containing acronym", "IPv6 address parser", "IPV6_ADDRESS_PARSER");
        yield return Kat("IDENT-0039", StringFormattingOperation.SnakeCase, "Mixed-case token", "OAuth token exchange", "oauth_token_exchange");
        yield return Kat("IDENT-0040", StringFormattingOperation.PascalCase, "Mixed-case token", "OAuth token exchange", "OAuthTokenExchange");
        yield return Kat("IDENT-0041", StringFormattingOperation.CamelCase, "Mixed-case token", "OAuth token exchange", "oauthTokenExchange");
        yield return Kat("IDENT-0042", StringFormattingOperation.CamelCase, "Empty string", "", "");
        yield return Kat("IDENT-0043", StringFormattingOperation.PascalCase, "Empty string", "", "");
        yield return Kat("IDENT-0044", StringFormattingOperation.SnakeCase, "Empty string", "", "");
        yield return Kat("IDENT-0045", StringFormattingOperation.KebabCase, "Whitespace-only string", "   ", "");
    }

    private static IEnumerable<StringFormattingKnownAnswer> Slug()
    {
        yield return Kat("SLUG-0001", StringFormattingOperation.Slug, "Simple words", "hello world", "hello-world");
        yield return Kat("SLUG-0002", StringFormattingOperation.Slug, "Title-cased words", "Hello World", "hello-world");
        yield return Kat("SLUG-0003", StringFormattingOperation.Slug, "Mixed punctuation removed", "Hello, World! 100% ready?", "hello-world-100-ready");
        yield return Kat("SLUG-0004", StringFormattingOperation.Slug, "Repeated separators collapse", "customer___display---name...value", "customer-display-name-value");
        yield return Kat("SLUG-0005", StringFormattingOperation.Slug, "Leading and trailing separators removed", "__customer--display.name__", "customer-display-name");
        yield return Kat("SLUG-0006", StringFormattingOperation.Slug, "Path-like input", "/api/v1/customer-details/", "api-v1-customer-details");
        yield return Kat("SLUG-0007", StringFormattingOperation.Slug, "Diacritics removed", "Crème brûlée recipe", "creme-brulee-recipe", normalizeDiacritics: true);
        yield return Kat("SLUG-0008", StringFormattingOperation.Slug, "German sharp s transliteration", "straße name", "strasse-name", cultureName: "de-DE", normalizeDiacritics: true, notes: "Expected behaviour may require explicit transliteration, not only FormD diacritic stripping.");
        yield return Kat("SLUG-0009", StringFormattingOperation.Slug, "Acronym input", "HTTP API for JSON data", "http-api-for-json-data");
        yield return Kat("SLUG-0010", StringFormattingOperation.Slug, "Empty string", "", "");
        yield return Kat("SLUG-0011", StringFormattingOperation.Slug, "Whitespace-only string", "   ", "");
    }

    private static IEnumerable<StringFormattingKnownAnswer> Whitespace()
    {
        yield return Kat("SPACE-0001", StringFormattingOperation.NormalizeWhitespace, "Already normalized whitespace", "hello world", "hello world");
        yield return Kat("SPACE-0002", StringFormattingOperation.NormalizeWhitespace, "Leading and trailing whitespace removed", "  hello world  ", "hello world");
        yield return Kat("SPACE-0003", StringFormattingOperation.NormalizeWhitespace, "Tabs collapse to single spaces", "hello\t\tworld", "hello world");
        yield return Kat("SPACE-0004", StringFormattingOperation.NormalizeWhitespace, "New lines collapse to single spaces", "hello\r\nworld\nfrom\rbodu", "hello world from bodu");
        yield return Kat("SPACE-0005", StringFormattingOperation.NormalizeWhitespace, "Mixed whitespace collapses", "  hello\t\tworld \r\n from   bodu  ", "hello world from bodu");
        yield return Kat("SPACE-0006", StringFormattingOperation.NormalizeWhitespace, "Whitespace-only string becomes empty", "   \t\r\n   ", "");
        yield return Kat("SPACE-0007", StringFormattingOperation.NormalizeWhitespace, "Empty string remains empty", "", "");
        yield return Kat("SPACE-0008", StringFormattingOperation.NormalizeWhitespace, "Non-breaking space is normalized", "hello world", "hello world");
    }

    private static StringFormattingKnownAnswer Kat(
        string id,
        StringFormattingOperation operation,
        string scenario,
        string input,
        string expected,
        string? cultureName = null,
        IReadOnlyCollection<string>? acronyms = null,
        IReadOnlyCollection<string>? minorWords = null,
        bool preserveExistingAcronyms = true,
        bool preserveMixedCaseWords = true,
        bool preserveWordsContainingDigits = true,
        bool normalizeDiacritics = false,
        string? notes = null) =>
        new(id, operation, scenario, input, expected,
            cultureName,
            acronyms ?? DefaultAcronyms,
            minorWords ?? DefaultMinorWords,
            preserveExistingAcronyms,
            preserveMixedCaseWords,
            preserveWordsContainingDigits,
            normalizeDiacritics,
            notes);
}
