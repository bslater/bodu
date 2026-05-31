// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedValue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Represents any value in the Bencode serialization format. Serves as the abstract base for the four concrete value
/// kinds — <see cref="BencodedInteger" />, <see cref="BencodedString" />, <see cref="BencodedList" />, and
/// <see cref="BencodedDictionary" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Kind" /> identifies the concrete subclass without forcing a cast, so callers can switch on the kind
/// before narrowing to a specific type. The conventional consumer pattern is to call
/// <see cref="Bencode.Decode(ReadOnlySpan{byte})" />, inspect <see cref="Kind" /> (or pattern-match against one of the
/// concrete subclasses), and recurse into nested lists and dictionaries from there.
/// </para>
/// <para>
/// Instances are immutable. <see cref="BencodedString" /> exposes its raw bytes — the format is byte-oriented and does
/// not specify any text encoding, so callers that need a string must convert deliberately (typically as UTF-8 when
/// interoperating with modern peers).
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// BencodedValue root = Bencode.Decode(payload);
///
/// switch (root)
/// {
///     case BencodedInteger    i: Console.WriteLine($"int: {i.Value}");                              break;
///     case BencodedString     s: Console.WriteLine($"str: {s.GetUtf8String()}");                    break;
///     case BencodedList       l: Console.WriteLine($"list of {l.Count} items");                     break;
///     case BencodedDictionary d: Console.WriteLine($"dict with {d.Count} entries");                 break;
/// }
///]]>
/// </example>
public abstract class BencodedValue
{
    /// <summary>
    /// Gets the concrete kind of the bencoded value.
    /// </summary>
    public abstract BencodedValueKind Kind { get; }
}
