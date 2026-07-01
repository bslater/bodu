// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Yaml.Document;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Nodes;

/// <summary>
/// Represents a node in a mutable YAML document object model, in the manner of
/// <see cref="System.Text.Json.Nodes.JsonNode" />.
/// </summary>
/// <remarks>
/// The node tree is fully mutable: mappings (<see cref="YamlObject" />), sequences (<see cref="YamlArray" />), and
/// scalars (<see cref="YamlValue" />) can be created, edited, and re-serialized. A node may appear at most once in a
/// tree.
/// </remarks>
public abstract class YamlNode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlNode" /> class.
    /// </summary>
    private protected YamlNode()
    {
    }

    /// <summary>
    /// Gets or sets the child node at the specified sequence index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <value>The child node, or <see langword="null" />.</value>
    /// <exception cref="InvalidOperationException">The node is not a sequence.</exception>
    public virtual YamlNode? this[int index]
    {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }

    /// <summary>
    /// Gets or sets the child node mapped to the specified key.
    /// </summary>
    /// <param name="key">The mapping key.</param>
    /// <value>The mapped node, or <see langword="null" />.</value>
    /// <exception cref="InvalidOperationException">The node is not a mapping.</exception>
    public virtual YamlNode? this[string key]
    {
        get => throw new InvalidOperationException();
        set => throw new InvalidOperationException();
    }

    /// <summary>
    /// Parses YAML text into a mutable node tree.
    /// </summary>
    /// <param name="yaml">The YAML source.</param>
    /// <returns>The root node, or <see langword="null" /> when the document is empty/null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="yaml" /> is <see langword="null" />.</exception>
    /// <exception cref="YamlFormatException">The source is not valid YAML.</exception>
    public static YamlNode? Parse(string yaml)
    {
        Bodu.ThrowHelper.ThrowIfNull(yaml);
        using var document = YamlDocument.Parse(yaml);
        return FromElement(document.RootElement);
    }

    /// <summary>
    /// Returns this node as a <see cref="YamlObject" />.
    /// </summary>
    /// <returns>The node typed as a mapping.</returns>
    /// <exception cref="InvalidOperationException">The node is not a mapping.</exception>
    public YamlObject AsObject() => this as YamlObject ?? throw new InvalidOperationException();

    /// <summary>
    /// Returns this node as a <see cref="YamlArray" />.
    /// </summary>
    /// <returns>The node typed as a sequence.</returns>
    /// <exception cref="InvalidOperationException">The node is not a sequence.</exception>
    public YamlArray AsArray() => this as YamlArray ?? throw new InvalidOperationException();

    /// <summary>
    /// Returns this node as a <see cref="YamlValue" />.
    /// </summary>
    /// <returns>The node typed as a scalar.</returns>
    /// <exception cref="InvalidOperationException">The node is not a scalar.</exception>
    public YamlValue AsValue() => this as YamlValue ?? throw new InvalidOperationException();

    /// <summary>
    /// Serializes the node tree to YAML text.
    /// </summary>
    /// <returns>The YAML representation of the node.</returns>
    public string ToYamlString()
    {
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8YamlWriter(buffer);
        WriteTo(ref writer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    /// <summary>
    /// Writes this node to the given writer.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    public abstract void WriteTo(ref Utf8YamlWriter writer);

    /// <summary>
    /// Builds a mutable node from a read-only document element.
    /// </summary>
    /// <param name="element">The element to convert.</param>
    /// <returns>The corresponding mutable node, or <see langword="null" /> for a null scalar.</returns>
    internal static YamlNode? FromElement(YamlElement element)
    {
        switch (element.ValueKind)
        {
            case YamlValueKind.Null:
                return null;
            case YamlValueKind.Boolean:
                return YamlValue.Create(element.GetBoolean());
            case YamlValueKind.Integer:
                return YamlValue.Create(element.GetInt64());
            case YamlValueKind.Float:
                return YamlValue.Create(element.GetDouble());
            case YamlValueKind.String:
                return YamlValue.Create(element.GetString());
            case YamlValueKind.Sequence:
                var array = new YamlArray();
                foreach (YamlElement item in element.EnumerateSequence())
                    array.Add(FromElement(item));

                return array;
            default:
                var obj = new YamlObject();
                foreach (YamlProperty pair in element.EnumerateMapping())
                    obj[pair.Name] = FromElement(pair.Value);

                return obj;
        }
    }

    /// <summary>
    /// Writes a child node to the writer, emitting a null scalar when the child is absent.
    /// </summary>
    /// <param name="writer">The writer to emit into.</param>
    /// <param name="node">The child node, or <see langword="null" />.</param>
    private protected static void WriteChild(ref Utf8YamlWriter writer, YamlNode? node)
    {
        if (node is null)
            writer.WriteNull();
        else
            node.WriteTo(ref writer);
    }
}
