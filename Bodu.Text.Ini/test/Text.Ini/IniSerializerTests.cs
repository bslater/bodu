// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniSerializerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;

using Bodu.Test.Kat;
using Bodu.Text.Serialization;

namespace Bodu.Text.Ini;

/// <summary>
/// Contains the shared model types for the <see cref="IniSerializer" /> test suite; the member backbone lives in the
/// <c>.Serialize</c>, <c>.Deserialize</c>, <c>.SerializeAsync</c>, <c>.DeserializeAsync</c>, and <c>.RoundTrip</c>
/// partials.
/// </summary>
[TestClass]
public partial class IniSerializerTests
{
    /// <summary>
    /// A root model with global scalar members, a section POCO, and a section dictionary.
    /// </summary>
    private sealed class ServerConfig
    {
        /// <summary>Gets or sets the server name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the retry count (a global key).</summary>
        public int Retries { get; set; }

        /// <summary>Gets or sets the database section.</summary>
        public DatabaseSection? Database { get; set; }

        /// <summary>Gets or sets the logging section as a dictionary.</summary>
        public Dictionary<string, string>? Logging { get; set; }
    }

    /// <summary>
    /// A section model of scalar members.
    /// </summary>
    private sealed class DatabaseSection
    {
        /// <summary>Gets or sets the host name.</summary>
        public string? Host { get; set; }

        /// <summary>Gets or sets the port number.</summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// A root model whose only member is required.
    /// </summary>
    private sealed class RequiredConfig
    {
        /// <summary>Gets or sets the required name.</summary>
        [Required]
        public string? Name { get; set; }
    }

    /// <summary>
    /// A root model with a section nested one level too deep.
    /// </summary>
    private sealed class DeepConfig
    {
        /// <summary>Gets or sets the outer section.</summary>
        public OuterSection? Outer { get; set; }
    }

    /// <summary>
    /// A section model that illegally contains a further section.
    /// </summary>
    private sealed class OuterSection
    {
        /// <summary>Gets or sets the nested section, which INI cannot represent.</summary>
        public DatabaseSection? Inner { get; set; }
    }

    /// <summary>
    /// A root model with a public global key and a private property, which is not part of its serialized form.
    /// </summary>
    private sealed class PrivateMemberConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets a value the serializer must not write, because the property is private.</summary>
        private string Ignored { get; set; } = "ignored";
    }

    /// <summary>
    /// A root model whose only member is an integer.
    /// </summary>
    private sealed class PortConfig
    {
        /// <summary>Gets or sets the port number (a global key).</summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// A root model whose only member is a <see cref="float" />.
    /// </summary>
    private sealed class RatioConfig
    {
        /// <summary>Gets or sets the ratio (a global key).</summary>
        public float Ratio { get; set; }
    }

    /// <summary>
    /// A POCO whose members are the scalars beyond the common set: <see cref="DateOnly" />, <see cref="TimeOnly" />, the
    /// 128-bit and half-precision numbers and <see cref="System.Numerics.BigInteger" />, two of them also in nullable form.
    /// </summary>
    private sealed class ExtendedScalarRecord
    {
        /// <summary>Gets or sets the date.</summary>
        /// <value>The date.</value>
        public DateOnly Day { get; set; }

        /// <summary>Gets or sets the time of day.</summary>
        /// <value>The time of day.</value>
        public TimeOnly Time { get; set; }

        /// <summary>Gets or sets the signed 128-bit number.</summary>
        /// <value>The signed 128-bit number.</value>
        public Int128 Large { get; set; }

        /// <summary>Gets or sets the unsigned 128-bit number.</summary>
        /// <value>The unsigned 128-bit number.</value>
        public UInt128 Unsigned { get; set; }

        /// <summary>Gets or sets the half-precision number.</summary>
        /// <value>The half-precision number.</value>
        public Half Small { get; set; }

        /// <summary>Gets or sets the arbitrary-precision integer.</summary>
        /// <value>The arbitrary-precision integer.</value>
        public System.Numerics.BigInteger Big { get; set; }

        /// <summary>Gets or sets the optional signed 128-bit number.</summary>
        /// <value>The number, or <see langword="null" />.</value>
        public Int128? MaybeLarge { get; set; }

        /// <summary>Gets or sets the optional time of day.</summary>
        /// <value>The time of day, or <see langword="null" />.</value>
        public TimeOnly? MaybeTime { get; set; }
    }

    /// <summary>
    /// A root model with a global key <c>Name</c> and no public parameterless constructor, which deserialization cannot
    /// create.
    /// </summary>
    private sealed class ConstructedConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructedConfig" /> class.
        /// </summary>
        /// <param name="name">The server name.</param>
        public ConstructedConfig(string name) =>
            Name = name;

        /// <summary>Gets or sets the server name (a global key).</summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// An abstract root model with a global key <c>Name</c>, which deserialization cannot create.
    /// </summary>
    private abstract class AbstractConfig
    {
        /// <summary>Gets or sets the server name (a global key).</summary>
        public string? Name { get; set; }
    }

    /// <summary>
    /// A root model whose members hold null and default values, two of them with an ignore condition of their own.
    /// </summary>
    private sealed class ConditionalConfig
    {
        /// <summary>Gets or sets a text, <see langword="null" /> unless set.</summary>
        public string? Text { get; set; }

        /// <summary>Gets or sets a number, zero unless set.</summary>
        public int Zero { get; set; }

        /// <summary>Gets or sets a number, one unless set.</summary>
        public int One { get; set; } = 1;

        /// <summary>Gets or sets a flag, <see langword="false" /> unless set.</summary>
        public bool Flag { get; set; }

        /// <summary>Gets or sets an optional number, <see langword="null" /> unless set.</summary>
        public int? Maybe { get; set; }

        /// <summary>Gets or sets a number its own condition leaves out while it is zero.</summary>
        [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.WhenWritingDefault)]
        public int ZeroByMember { get; set; }

        /// <summary>Gets or sets a text its own condition always writes.</summary>
        [Bodu.Text.Serialization.Ignore(Condition = IgnoreCondition.Never)]
        public string? NullByMember { get; set; }
    }

    /// <summary>
    /// A value-type root model with two members.
    /// </summary>
    private struct PortStruct
    {
        /// <summary>Gets or sets the port.</summary>
        public int Port { get; set; }

        /// <summary>Gets or sets the host.</summary>
        public string? Host { get; set; }
    }

    /// <summary>
    /// A root model with a global key and a section whose type is a nullable structure.
    /// </summary>
    private sealed class NullableStructSectionConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the section, <see langword="null" /> unless set.</summary>
        public PortStruct? S { get; set; }
    }

    /// <summary>
    /// A root model whose global key is a nullable number, which a value that is not a number cannot be converted to.
    /// </summary>
    private sealed class NullableCountConfig
    {
        /// <summary>Gets or sets the optional count (a global key).</summary>
        public int? Count { get; set; }
    }

    /// <summary>
    /// A root model whose global keys are dates and times, two of them also in nullable form.
    /// </summary>
    private sealed class TemporalConfig
    {
        /// <summary>Gets or sets the instant (a global key).</summary>
        public DateTime At { get; set; }

        /// <summary>Gets or sets a time of an unspecified kind (a global key).</summary>
        public DateTime Unspecified { get; set; }

        /// <summary>Gets or sets the instant with its offset (a global key).</summary>
        public DateTimeOffset AtOffset { get; set; }

        /// <summary>Gets or sets the optional instant (a global key).</summary>
        public DateTime? MaybeAt { get; set; }

        /// <summary>Gets or sets the optional instant with its offset (a global key).</summary>
        public DateTimeOffset? MaybeAtOffset { get; set; }
    }

    /// <summary>
    /// A root model whose global keys are the native-sized integers <see cref="nint" /> and <see cref="nuint" />, one
    /// also in nullable form.
    /// </summary>
    private sealed class NativeIntegerRecord
    {
        /// <summary>Gets or sets the native-sized signed integer (a global key).</summary>
        public nint Handle { get; set; }

        /// <summary>Gets or sets the native-sized unsigned integer (a global key).</summary>
        public nuint Size { get; set; }

        /// <summary>Gets or sets the optional native-sized signed integer (a global key).</summary>
        public nint? MaybeHandle { get; set; }
    }

    /// <summary>
    /// A root model whose global key and section member are declared with the C# <see langword="required" /> keyword
    /// rather than <see cref="RequiredAttribute" />.
    /// </summary>
    private sealed class RequiredKeywordConfig
    {
        /// <summary>Gets or sets the required server name (a global key).</summary>
        public required string Name { get; set; }

        /// <summary>Gets or sets the required database section.</summary>
        public required RequiredKeywordSection Database { get; set; }
    }

    /// <summary>
    /// A section model whose key is declared with the C# <see langword="required" /> keyword.
    /// </summary>
    private sealed class RequiredKeywordSection
    {
        /// <summary>Gets or sets the required host name.</summary>
        public required string Host { get; set; }
    }

    /// <summary>
    /// A root model whose two global keys are both mapped to the name <c>X</c>.
    /// </summary>
    private sealed class DuplicateKeyConfig
    {
        /// <summary>Gets or sets the first value mapped to <c>X</c>.</summary>
        [PropertyName("X")]
        public string? First { get; set; }

        /// <summary>Gets or sets the second value mapped to <c>X</c>.</summary>
        [PropertyName("X")]
        public string? Second { get; set; }
    }

    /// <summary>
    /// A base root model whose text <c>Port</c> global key <see cref="PortNumberHidingConfig" /> hides.
    /// </summary>
    private class PortTextBase
    {
        /// <summary>Gets or sets the base port text (a global key), which a derived type hides.</summary>
        public string? Port { get; set; }
    }

    /// <summary>
    /// A root model whose <see cref="int" /> <c>Port</c> global key hides the base type's text <c>Port</c> with the
    /// <see langword="new" /> modifier.
    /// </summary>
    private sealed class PortNumberHidingConfig
        : PortTextBase
    {
        /// <summary>Gets or sets the port (a global key), hiding the base type's.</summary>
        public new int Port { get; set; }
    }

    /// <summary>
    /// A base root model whose <see cref="int" /> <c>Port</c> global key <see cref="PortTextHidingConfig" /> hides.
    /// </summary>
    private class PortNumberBase
    {
        /// <summary>Gets or sets the base port number (a global key), which a derived type hides.</summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// A root model whose text <c>Port</c> global key hides the base type's <see cref="int" /> <c>Port</c> with the
    /// <see langword="new" /> modifier.
    /// </summary>
    private sealed class PortTextHidingConfig
        : PortNumberBase
    {
        /// <summary>Gets or sets the port text (a global key), hiding the base type's.</summary>
        public new string? Port { get; set; }
    }

    /// <summary>
    /// A root model whose section has no public parameterless constructor.
    /// </summary>
    private sealed class ConstructedSectionConfig
    {
        /// <summary>Gets or sets the database section, which deserialization cannot create.</summary>
        public ConstructedSection? Database { get; set; }
    }

    /// <summary>
    /// A section model with no public parameterless constructor.
    /// </summary>
    private sealed class ConstructedSection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructedSection" /> class.
        /// </summary>
        /// <param name="host">The host name.</param>
        public ConstructedSection(string host) =>
            Host = host;

        /// <summary>Gets or sets the host name.</summary>
        public string Host { get; set; }
    }

    /// <summary>
    /// A root model with a global scalar member and a section held in a dictionary that implements only the generic
    /// dictionary interface.
    /// </summary>
    private sealed class GenericOnlySectionConfig
    {
        /// <summary>Gets or sets the application name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets the logging section.</summary>
        public GenericOnlyDictionary<string>? Logging { get; set; }
    }

    /// <summary>
    /// A root model whose only member is a dictionary keyed by <see cref="int" />, which can be neither a section nor
    /// the reserved global section.
    /// </summary>
    private sealed class IntKeyedSectionConfig
    {
        /// <summary>Gets or sets the codes.</summary>
        public Dictionary<int, string>? Codes { get; set; }
    }

    /// <summary>
    /// A dictionary keyed by <see cref="string" /> that implements the generic
    /// <see cref="IDictionary{TKey, TValue}" /> interface but not the non-generic <see cref="IDictionary" />, and
    /// enumerates its entries in the order they were added.
    /// </summary>
    /// <typeparam name="TValue">The type of the values.</typeparam>
    private sealed class GenericOnlyDictionary<TValue> : IDictionary<string, TValue>
    {
        /// <summary>The entries, which enumerate in the order they were added.</summary>
        private readonly Dictionary<string, TValue> _entries = new(StringComparer.Ordinal);

        /// <inheritdoc />
        public ICollection<string> Keys => _entries.Keys;

        /// <inheritdoc />
        public ICollection<TValue> Values => _entries.Values;

        /// <inheritdoc />
        public int Count => _entries.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public TValue this[string key]
        {
            get => _entries[key];
            set => _entries[key] = value;
        }

        /// <inheritdoc />
        public void Add(string key, TValue value) =>
            _entries.Add(key, value);

        /// <inheritdoc />
        public void Add(KeyValuePair<string, TValue> item) =>
            _entries.Add(item.Key, item.Value);

        /// <inheritdoc />
        public void Clear() =>
            _entries.Clear();

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, TValue> item) =>
            ((ICollection<KeyValuePair<string, TValue>>)_entries).Contains(item);

        /// <inheritdoc />
        public bool ContainsKey(string key) =>
            _entries.ContainsKey(key);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex) =>
            ((ICollection<KeyValuePair<string, TValue>>)_entries).CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() =>
            _entries.GetEnumerator();

        /// <inheritdoc />
        public bool Remove(string key) =>
            _entries.Remove(key);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, TValue> item) =>
            ((ICollection<KeyValuePair<string, TValue>>)_entries).Remove(item);

        /// <inheritdoc />
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value) =>
            _entries.TryGetValue(key, out value);

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    /// <summary>
    /// A root model whose section member is a dictionary of lists, a value type that is not a scalar.
    /// </summary>
    private sealed class ListValuedSectionConfig
    {
        /// <summary>Gets or sets the codes section.</summary>
        public Dictionary<string, List<int>?>? Codes { get; set; }
    }

    /// <summary>
    /// A root model with a global scalar member and a member declared as <see cref="object" />, which maps to neither a
    /// key nor a section.
    /// </summary>
    private sealed class ObjectMemberConfig
    {
        /// <summary>Gets or sets the application name (a global key).</summary>
        public string? Name { get; set; }

        /// <summary>Gets or sets a value of any type.</summary>
        public object? O { get; set; }
    }

    /// <summary>
    /// A root model whose section type has a get-only member that is not a scalar.
    /// </summary>
    private sealed class GetOnlyListSectionConfig
    {
        /// <summary>Gets or sets the section.</summary>
        public GetOnlyListSection S { get; set; } = new();
    }

    /// <summary>
    /// A section model with a get-only list, which nests beyond INI's two levels whether it is written or read.
    /// </summary>
    private sealed class GetOnlyListSection
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; } = "a";

        /// <summary>Gets the values, which no key can hold.</summary>
        public List<int> Values { get; } = [];
    }

    /// <summary>
    /// A root model whose section type has a get-only scalar member and an ignored get-only list.
    /// </summary>
    private sealed class GetOnlyMembersSectionConfig
    {
        /// <summary>Gets or sets the section.</summary>
        public GetOnlyMembersSection S { get; set; } = new();
    }

    /// <summary>
    /// A section model with a settable scalar, a get-only scalar, which is written and not read, and a get-only list
    /// left out with <see cref="Bodu.Text.Serialization.IgnoreAttribute" />.
    /// </summary>
    private sealed class GetOnlyMembersSection
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; } = "a";

        /// <summary>Gets a computed value.</summary>
        public string Computed => "computed";

        /// <summary>Gets the values, which neither direction maps.</summary>
        [Bodu.Text.Serialization.Ignore]
        public List<int> Values { get; } = [];
    }

    /// <summary>
    /// A root model whose section type has a list that can only be set.
    /// </summary>
    private sealed class SetOnlyListSectionConfig
    {
        /// <summary>Gets or sets the section.</summary>
        public SetOnlyListSection S { get; set; } = new();
    }

    /// <summary>
    /// A section model with a list that can only be set, which no key can hold.
    /// </summary>
    private sealed class SetOnlyListSection
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; } = "a";

        /// <summary>Sets the values, which the setter discards.</summary>
        public List<int> Values
        {
            set => _ = value;
        }
    }

    /// <summary>
    /// A root model with a get-only list, which nests beyond INI's two levels.
    /// </summary>
    private sealed class GetOnlyRootListConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string Name { get; set; } = "a";

        /// <summary>Gets the values.</summary>
        public List<int> L { get; } = [];
    }

    /// <summary>
    /// A root model with a list that can only be set, which nests beyond INI's two levels.
    /// </summary>
    private sealed class SetOnlyRootListConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string Name { get; set; } = "a";

        /// <summary>Sets the values, which the setter discards.</summary>
        public List<int> L
        {
            set => _ = value;
        }
    }

    /// <summary>
    /// A root model with a get-only member declared as <see cref="object" />, which maps to neither a key nor a section.
    /// </summary>
    private sealed class GetOnlyRootObjectConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string Name { get; set; } = "a";

        /// <summary>Gets a value of any type.</summary>
        public object O { get; } = 1;
    }

    /// <summary>
    /// A root model with a member declared as <see cref="object" /> that can only be set, which maps to neither a key
    /// nor a section.
    /// </summary>
    private sealed class SetOnlyRootObjectConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string Name { get; set; } = "a";

        /// <summary>Sets a value of any type, which the setter discards.</summary>
        public object O
        {
            set => _ = value;
        }
    }

    /// <summary>
    /// A root model with a get-only section whose type has a list.
    /// </summary>
    private sealed class GetOnlyListSectionMemberConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string Name { get; set; } = "a";

        /// <summary>Gets the section.</summary>
        public ListSection S { get; } = new();
    }

    /// <summary>
    /// A root model with a section, <see langword="null" /> unless set, whose type has a list.
    /// </summary>
    private sealed class NullListSectionConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string Name { get; set; } = "a";

        /// <summary>Gets or sets the section.</summary>
        public ListSection? S { get; set; }
    }

    /// <summary>
    /// A section model with a settable list, which no key can hold.
    /// </summary>
    private sealed class ListSection
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; } = "b";

        /// <summary>Gets or sets the values.</summary>
        public List<int> Values { get; set; } = [];
    }

    /// <summary>
    /// A root model whose members INI cannot represent are all left out with <see cref="Bodu.Text.Serialization.IgnoreAttribute" />,
    /// whatever their accessors, at the root and in a section.
    /// </summary>
    private sealed class IgnoredUnrepresentableConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string Name { get; set; } = "a";

        /// <summary>Gets a list, left out.</summary>
        [Bodu.Text.Serialization.Ignore]
        public List<int> L { get; } = [];

        /// <summary>Sets a value of any type, left out.</summary>
        [Bodu.Text.Serialization.Ignore]
        public object O
        {
            set => _ = value;
        }

        /// <summary>Gets or sets the section.</summary>
        public IgnoredUnrepresentableSection S { get; set; } = new();
    }

    /// <summary>
    /// A section model whose list, which can only be set, is left out with
    /// <see cref="Bodu.Text.Serialization.IgnoreAttribute" />.
    /// </summary>
    private sealed class IgnoredUnrepresentableSection
    {
        /// <summary>Gets or sets the host.</summary>
        public string Host { get; set; } = "h";

        /// <summary>Sets the values, left out.</summary>
        [Bodu.Text.Serialization.Ignore]
        public List<int> Values
        {
            set => _ = value;
        }
    }

    /// <summary>
    /// A root model of shapes INI represents whatever their accessors and values: a get-only scalar, a section that is
    /// <see langword="null" /> unless set, and a get-only section of scalars.
    /// </summary>
    private sealed class RepresentableShapesConfig
    {
        /// <summary>Gets or sets the name (a global key).</summary>
        public string Name { get; set; } = "a";

        /// <summary>Gets a computed number, written and not read.</summary>
        public int Computed => 7;

        /// <summary>Gets or sets the database section.</summary>
        public DatabaseSection? Database { get; set; }

        /// <summary>Gets a section, written and not read.</summary>
        public GetOnlyMembersSection Fixed { get; } = new();
    }

    /// <summary>
    /// A dictionary keyed by <see cref="int" /> that implements the generic <see cref="IDictionary{TKey, TValue}" />
    /// interface but not the non-generic <see cref="IDictionary" />, so that it is neither a string-keyed dictionary nor
    /// one the serializer recognizes as a dictionary through the non-generic interface.
    /// </summary>
    private sealed class GenericOnlyIntKeyedDictionary : IDictionary<int, string>
    {
        /// <summary>The entries, which enumerate in the order they were added.</summary>
        private readonly Dictionary<int, string> _entries = [];

        /// <inheritdoc />
        public ICollection<int> Keys => _entries.Keys;

        /// <inheritdoc />
        public ICollection<string> Values => _entries.Values;

        /// <inheritdoc />
        public int Count => _entries.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public string this[int key]
        {
            get => _entries[key];
            set => _entries[key] = value;
        }

        /// <inheritdoc />
        public void Add(int key, string value) =>
            _entries.Add(key, value);

        /// <inheritdoc />
        public void Add(KeyValuePair<int, string> item) =>
            _entries.Add(item.Key, item.Value);

        /// <inheritdoc />
        public void Clear() =>
            _entries.Clear();

        /// <inheritdoc />
        public bool Contains(KeyValuePair<int, string> item) =>
            ((ICollection<KeyValuePair<int, string>>)_entries).Contains(item);

        /// <inheritdoc />
        public bool ContainsKey(int key) =>
            _entries.ContainsKey(key);

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<int, string>[] array, int arrayIndex) =>
            ((ICollection<KeyValuePair<int, string>>)_entries).CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<int, string>> GetEnumerator() =>
            _entries.GetEnumerator();

        /// <inheritdoc />
        public bool Remove(int key) =>
            _entries.Remove(key);

        /// <inheritdoc />
        public bool Remove(KeyValuePair<int, string> item) =>
            ((ICollection<KeyValuePair<int, string>>)_entries).Remove(item);

        /// <inheritdoc />
        public bool TryGetValue(int key, [MaybeNullWhen(false)] out string value) =>
            _entries.TryGetValue(key, out value);

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }

    /// <summary>
    /// A hand-written <see cref="IIniSectionFactory{TSection}" /> for <see cref="DatabaseSection" />, standing in for
    /// the generated factory in the reflection-free section-overload tests.
    /// </summary>
    private sealed class DatabaseSectionFactory : IIniSectionFactory<DatabaseSection>
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Keys { get; } = new[] { "Host", "Port" };

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<string, string>> GetEntries(DatabaseSection section)
        {
            yield return new KeyValuePair<string, string>("Host", section.Host ?? string.Empty);
            yield return new KeyValuePair<string, string>("Port", section.Port.ToString(CultureInfo.InvariantCulture));
        }

        /// <inheritdoc />
        public DatabaseSection Create(IEnumerable<KeyValuePair<string, string>> entries)
        {
            var section = new DatabaseSection();
            foreach (KeyValuePair<string, string> entry in entries)
            {
                if (string.Equals(entry.Key, "Host", StringComparison.Ordinal))
                    section.Host = entry.Value;
                else if (string.Equals(entry.Key, "Port", StringComparison.Ordinal))
                    section.Port = int.Parse(entry.Value, CultureInfo.InvariantCulture);
            }

            return section;
        }
    }

    /// <summary>
    /// Gets scalar root values, each of which serialization must reject with <see cref="IniSerializationException" /> naming its type.
    /// </summary>
    /// <value>The rows, each a named scalar value and the exception it raises.</value>
    public static IEnumerable<object[]> ScalarRootValues =>
    [
        [new InvalidKat<object>("decimal", 1.5m, typeof(IniSerializationException), MessageContains: "System.Decimal")],
        [new InvalidKat<object>("DateTime", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), typeof(IniSerializationException), MessageContains: "System.DateTime")],
        [new InvalidKat<object>("DateTimeOffset", DateTimeOffset.UnixEpoch, typeof(IniSerializationException), MessageContains: "System.DateTimeOffset")],
        [new InvalidKat<object>("TimeSpan", TimeSpan.FromHours(1), typeof(IniSerializationException), MessageContains: "System.TimeSpan")],
        [new InvalidKat<object>("Guid", Guid.Empty, typeof(IniSerializationException), MessageContains: "System.Guid")],
        [new InvalidKat<object>("Uri", new Uri("https://example.com/"), typeof(IniSerializationException), MessageContains: "System.Uri")],
        [new InvalidKat<object>("DateOnly", new DateOnly(2026, 1, 2), typeof(IniSerializationException), MessageContains: "System.DateOnly")],
        [new InvalidKat<object>("TimeOnly", new TimeOnly(1, 2), typeof(IniSerializationException), MessageContains: "System.TimeOnly")],
        [new InvalidKat<object>("Int128", (Int128)5, typeof(IniSerializationException), MessageContains: "System.Int128")],
        [new InvalidKat<object>("UInt128", (UInt128)5, typeof(IniSerializationException), MessageContains: "System.UInt128")],
        [new InvalidKat<object>("Half", (Half)1, typeof(IniSerializationException), MessageContains: "System.Half")],
        [new InvalidKat<object>("BigInteger", new BigInteger(7), typeof(IniSerializationException), MessageContains: "System.Numerics.BigInteger")],
        [new InvalidKat<object>("char", 'c', typeof(IniSerializationException), MessageContains: "System.Char")],
    ];

    /// <summary>
    /// Gets scalar root types, including nullable forms, each of which deserialization must reject with
    /// <see cref="IniSerializationException" /> naming its type.
    /// </summary>
    /// <value>The rows, each a named scalar type and the exception it raises.</value>
    public static IEnumerable<object[]> ScalarRootTypes =>
    [
        [new InvalidKat<Type>("decimal", typeof(decimal), typeof(IniSerializationException), MessageContains: "System.Decimal")],
        [new InvalidKat<Type>("DateTime", typeof(DateTime), typeof(IniSerializationException), MessageContains: "System.DateTime")],
        [new InvalidKat<Type>("DateTimeOffset", typeof(DateTimeOffset), typeof(IniSerializationException), MessageContains: "System.DateTimeOffset")],
        [new InvalidKat<Type>("TimeSpan", typeof(TimeSpan), typeof(IniSerializationException), MessageContains: "System.TimeSpan")],
        [new InvalidKat<Type>("Guid", typeof(Guid), typeof(IniSerializationException), MessageContains: "System.Guid")],
        [new InvalidKat<Type>("Uri", typeof(Uri), typeof(IniSerializationException), MessageContains: "System.Uri")],
        [new InvalidKat<Type>("DateOnly", typeof(DateOnly), typeof(IniSerializationException), MessageContains: "System.DateOnly")],
        [new InvalidKat<Type>("TimeOnly", typeof(TimeOnly), typeof(IniSerializationException), MessageContains: "System.TimeOnly")],
        [new InvalidKat<Type>("Int128", typeof(Int128), typeof(IniSerializationException), MessageContains: "System.Int128")],
        [new InvalidKat<Type>("UInt128", typeof(UInt128), typeof(IniSerializationException), MessageContains: "System.UInt128")],
        [new InvalidKat<Type>("Half", typeof(Half), typeof(IniSerializationException), MessageContains: "System.Half")],
        [new InvalidKat<Type>("BigInteger", typeof(BigInteger), typeof(IniSerializationException), MessageContains: "System.Numerics.BigInteger")],
        [new InvalidKat<Type>("int?", typeof(int?), typeof(IniSerializationException), MessageContains: "System.Nullable")],
        [new InvalidKat<Type>("Guid?", typeof(Guid?), typeof(IniSerializationException), MessageContains: "System.Nullable")],
        [new InvalidKat<Type>("DateOnly?", typeof(DateOnly?), typeof(IniSerializationException), MessageContains: "System.Nullable")],
    ];

    /// <summary>
    /// Gets root types with a member INI cannot represent, whatever its accessors or value, each of which serialization
    /// and deserialization must reject with <see cref="IniSerializationException" /> naming the member.
    /// </summary>
    /// <value>The rows, each a named root type and the exception it raises.</value>
    public static IEnumerable<object[]> UnrepresentableMemberTypes =>
    [
        [new InvalidKat<Type>("A section's list that can only be set", typeof(SetOnlyListSectionConfig), typeof(IniSerializationException), MessageContains: "'Values'")],
        [new InvalidKat<Type>("A section's get-only list", typeof(GetOnlyListSectionConfig), typeof(IniSerializationException), MessageContains: "'Values'")],
        [new InvalidKat<Type>("A get-only root list", typeof(GetOnlyRootListConfig), typeof(IniSerializationException), MessageContains: "'L'")],
        [new InvalidKat<Type>("A root list that can only be set", typeof(SetOnlyRootListConfig), typeof(IniSerializationException), MessageContains: "'L'")],
        [new InvalidKat<Type>("A get-only root object", typeof(GetOnlyRootObjectConfig), typeof(IniSerializationException), MessageContains: "'O'")],
        [new InvalidKat<Type>("A root object that can only be set", typeof(SetOnlyRootObjectConfig), typeof(IniSerializationException), MessageContains: "'O'")],
        [new InvalidKat<Type>("A get-only section with a list", typeof(GetOnlyListSectionMemberConfig), typeof(IniSerializationException), MessageContains: "'Values'")],
        [new InvalidKat<Type>("A null section with a list", typeof(NullListSectionConfig), typeof(IniSerializationException), MessageContains: "'Values'")],
        [new InvalidKat<Type>("A null section with a nested section", typeof(DeepConfig), typeof(IniSerializationException), MessageContains: "'Inner'")],
    ];

    /// <summary>
    /// Gets dictionary root values whose keys are not strings, each of which serialization must reject with
    /// <see cref="IniSerializationException" /> naming its type.
    /// </summary>
    /// <value>The rows, each a named dictionary and the exception it raises.</value>
    public static IEnumerable<object[]> NonStringKeyedDictionaryRootValues =>
    [
        [new InvalidKat<object>("Hashtable", new Hashtable { ["A"] = "a" }, typeof(IniSerializationException), MessageContains: typeof(Hashtable).ToString())],
        [new InvalidKat<object>("SortedList", new SortedList { ["A"] = "a" }, typeof(IniSerializationException), MessageContains: typeof(SortedList).ToString())],
        [new InvalidKat<object>("Dictionary<int, string>", new Dictionary<int, string> { [1] = "a" }, typeof(IniSerializationException), MessageContains: typeof(Dictionary<int, string>).ToString())],
    ];

    /// <summary>
    /// Gets dictionary root types whose keys are not strings, each of which deserialization must reject with
    /// <see cref="IniSerializationException" /> naming its type.
    /// </summary>
    /// <value>The rows, each a named dictionary type and the exception it raises.</value>
    public static IEnumerable<object[]> NonStringKeyedDictionaryRootTypes =>
    [
        [new InvalidKat<Type>("Hashtable", typeof(Hashtable), typeof(IniSerializationException), MessageContains: typeof(Hashtable).ToString())],
        [new InvalidKat<Type>("SortedList", typeof(SortedList), typeof(IniSerializationException), MessageContains: typeof(SortedList).ToString())],
        [new InvalidKat<Type>("Dictionary<int, string>", typeof(Dictionary<int, string>), typeof(IniSerializationException), MessageContains: typeof(Dictionary<int, string>).ToString())],
    ];

    /// <summary>
    /// Deserializes text into a root of a type known only at run time, through
    /// <see cref="IniSerializer.Deserialize{T}(string, IniSerializerOptions?)" />, rethrowing the serializer's own exception.
    /// </summary>
    /// <param name="rootType">The root type.</param>
    /// <param name="text">The text to deserialize.</param>
    /// <returns>The deserialized root.</returns>
    private static object? DeserializeAs(Type rootType, string text)
    {
        MethodInfo method = typeof(IniSerializer)
            .GetMethod(nameof(IniSerializer.Deserialize), [typeof(string), typeof(IniSerializerOptions)])!
            .MakeGenericMethod(rootType);

        try
        {
            return method.Invoke(null, [text, null]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
