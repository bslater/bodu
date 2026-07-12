// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Callbacks.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Bencode.Serialization;

using Bodu.Text.Serialization;

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the serialization callback contract: <see cref="IOnSerializing" /> and
/// <see cref="IOnSerialized" /> fire around writing, <see cref="IOnDeserializing" /> and
/// <see cref="IOnDeserialized" /> fire around reading, and the callbacks observe the documented state — a write
/// callback can influence the emitted bytes, the deserializing callback runs after construction but before settable
/// members are assigned, and the deserialized callback observes the fully materialized instance.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that serializing a value invokes <see cref="IOnSerializing.OnSerializing" /> before
    /// <see cref="IOnSerialized.OnSerialized" />, in that order.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenCallbacksImplemented_ShouldFireSerializingThenSerialized()
    {
        var log = new List<string>();
        var model = new SerializeCallbackModel(log) { Value = 1 };

        _ = BencodeSerializer.Serialize(model);

        CollectionAssert.AreEqual(new[] { "OnSerializing", "OnSerialized" }, log);
    }

    /// <summary>
    /// Verifies that a mutation performed in <see cref="IOnSerializing.OnSerializing" /> is reflected in the
    /// emitted bytes, since the callback runs before the members are written.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenOnSerializingMutatesState_ShouldReflectMutationInOutput()
    {
        var model = new MutatingSerializeModel { Value = 1 };

        byte[] bytes = BencodeSerializer.Serialize(model);

        // OnSerializing sets Value to 42 before the dictionary is written.
        Assert.AreEqual("d5:Valuei42ee", Encoding.Latin1.GetString(bytes));
    }

    /// <summary>
    /// Verifies that deserializing a value invokes <see cref="IOnDeserializing.OnDeserializing" /> before
    /// <see cref="IOnDeserialized.OnDeserialized" />, in that order.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenCallbacksImplemented_ShouldFireDeserializingThenDeserialized()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei1ee");

        DeserializeCallbackModel model = BencodeSerializer.Deserialize<DeserializeCallbackModel>(bytes);

        CollectionAssert.AreEqual(new[] { "OnDeserializing", "OnDeserialized" }, model.Log);
    }

    /// <summary>
    /// Verifies that <see cref="IOnDeserializing.OnDeserializing" /> runs before settable members are assigned,
    /// so it observes the member at its constructed default rather than the value read from the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOnDeserializing_ShouldRunBeforeSettableMembersAssigned()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei7ee");

        DeserializingObservesDefaultModel model = BencodeSerializer.Deserialize<DeserializingObservesDefaultModel>(bytes);

        Assert.AreEqual(0, model.ValueAtDeserializing);
        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that <see cref="IOnDeserialized.OnDeserialized" /> runs after every settable member is assigned,
    /// so it observes the value read from the input.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenOnDeserialized_ShouldRunAfterSettableMembersAssigned()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei7ee");

        DeserializedObservesValueModel model = BencodeSerializer.Deserialize<DeserializedObservesValueModel>(bytes);

        Assert.AreEqual(7, model.ValueAtDeserialized);
        Assert.AreEqual(7, model.Value);
    }

    /// <summary>
    /// Verifies that for a type built through a parameterized constructor,
    /// <see cref="IOnDeserializing.OnDeserializing" /> observes the constructor-bound argument, because the
    /// instance does not exist until the constructor has consumed it.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenParameterizedConstructorAndOnDeserializing_ShouldObserveConstructorArgument()
    {
        byte[] bytes = Encoding.Latin1.GetBytes("d5:Valuei5ee");

        ConstructorDeserializingModel model = BencodeSerializer.Deserialize<ConstructorDeserializingModel>(bytes);

        Assert.AreEqual(5, model.ValueAtDeserializing);
        Assert.AreEqual(5, model.Value);
    }

    /// <summary>
    /// Verifies that a value implementing all four callbacks fires no read callbacks on serialize and no write callbacks
    /// on deserialize, so the two lifecycles stay separate.
    /// </summary>
    [TestMethod]
    public void SerializeDeserialize_WhenAllCallbacks_ShouldFireOnlyRelevantLifecycle()
    {
        var serializeLog = new List<string>();
        var model = new AllCallbacksModel(serializeLog) { Value = 1 };

        byte[] bytes = BencodeSerializer.Serialize(model);
        CollectionAssert.AreEqual(new[] { "OnSerializing", "OnSerialized" }, serializeLog);

        AllCallbacksModel roundTripped = BencodeSerializer.Deserialize<AllCallbacksModel>(bytes);
        CollectionAssert.AreEqual(new[] { "OnDeserializing", "OnDeserialized" }, roundTripped.Log);
    }

    /// <summary>
    /// A model that records its serialize callbacks into an externally supplied log.
    /// </summary>
    private sealed class SerializeCallbackModel
        : IOnSerializing, IOnSerialized
    {
        /// <summary>
        /// The log that records the callback order.
        /// </summary>
        private readonly List<string> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializeCallbackModel" /> class.
        /// </summary>
        /// <param name="log">The log that records the callback order.</param>
        public SerializeCallbackModel(List<string> log)
        {
            _log = log;
        }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <inheritdoc />
        public void OnSerializing() => _log.Add("OnSerializing");

        /// <inheritdoc />
        public void OnSerialized() => _log.Add("OnSerialized");
    }

    /// <summary>
    /// A model whose serializing callback mutates a member before it is written.
    /// </summary>
    private sealed class MutatingSerializeModel
        : IOnSerializing
    {
        /// <summary>
        /// Gets or sets the value, overwritten by <see cref="OnSerializing" /> before the member is written.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <inheritdoc />
        public void OnSerializing() => Value = 42;
    }

    /// <summary>
    /// A model that records its deserialize callbacks into an internal log surfaced for assertion.
    /// </summary>
    private sealed class DeserializeCallbackModel
        : IOnDeserializing, IOnDeserialized
    {
        /// <summary>
        /// Gets the log that records the callback order.
        /// </summary>
        /// <value>The callback log.</value>
        public List<string> Log { get; } = new();

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <inheritdoc />
        public void OnDeserializing() => Log.Add("OnDeserializing");

        /// <inheritdoc />
        public void OnDeserialized() => Log.Add("OnDeserialized");
    }

    /// <summary>
    /// A model whose deserializing callback captures the member value before any settable member is assigned.
    /// </summary>
    private sealed class DeserializingObservesDefaultModel
        : IOnDeserializing
    {
        /// <summary>
        /// Gets or sets the value read from the input.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <summary>
        /// Gets the value observed when <see cref="OnDeserializing" /> ran.
        /// </summary>
        /// <value>The observed value.</value>
        public int ValueAtDeserializing { get; private set; }

        /// <inheritdoc />
        public void OnDeserializing() => ValueAtDeserializing = Value;
    }

    /// <summary>
    /// A model whose deserialized callback captures the member value after every settable member is assigned.
    /// </summary>
    private sealed class DeserializedObservesValueModel
        : IOnDeserialized
    {
        /// <summary>
        /// Gets or sets the value read from the input.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <summary>
        /// Gets the value observed when <see cref="OnDeserialized" /> ran.
        /// </summary>
        /// <value>The observed value.</value>
        public int ValueAtDeserialized { get; private set; }

        /// <inheritdoc />
        public void OnDeserialized() => ValueAtDeserialized = Value;
    }

    /// <summary>
    /// A model built through a parameterized constructor whose deserializing callback observes the bound argument.
    /// </summary>
    private sealed class ConstructorDeserializingModel
        : IOnDeserializing
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConstructorDeserializingModel" /> class.
        /// </summary>
        /// <param name="value">The value bound through the constructor.</param>
        public ConstructorDeserializingModel(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value, supplied through the constructor.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; }

        /// <summary>
        /// Gets the value observed when <see cref="OnDeserializing" /> ran.
        /// </summary>
        /// <value>The observed value.</value>
        public int ValueAtDeserializing { get; private set; }

        /// <inheritdoc />
        public void OnDeserializing() => ValueAtDeserializing = Value;
    }

    /// <summary>
    /// A model implementing all four callbacks, used to confirm the read and write lifecycles do not overlap.
    /// </summary>
    private sealed class AllCallbacksModel
        : IOnSerializing, IOnSerialized, IOnDeserializing, IOnDeserialized
    {
        /// <summary>
        /// The serialize log supplied by the caller, used only when the instance is written.
        /// </summary>
        private readonly List<string>? _serializeLog;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllCallbacksModel" /> class for deserialization.
        /// </summary>
        public AllCallbacksModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AllCallbacksModel" /> class with a serialize log.
        /// </summary>
        /// <param name="serializeLog">The log that records the serialize callbacks.</param>
        public AllCallbacksModel(List<string> serializeLog)
        {
            _serializeLog = serializeLog;
        }

        /// <summary>
        /// Gets the log that records the deserialize callbacks.
        /// </summary>
        /// <value>The deserialize log.</value>
        public List<string> Log { get; } = new();

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public int Value { get; set; }

        /// <inheritdoc />
        public void OnSerializing() => _serializeLog?.Add("OnSerializing");

        /// <inheritdoc />
        public void OnSerialized() => _serializeLog?.Add("OnSerialized");

        /// <inheritdoc />
        public void OnDeserializing() => Log.Add("OnDeserializing");

        /// <inheritdoc />
        public void OnDeserialized() => Log.Add("OnDeserialized");
    }
}
