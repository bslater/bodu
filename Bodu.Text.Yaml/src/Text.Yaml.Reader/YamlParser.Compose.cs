// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlParser.Compose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Text.Yaml.Reader;

/// <summary>
/// Composition passes applied to a parsed document before it is returned: alias resolution, merge-key (<c>&lt;&lt;</c>)
/// expansion, and core-schema tag coercion.
/// </summary>
internal sealed partial class YamlParser
{
    /// <summary>
    /// Runs the composition passes over the current row store in dependency order.
    /// </summary>
    private void Compose()
    {
        ResolveAliases();
        CoerceTags();
        ExpandMergeKeys();
    }

    /// <summary>
    /// Resolves each alias row to the most recent anchor of the same name defined earlier in the document.
    /// </summary>
    /// <exception cref="YamlFormatException">An alias refers to an anchor that was never defined.</exception>
    private void ResolveAliases()
    {
        var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r.Anchor is not null)
                anchors[r.Anchor] = i;

            if (r.Kind == YamlReaderNodeKind.Alias && r.Tag is not null)
            {
                if (!anchors.TryGetValue(r.Tag, out var target))
                {
                    throw new YamlFormatException(string.Format(
                        CultureInfo.CurrentCulture, YamlResourceStrings.Format_Invalid_YamlUndefinedAlias, r.Tag));
                }

                r.AliasTarget = target;
                _rows[i] = r;
            }
        }
    }

    /// <summary>
    /// Expands merge keys by injecting alias rows for keys contributed by the merged mappings.
    /// </summary>
    private void ExpandMergeKeys()
    {
        var count = _rows.Count;
        for (var i = 0; i < count; i++)
        {
            if (_rows[i].Kind == YamlReaderNodeKind.Mapping)
                ExpandMappingMerge(i);
        }
    }

    /// <summary>
    /// Expands a single mapping's merge key, preserving explicit-key and earlier-source precedence.
    /// </summary>
    /// <param name="mapping">The mapping row index.</param>
    private void ExpandMappingMerge(int mapping)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var mergeChild = -1;
        var mergePrev = -1;
        var prev = -1;
        var child = _rows[mapping].FirstChild;

        while (child >= 0)
        {
            var key = _rows[child].Key;
            if (key == "<<" && mergeChild < 0)
            {
                mergeChild = child;
                mergePrev = prev;
            }
            else if (key is not null)
            {
                keys.Add(key);
            }

            prev = child;
            child = _rows[child].NextSibling;
        }

        if (mergeChild < 0)
            return;

        UnlinkChild(mapping, mergeChild, mergePrev);

        var sources = new List<int>();
        var value = Resolve(mergeChild);
        var valueRow = _rows[value];
        if (valueRow.Kind == YamlReaderNodeKind.Mapping)
        {
            sources.Add(value);
        }
        else if (valueRow.Kind == YamlReaderNodeKind.Sequence)
        {
            var element = valueRow.FirstChild;
            while (element >= 0)
            {
                sources.Add(Resolve(element));
                element = _rows[element].NextSibling;
            }
        }

        foreach (var source in sources)
        {
            if (_rows[source].Kind != YamlReaderNodeKind.Mapping)
                continue;

            var pair = _rows[source].FirstChild;
            while (pair >= 0)
            {
                var key = _rows[pair].Key;
                if (key is not null && keys.Add(key))
                    AppendChild(mapping, NewMergeAlias(key, Resolve(pair)));

                pair = _rows[pair].NextSibling;
            }
        }
    }

    /// <summary>
    /// Removes a child from a container's sibling chain and updates its bookkeeping.
    /// </summary>
    /// <param name="parent">The container row index.</param>
    /// <param name="child">The child row index to remove.</param>
    /// <param name="previous">The child's predecessor row index, or <c>-1</c> when it is the first child.</param>
    private void UnlinkChild(int parent, int child, int previous)
    {
        var p = _rows[parent];
        var next = _rows[child].NextSibling;

        if (previous < 0)
            p.FirstChild = next;
        else
        {
            var prevRow = _rows[previous];
            prevRow.NextSibling = next;
            _rows[previous] = prevRow;
        }

        if (p.LastChild == child)
            p.LastChild = previous;

        p.ChildCount--;
        _rows[parent] = p;
    }

    /// <summary>
    /// Creates a merge-injected alias row carrying a key and pointing at a merged value.
    /// </summary>
    /// <param name="key">The merged key.</param>
    /// <param name="target">The resolved target row index.</param>
    /// <returns>The row index of the new alias.</returns>
    private int NewMergeAlias(string key, int target)
    {
        var row = new YamlReaderRow
        {
            Kind = YamlReaderNodeKind.Alias,
            ValueKind = YamlValueKind.None,
            ScalarStyle = YamlScalarStyle.Plain,
            Key = key,
            Anchor = null,
            Tag = null,
            Offset = _rows[target].Offset,
            FirstChild = -1,
            LastChild = -1,
            NextSibling = -1,
            AliasTarget = target,
            ChildCount = 0,
            Depth = 0,
            Flags = YamlReaderRowFlags.Merged,
        };
        _rows.Add(row);
        return _rows.Count - 1;
    }

    /// <summary>
    /// Applies core-schema tag coercion to tagged scalar rows.
    /// </summary>
    /// <remarks>
    /// Handles the verbatim and shorthand spellings of the <c>str</c>, <c>null</c>, <c>bool</c>, <c>int</c>, and
    /// <c>float</c> core tags. The <c>str</c> tag forces any scalar to a string; the others reinterpret a plain or
    /// quoted scalar's text under the requested type.
    /// </remarks>
    private void CoerceTags()
    {
        for (var i = 0; i < _rows.Count; i++)
        {
            var r = _rows[i];
            if (r.Kind != YamlReaderNodeKind.Scalar || r.Tag is null)
                continue;

            var tag = NormalizeCoreTag(r.Tag);
            if (tag is null)
                continue;

            var text = ScalarText(r);
            switch (tag)
            {
                case "str":
                    SetString(ref r, text);
                    break;

                case "null":
                    r.ValueKind = YamlValueKind.Null;
                    r.ScalarBits = 0;
                    break;

                case "bool":
                    if (bool.TryParse(text, out var b))
                    {
                        r.ValueKind = YamlValueKind.Boolean;
                        r.ScalarBits = b ? 1 : 0;
                    }

                    break;

                case "int":
                    if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var l))
                    {
                        r.ValueKind = YamlValueKind.Integer;
                        r.ScalarBits = l;
                    }

                    break;

                case "float":
                    if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    {
                        r.ValueKind = YamlValueKind.Float;
                        r.ScalarBits = BitConverter.DoubleToInt64Bits(d);
                    }

                    break;
            }

            _rows[i] = r;
        }
    }

    /// <summary>
    /// Reinterprets the row as a string scalar carrying the given text.
    /// </summary>
    /// <param name="r">The row to modify.</param>
    /// <param name="text">The string value.</param>
    private void SetString(ref YamlReaderRow r, string text)
    {
        if (r.ValueKind == YamlValueKind.String)
            return;

        r.ValueKind = YamlValueKind.String;
        r.ScalarBits = _strings.Count;
        _strings.Add(text);
    }

    /// <summary>
    /// Produces the canonical text of a scalar row for tag reinterpretation.
    /// </summary>
    /// <param name="r">The scalar row.</param>
    /// <returns>The scalar's textual form.</returns>
    private string ScalarText(YamlReaderRow r) => r.ValueKind switch
    {
        YamlValueKind.String => _strings[(int)r.ScalarBits],
        YamlValueKind.Integer => r.AsInt64().ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Float => r.AsDouble().ToString(CultureInfo.InvariantCulture),
        YamlValueKind.Boolean => r.AsBoolean() ? "true" : "false",
        _ => string.Empty,
    };

    /// <summary>
    /// Normalizes a tag to its core-schema short name when it is a recognized core tag.
    /// </summary>
    /// <param name="tag">The captured tag text.</param>
    /// <returns>The core short name (for example <c>int</c>), or <see langword="null" /> when not a core tag.</returns>
    private static string? NormalizeCoreTag(string tag)
    {
        var name = tag switch
        {
            "!!str" or "tag:yaml.org,2002:str" or "!<tag:yaml.org,2002:str>" => "str",
            "!!null" or "tag:yaml.org,2002:null" or "!<tag:yaml.org,2002:null>" => "null",
            "!!bool" or "tag:yaml.org,2002:bool" or "!<tag:yaml.org,2002:bool>" => "bool",
            "!!int" or "tag:yaml.org,2002:int" or "!<tag:yaml.org,2002:int>" => "int",
            "!!float" or "tag:yaml.org,2002:float" or "!<tag:yaml.org,2002:float>" => "float",
            _ => null,
        };

        return name;
    }

    /// <summary>
    /// Resolves an alias row to its target, or returns the index unchanged for non-alias rows.
    /// </summary>
    /// <param name="index">The row index.</param>
    /// <returns>The resolved row index.</returns>
    private int Resolve(int index)
    {
        var r = _rows[index];
        return r.Kind == YamlReaderNodeKind.Alias && r.AliasTarget >= 0 ? r.AliasTarget : index;
    }
}
