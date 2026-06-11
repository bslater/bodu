// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentBuilder.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// The authoritative TOML structural parser: consumes the source-order token stream of a <see cref="TomlLexer" /> and
/// materializes the document into a <see cref="TomlTableNode" /> value tree, enforcing the specification's key, value,
/// table, and array-of-tables rules for the configured <see cref="TomlSpecVersion" />.
/// </summary>
/// <remarks>
/// <para>
/// The lexer owns lexical validation; this type owns everything structural: dotted-key semantics, table reopening,
/// implicit-versus-explicit table tracking, arrays of tables, duplicate definitions, inline-table closedness, and the
/// maximum nesting depth. The split means a structurally invalid document lexes cleanly and is rejected here, with the
/// error position taken from the offending token.
/// </para>
/// <para>
/// TOML cannot be tokenized into tree order in a single forward pass, because out-of-line <c>[table]</c> and
/// <c>[[array-of-tables]]</c> headers contribute to structure defined elsewhere in the document. The builder therefore
/// materializes the whole document into the tree, and <see cref="Utf8TomlReader" /> walks it depth-first to emit a
/// normalized token stream.
/// </para>
/// </remarks>
internal sealed class TomlDocumentBuilder
{
    /// <summary>
    /// The root table of the document.
    /// </summary>
    private readonly TomlTableNode _root = new(0);

    /// <summary>
    /// Tables explicitly defined by a <c>[table]</c> header.
    /// </summary>
    private readonly HashSet<TomlTableNode> _headerDefined = [];

    /// <summary>
    /// Tables created implicitly as intermediates of a dotted key.
    /// </summary>
    private readonly HashSet<TomlTableNode> _dotted = [];

    /// <summary>
    /// Inline tables, which are closed to further extension once defined.
    /// </summary>
    private readonly HashSet<TomlTableNode> _inline = [];

    /// <summary>
    /// Super-tables created implicitly by a table-header path, which a later header may promote.
    /// </summary>
    private readonly HashSet<TomlTableNode> _implicitSuper = [];

    /// <summary>
    /// Arrays created by an array-of-tables header, to which further elements may be appended.
    /// </summary>
    private readonly HashSet<TomlArrayNode> _tableArrays = [];

    /// <summary>
    /// The TOML specification version whose grammar features the lexer enforces.
    /// </summary>
    private readonly TomlSpecVersion _specVersion;

    /// <summary>
    /// The maximum nesting depth of tables and arrays the builder will accept.
    /// </summary>
    private readonly int _maxDepth;

    /// <summary>
    /// The current nesting depth of arrays and inline tables, used to enforce <see cref="_maxDepth" />.
    /// </summary>
    private int _depth;

    /// <summary>
    /// The table that bare key/value pairs are currently assigned to.
    /// </summary>
    private TomlTableNode _current;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlDocumentBuilder" /> class.
    /// </summary>
    /// <param name="specVersion">The specification version whose grammar to enforce.</param>
    /// <param name="maxDepth">The maximum nesting depth of tables and arrays to accept.</param>
    internal TomlDocumentBuilder(TomlSpecVersion specVersion, int maxDepth)
    {
        _specVersion = specVersion;
        _maxDepth = maxDepth;
        _current = _root;
    }

    /// <summary>
    /// Parses the supplied UTF-8 TOML source and returns the document's root table.
    /// </summary>
    /// <param name="source">The UTF-8 TOML source bytes.</param>
    /// <returns>The root <see cref="TomlTableNode" />, carrying byte offsets from the lexer.</returns>
    /// <exception cref="TomlFormatException">Thrown when the source is not valid TOML.</exception>
    internal TomlTableNode Parse(ReadOnlySpan<byte> source)
    {
        var lexer = new TomlLexer(source, _specVersion);
        while (lexer.Read())
        {
            switch (lexer.TokenType)
            {
                case TomlLexTokenType.Comment:
                    break;

                case TomlLexTokenType.TableHeader:
                    DefineStandardTable(ReadHeaderPath(ref lexer), ref lexer);
                    break;

                case TomlLexTokenType.ArrayTableHeader:
                    DefineArrayTable(ReadHeaderPath(ref lexer), ref lexer);
                    break;

                case TomlLexTokenType.Key:
                default:
                {
                    List<string> path = ReadKeyPath(ref lexer);
                    TomlReaderNode value = ReadValue(ref lexer);
                    AssignKeyValue(_current, path, value, ref lexer);
                    break;
                }
            }
        }

        return _root;
    }

    /// <summary>
    /// Reads the key segments of a header whose <see cref="TomlLexTokenType.TableHeader" /> or
    /// <see cref="TomlLexTokenType.ArrayTableHeader" /> token is current.
    /// </summary>
    /// <param name="lexer">The lexer to read from.</param>
    /// <returns>The key segments in order.</returns>
    private static List<string> ReadHeaderPath(ref TomlLexer lexer)
    {
        _ = lexer.Read();
        return ReadKeyPath(ref lexer);
    }

    /// <summary>
    /// Reads the remaining segments of a dotted key path whose first <see cref="TomlLexTokenType.Key" /> token is
    /// current.
    /// </summary>
    /// <param name="lexer">The lexer to read from.</param>
    /// <returns>The key segments in order.</returns>
    private static List<string> ReadKeyPath(ref TomlLexer lexer)
    {
        var keys = new List<string> { lexer.GetString() };
        while (!lexer.IsFinalKeySegment)
        {
            _ = lexer.Read();
            keys.Add(lexer.GetString());
        }

        return keys;
    }

    /// <summary>
    /// Reads the next value from the lexer, materializing arrays and inline tables recursively.
    /// </summary>
    /// <param name="lexer">The lexer to read from, positioned before the value's first token.</param>
    /// <returns>The materialized value node.</returns>
    private TomlReaderNode ReadValue(ref TomlLexer lexer)
    {
        _ = lexer.Read();
        return ReadValueAtCurrent(ref lexer);
    }

    /// <summary>
    /// Materializes the value whose first token is current: a scalar, an array, or an inline table.
    /// </summary>
    /// <param name="lexer">The lexer positioned on the value's first token.</param>
    /// <returns>The materialized value node.</returns>
    private TomlReaderNode ReadValueAtCurrent(ref TomlLexer lexer)
    {
        switch (lexer.TokenType)
        {
            case TomlLexTokenType.StartArray:
            {
                EnterDepth(ref lexer);
                var array = new TomlArrayNode(lexer.TokenStartIndex);
                while (true)
                {
                    _ = lexer.Read();
                    if (lexer.TokenType == TomlLexTokenType.Comment)
                        continue;
                    if (lexer.TokenType == TomlLexTokenType.EndArray)
                        break;

                    array.Add(ReadValueAtCurrent(ref lexer));
                }

                LeaveDepth();
                return array;
            }

            case TomlLexTokenType.StartInlineTable:
            {
                EnterDepth(ref lexer);
                var table = new TomlTableNode(lexer.TokenStartIndex);
                while (true)
                {
                    _ = lexer.Read();
                    if (lexer.TokenType == TomlLexTokenType.Comment)
                        continue;
                    if (lexer.TokenType == TomlLexTokenType.EndInlineTable)
                        break;

                    List<string> path = ReadKeyPath(ref lexer);
                    TomlReaderNode value = ReadValue(ref lexer);
                    AssignKeyValue(table, path, value, ref lexer);
                }

                _inline.Add(table);
                LeaveDepth();
                return table;
            }

            case TomlLexTokenType.String:
                return new TomlScalarNode(TomlTokenType.String, lexer.GetString(), lexer.TokenStartIndex);

            case TomlLexTokenType.Integer:
                return new TomlScalarNode(TomlTokenType.Integer, lexer.GetInt64(), lexer.TokenStartIndex);

            case TomlLexTokenType.Float:
                return new TomlScalarNode(TomlTokenType.Float, lexer.GetDouble(), lexer.TokenStartIndex);

            case TomlLexTokenType.Boolean:
                return new TomlScalarNode(TomlTokenType.Boolean, lexer.GetBoolean(), lexer.TokenStartIndex);

            case TomlLexTokenType.OffsetDateTime:
                return new TomlScalarNode(TomlTokenType.OffsetDateTime, lexer.GetDateTimeOffset(), lexer.TokenStartIndex);

            case TomlLexTokenType.LocalDateTime:
                return new TomlScalarNode(TomlTokenType.LocalDateTime, lexer.GetDateTime(), lexer.TokenStartIndex);

            case TomlLexTokenType.LocalDate:
                return new TomlScalarNode(TomlTokenType.LocalDate, lexer.GetDateOnly(), lexer.TokenStartIndex);

            case TomlLexTokenType.LocalTime:
            default:
                return new TomlScalarNode(TomlTokenType.LocalTime, lexer.GetTimeOnly(), lexer.TokenStartIndex);
        }
    }

    /// <summary>
    /// Records entry into a nested array or inline table and enforces the maximum nesting depth.
    /// </summary>
    /// <param name="lexer">The lexer whose current token supplies the error position.</param>
    /// <exception cref="TomlFormatException">Thrown when the nesting depth exceeds the configured maximum.</exception>
    private void EnterDepth(ref TomlLexer lexer)
    {
        _depth++;
        if (_depth > _maxDepth)
            throw lexer.TokenError(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Format_Invalid_TomlNestingTooDeep, _maxDepth));
    }

    /// <summary>
    /// Records exit from a nested array or inline table.
    /// </summary>
    private void LeaveDepth() => _depth--;

    /// <summary>
    /// Defines a standard table at <paramref name="path" /> and makes it the current table.
    /// </summary>
    /// <param name="path">The table key path.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    private void DefineStandardTable(List<string> path, ref TomlLexer lexer)
    {
        TomlTableNode table = _root;
        for (var i = 0; i < path.Count - 1; i++)
            table = WalkHeaderSegment(table, path[i], ref lexer);

        var key = path[^1];
        if (table.TryGetValue(key, out TomlReaderNode? existing))
        {
            if (existing is not TomlTableNode child)
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateTable);
            if (_inline.Contains(child))
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlExtendInlineTable);
            if (_headerDefined.Contains(child) || _dotted.Contains(child))
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateTable);

            _implicitSuper.Remove(child);
            _headerDefined.Add(child);
            _current = child;
            return;
        }

        TomlTableNode created = CreateChildTable(table, ref lexer);
        table.Set(key, created);
        _headerDefined.Add(created);
        _current = created;
    }

    /// <summary>
    /// Defines (or appends to) an array of tables at <paramref name="path" /> and makes the new element the current
    /// table.
    /// </summary>
    /// <param name="path">The array key path.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    private void DefineArrayTable(List<string> path, ref TomlLexer lexer)
    {
        TomlTableNode table = _root;
        for (var i = 0; i < path.Count - 1; i++)
            table = WalkHeaderSegment(table, path[i], ref lexer);

        var key = path[^1];
        TomlArrayNode array;
        if (table.TryGetValue(key, out TomlReaderNode? existing))
        {
            if (existing is not TomlArrayNode found || !_tableArrays.Contains(found))
            {
                throw lexer.TokenError(existing is TomlArrayNode
                    ? TomlResourceStrings.Format_Invalid_TomlAppendToStaticArray
                    : TomlResourceStrings.Format_Invalid_TomlArrayTableConflict);
            }

            array = found;
        }
        else
        {
            array = new TomlArrayNode(lexer.TokenStartIndex);
            _tableArrays.Add(array);
            table.Set(key, array);
        }

        TomlTableNode element = CreateChildTable(table, ref lexer);
        array.Add(element);
        _current = element;
    }

    /// <summary>
    /// Walks (creating if necessary) a single super-table segment of a table header path.
    /// </summary>
    /// <param name="table">The table to walk from.</param>
    /// <param name="key">The segment key.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    /// <returns>The table for the segment.</returns>
    private TomlTableNode WalkHeaderSegment(TomlTableNode table, string key, ref TomlLexer lexer)
    {
        if (_inline.Contains(table))
            throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlExtendInlineTable);

        if (table.TryGetValue(key, out TomlReaderNode? existing))
        {
            switch (existing)
            {
                case TomlTableNode child when !_inline.Contains(child):
                    return child;
                case TomlTableNode:
                    throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlExtendInlineTable);
                case TomlArrayNode array when _tableArrays.Contains(array) && array.Count > 0:
                    return (TomlTableNode)array.Last;
                default:
                    throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateTable);
            }
        }

        TomlTableNode created = CreateChildTable(table, ref lexer);
        table.Set(key, created);
        _implicitSuper.Add(created);
        return created;
    }

    /// <summary>
    /// Creates a child table one level beneath <paramref name="parent" />, enforcing the maximum nesting depth.
    /// </summary>
    /// <param name="parent">The table the child will be added under.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    /// <returns>The created table.</returns>
    /// <exception cref="TomlFormatException">Thrown when the child would exceed the configured maximum depth.</exception>
    private TomlTableNode CreateChildTable(TomlTableNode parent, ref TomlLexer lexer)
    {
        if (parent.Depth >= _maxDepth)
            throw lexer.TokenError(string.Format(CultureInfo.CurrentCulture, TomlResourceStrings.Format_Invalid_TomlNestingTooDeep, _maxDepth));

        return new TomlTableNode(lexer.TokenStartIndex, parent.Depth + 1);
    }

    /// <summary>
    /// Assigns a key/value pair under <paramref name="target" />, creating intermediate dotted-key tables.
    /// </summary>
    /// <param name="target">The table receiving the assignment.</param>
    /// <param name="path">The dotted key path.</param>
    /// <param name="value">The value to assign.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    private void AssignKeyValue(TomlTableNode target, List<string> path, TomlReaderNode value, ref TomlLexer lexer)
    {
        TomlTableNode table = target;
        for (var i = 0; i < path.Count - 1; i++)
            table = WalkDottedSegment(table, path[i], ref lexer);

        var key = path[^1];
        if (table.ContainsKey(key))
            throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateKey);

        table.Set(key, value);
    }

    /// <summary>
    /// Walks (creating if necessary) a single intermediate table segment of a dotted key.
    /// </summary>
    /// <param name="table">The table to walk from.</param>
    /// <param name="key">The segment key.</param>
    /// <param name="lexer">The lexer whose current token supplies error positions.</param>
    /// <returns>The intermediate table.</returns>
    private TomlTableNode WalkDottedSegment(TomlTableNode table, string key, ref TomlLexer lexer)
    {
        if (table.TryGetValue(key, out TomlReaderNode? existing))
        {
            if (existing is not TomlTableNode child)
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlKeyOnValue);
            if (_inline.Contains(child))
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlExtendInlineTable);

            // Dotted keys may not extend a table already defined in [table] form (see toml-lang/toml#846).
            if (_headerDefined.Contains(child))
                throw lexer.TokenError(TomlResourceStrings.Format_Invalid_TomlDuplicateTable);

            // Traversing an implicit super-table with a dotted key defines it via dotted keys: a later [table] header
            // may no longer re-open it, mirroring the rule that headers cannot redefine dotted-key tables.
            if (_implicitSuper.Remove(child))
                _dotted.Add(child);

            return child;
        }

        TomlTableNode created = CreateChildTable(table, ref lexer);
        _dotted.Add(created);
        table.Set(key, created);
        return created;
    }
}
