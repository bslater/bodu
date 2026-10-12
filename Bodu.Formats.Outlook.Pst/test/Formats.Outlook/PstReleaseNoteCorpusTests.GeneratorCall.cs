// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.GeneratorCall.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one call of a <c>gen:</c> pipeline: a generator or transform name and the arguments it was given.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pipeline is calls separated by <c>|</c>, each <c>Name(key=value,...)</c>. A value runs to the next <c>,</c> or
    /// <c>)</c>, or is double-quoted with <c>""</c> standing for a quote. Integers are decimal digits or <c>0x</c> and
    /// hexadecimal digits, and byte strings are hexadecimal digits with an optional <c>0x</c>.
    /// </para>
    /// <para>
    /// A generator reads each key it knows with <see cref="Get" />, which allows it at most once, or with
    /// <see cref="GetAll" /> for the keys the catalogue README marks repeatable; <see cref="EnsureAllRead" /> then
    /// rejects any key it did not read, so a misspelt key fails its row instead of being ignored.
    /// </para>
    /// </remarks>
    private sealed class GeneratorCall
    {
        /// <summary>The arguments, in the order the call gives them.</summary>
        private readonly List<KeyValuePair<string, string>> _arguments;

        /// <summary>The keys the generator has read.</summary>
        private readonly HashSet<string> _read = new(StringComparer.Ordinal);

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratorCall" /> class.
        /// </summary>
        /// <param name="name">The generator or transform name.</param>
        /// <param name="arguments">The arguments, in the order the call gives them.</param>
        public GeneratorCall(string name, List<KeyValuePair<string, string>> arguments)
        {
            Name = name;
            _arguments = arguments;
        }

        /// <summary>
        /// Gets the generator or transform name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Parses the text after <c>gen:</c> into its calls.
        /// </summary>
        /// <param name="text">The pipeline.</param>
        /// <returns>The calls, in order: the generator first, then the transforms.</returns>
        /// <exception cref="FormatException">The text is not a pipeline of calls.</exception>
        public static IReadOnlyList<GeneratorCall> ParsePipeline(string text)
        {
            var calls = new List<GeneratorCall>();
            int i = 0;
            while (true)
            {
                int open = text.IndexOf('(', i);
                string name = open < 0 ? string.Empty : text[i..open];
                if (!IsWord(name)) throw new FormatException($"gen: expected a call name followed by '(' at position {i}.");

                i = open + 1;
                var arguments = new List<KeyValuePair<string, string>>();
                while (i < text.Length && text[i] != ')')
                {
                    int equals = text.IndexOf('=', i);
                    string key = equals < 0 ? string.Empty : text[i..equals];
                    if (!IsWord(key)) throw new FormatException($"gen: {name}: expected key=value at position {i}.");

                    i = equals + 1;
                    arguments.Add(new KeyValuePair<string, string>(key, ReadValue(text, ref i, name, key)));
                    if (i < text.Length && text[i] == ',')
                        i++;
                    else if (i >= text.Length || text[i] != ')')
                        throw new FormatException($"gen: {name}: expected ',' or ')' after the value of '{key}'.");
                }

                if (i >= text.Length) throw new FormatException($"gen: {name}: the call has no closing ')'.");

                i++;
                calls.Add(new GeneratorCall(name, arguments));
                if (i == text.Length)
                    return calls;

                if (text[i] != '|') throw new FormatException($"gen: expected '|' between calls at position {i}.");

                i++;
            }
        }

        /// <summary>
        /// Reads an integer written in decimal digits, or <c>0x</c> and hexadecimal digits.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The text is not an integer in either form.</exception>
        public static long ParseInteger(string text)
        {
            if (text.StartsWith("0x", StringComparison.Ordinal))
            {
                if (ulong.TryParse(text.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong hex))
                    return (long)hex;
            }
            else if (long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out long value))
            {
                return value;
            }

            throw new FormatException($"gen: '{text}' is not a decimal or 0x hexadecimal integer.");
        }

        /// <summary>
        /// Reads a byte string written as hexadecimal digits, with an optional <c>0x</c>.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The bytes; none for empty text.</returns>
        /// <exception cref="FormatException">The text is not an even number of hexadecimal digits.</exception>
        public static byte[] ParseHex(string text)
        {
            string digits = text.StartsWith("0x", StringComparison.Ordinal) ? text[2..] : text;

            return Convert.FromHexString(digits);
        }

        /// <summary>
        /// Reads a key that may be given at most once.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The value, or <see langword="null" /> when the call does not give the key.</returns>
        /// <exception cref="FormatException">The call gives the key more than once.</exception>
        public string? Get(string key)
        {
            IReadOnlyList<string> values = GetAll(key);
            if (values.Count > 1) throw new FormatException($"gen: {Name}: the key '{key}' is given more than once.");

            return values.Count == 0 ? null : values[0];
        }

        /// <summary>
        /// Reads a key that may be given any number of times.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The values, in the order the call gives them.</returns>
        public IReadOnlyList<string> GetAll(string key)
        {
            _read.Add(key);

            return [.. _arguments.Where(argument => argument.Key == key).Select(argument => argument.Value)];
        }

        /// <summary>
        /// Reads a text key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="fallback">The value when the call does not give the key.</param>
        /// <returns>The value.</returns>
        public string GetString(string key, string fallback) =>
            Get(key) ?? fallback;

        /// <summary>
        /// Reads an integer key that fits <see cref="int" />.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="fallback">The value when the call does not give the key.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is not an integer.</exception>
        public int GetInt32(string key, int fallback) =>
            Get(key) is string text ? (int)ParseInteger(text) : fallback;

        /// <summary>
        /// Reads an integer key that fits <see cref="uint" />.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="fallback">The value when the call does not give the key.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is not an integer.</exception>
        public uint GetUInt32(string key, uint fallback) =>
            Get(key) is string text ? (uint)ParseInteger(text) : fallback;

        /// <summary>
        /// Reads a Boolean key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="fallback">The value when the call does not give the key.</param>
        /// <returns>The value.</returns>
        /// <exception cref="FormatException">The value is neither <c>true</c> nor <c>false</c>.</exception>
        public bool GetBoolean(string key, bool fallback) =>
            Get(key) switch
            {
                null => fallback,
                "true" => true,
                "false" => false,
                string other => throw new FormatException($"gen: {Name}: the key '{key}' takes true or false, not '{other}'."),
            };

        /// <summary>
        /// Checks that the generator read every key the call gives.
        /// </summary>
        /// <exception cref="FormatException">The call gives a key the generator does not know.</exception>
        public void EnsureAllRead()
        {
            foreach (KeyValuePair<string, string> argument in _arguments)
            {
                if (!_read.Contains(argument.Key))
                    throw new FormatException($"gen: {Name}: unknown key '{argument.Key}'.");
            }
        }

        /// <summary>
        /// Determines whether text is a call name or key: one or more ASCII letters and digits.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns><see langword="true" /> when the text is a word.</returns>
        private static bool IsWord(string text) =>
            text.Length > 0 && text.All(char.IsAsciiLetterOrDigit);

        /// <summary>
        /// Reads one argument value, quoted or not, advancing past it.
        /// </summary>
        /// <param name="text">The pipeline.</param>
        /// <param name="i">
        /// The position of the value's first character; on return, the position after the value.
        /// </param>
        /// <param name="name">The call name, for error messages.</param>
        /// <param name="key">The key, for error messages.</param>
        /// <returns>The value, with a quoted value's quotes removed and its doubled quotes undoubled.</returns>
        /// <exception cref="FormatException">A quoted value has no closing quote.</exception>
        private static string ReadValue(string text, ref int i, string name, string key)
        {
            if (i >= text.Length || text[i] != '"')
            {
                int start = i;
                while (i < text.Length && text[i] is not (',' or ')' or '"'))
                    i++;

                return text[start..i];
            }

            var builder = new StringBuilder();
            i++;
            while (true)
            {
                if (i >= text.Length) throw new FormatException($"gen: {name}: the quoted value of '{key}' has no closing quote.");

                if (text[i] != '"')
                {
                    builder.Append(text[i++]);
                }
                else if (i + 1 < text.Length && text[i + 1] == '"')
                {
                    builder.Append('"');
                    i += 2;
                }
                else
                {
                    i++;
                    return builder.ToString();
                }
            }
        }
    }
}
