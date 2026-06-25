// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNamingPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Text.Yaml;

/// <summary>
/// Determines how a member name is converted to a YAML key during serialization, in the manner of
/// <see cref="System.Text.Json.JsonNamingPolicy" />.
/// </summary>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new YamlSerializerOptions { PropertyNamingPolicy = YamlNamingPolicy.SnakeCaseLower };
/// // A "ServerHost" member is written as the key "server_host".
///]]>
/// </code>
/// </example>
public abstract class YamlNamingPolicy
{
    /// <summary>
    /// Gets a naming policy that converts member names to <c>camelCase</c>.
    /// </summary>
    /// <value>The camel-case naming policy.</value>
    public static YamlNamingPolicy CamelCase { get; } = new CamelCaseNamingPolicy();

    /// <summary>
    /// Gets a naming policy that converts member names to lower <c>snake_case</c>.
    /// </summary>
    /// <value>The lower snake-case naming policy.</value>
    public static YamlNamingPolicy SnakeCaseLower { get; } = new SeparatorNamingPolicy('_', toUpper: false);

    /// <summary>
    /// Gets a naming policy that converts member names to lower <c>kebab-case</c>.
    /// </summary>
    /// <value>The lower kebab-case naming policy.</value>
    public static YamlNamingPolicy KebabCaseLower { get; } = new SeparatorNamingPolicy('-', toUpper: false);

    /// <summary>
    /// Converts the specified member name according to the policy.
    /// </summary>
    /// <param name="name">The member name to convert.</param>
    /// <returns>The converted YAML key.</returns>
    public abstract string ConvertName(string name);

    /// <summary>
    /// A naming policy that lowercases the first character to produce camel case.
    /// </summary>
    private sealed class CamelCaseNamingPolicy : YamlNamingPolicy
    {
        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
                return name;

            var chars = name.ToCharArray();
            chars[0] = char.ToLowerInvariant(chars[0]);
            return new string(chars);
        }
    }

    /// <summary>
    /// A naming policy that inserts a separator before internal capitals and adjusts case.
    /// </summary>
    private sealed class SeparatorNamingPolicy : YamlNamingPolicy
    {
        private readonly char _separator;
        private readonly bool _toUpper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SeparatorNamingPolicy" /> class.
        /// </summary>
        /// <param name="separator">The separator inserted between word boundaries.</param>
        /// <param name="toUpper">Whether the result is uppercased rather than lowercased.</param>
        public SeparatorNamingPolicy(char separator, bool toUpper)
        {
            _separator = separator;
            _toUpper = toUpper;
        }

        /// <inheritdoc />
        public override string ConvertName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            var sb = new StringBuilder(name.Length + 4);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c) && i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1])))
                    sb.Append(_separator);

                sb.Append(_toUpper ? char.ToUpperInvariant(c) : char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }
    }
}
